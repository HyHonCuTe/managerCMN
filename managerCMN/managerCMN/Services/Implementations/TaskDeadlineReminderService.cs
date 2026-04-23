using Microsoft.EntityFrameworkCore;
using managerCMN.Data;
using managerCMN.Models.Enums;
using managerCMN.Services.Interfaces;

namespace managerCMN.Services.Implementations;

public class TaskDeadlineReminderService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TaskDeadlineReminderService> _logger;

    public TaskDeadlineReminderService(IServiceScopeFactory scopeFactory, ILogger<TaskDeadlineReminderService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = NextRunTime() - DateTime.Now;
            if (delay > TimeSpan.Zero)
                await Task.Delay(delay, stoppingToken);

            if (stoppingToken.IsCancellationRequested) break;

            try
            {
                await SendDeadlineRemindersAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending deadline reminders");
            }
        }
    }

    private static DateTime NextRunTime()
    {
        var now = DateTime.Now;
        var target = DateTime.Today.AddHours(10).AddMinutes(10);
        return now < target ? target : target.AddDays(1);
    }

    private async Task SendDeadlineRemindersAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

        var now = DateTime.Now;
        var today = now.Date;
        var d1 = today.AddDays(1);
        var d3 = today.AddDays(3);

        await SendTaskRemindersAsync(context, notificationService, now, d1, d3, stoppingToken);
        await SendTicketRemindersAsync(context, notificationService, now, d1, d3, stoppingToken);
    }

    // ── Project task reminders ────────────────────────────────────────────────

    private static async Task SendTaskRemindersAsync(
        ApplicationDbContext context,
        INotificationService notificationService,
        DateTime now, DateTime d1, DateTime d3,
        CancellationToken ct)
    {
        var tasks = await context.ProjectTasks
            .AsNoTracking()
            .Where(t => t.DueDate.HasValue
                && t.DueDate.Value.Date >= d1
                && t.DueDate.Value.Date <= d3
                && t.Status != ProjectTaskStatus.Done
                && t.Status != ProjectTaskStatus.Cancelled)
            .Select(t => new { t.ProjectTaskId, t.ProjectId, t.Title, t.DueDate })
            .ToListAsync(ct);

        if (tasks.Count == 0) return;

        var taskIds = tasks.Select(t => t.ProjectTaskId).ToList();

        // Only assignees who haven't completed their part
        var assignmentMap = await context.ProjectTaskAssignments
            .AsNoTracking()
            .Where(a => taskIds.Contains(a.ProjectTaskId) && !a.IsCompleted)
            .GroupBy(a => a.ProjectTaskId)
            .ToDictionaryAsync(g => g.Key, g => g.Select(a => a.EmployeeId).ToList(), ct);

        var projectIds = tasks.Select(t => t.ProjectId).Distinct().ToList();
        var projectNames = await context.Projects
            .AsNoTracking()
            .Where(p => projectIds.Contains(p.ProjectId))
            .ToDictionaryAsync(p => p.ProjectId, p => p.Name, ct);

        var allEmployeeIds = assignmentMap.Values.SelectMany(ids => ids).Distinct().ToList();
        if (allEmployeeIds.Count == 0) return;

        var employeeUserMap = await context.Users
            .AsNoTracking()
            .Where(u => u.EmployeeId != null && allEmployeeIds.Contains(u.EmployeeId!.Value))
            .ToDictionaryAsync(u => u.EmployeeId!.Value, u => u.UserId, ct);

        foreach (var task in tasks)
        {
            if (!assignmentMap.TryGetValue(task.ProjectTaskId, out var assigneeIds) || assigneeIds.Count == 0)
                continue;

            var projectName = projectNames.TryGetValue(task.ProjectId, out var pn) ? pn : "Dự án";
            // Treat task deadline as end of due date (midnight of next day)
            var deadline = task.DueDate!.Value.Date.AddDays(1);
            var remaining = deadline - now;
            var timeLeft = FormatTimeLeft(remaining);
            var dueDateStr = task.DueDate.Value.ToString("dd/MM/yyyy");
            var targetUrl = $"/Project/Details/{task.ProjectId}?openTaskId={task.ProjectTaskId}";

            var title = $"Sắp hết hạn: {task.Title}";
            var message = $"Task trong dự án \"{projectName}\" sẽ hết hạn vào {dueDateStr} ({timeLeft}).";
            var tg =
                $"⏰ <b>Nhắc nhở deadline</b>\n" +
                $"📁 Dự án: {H(projectName)}\n" +
                $"🔖 Task: {H(task.Title)}\n" +
                $"📅 Hạn hoàn thành: {dueDateStr}\n" +
                $"⌛ Thời gian còn lại: {timeLeft}";

            foreach (var empId in assigneeIds)
            {
                if (!employeeUserMap.TryGetValue(empId, out var userId)) continue;
                await notificationService.CreateAsync(userId, title, message, targetUrl, telegramText: tg);
            }
        }
    }

    // ── Ticket reminders ──────────────────────────────────────────────────────

    private static async Task SendTicketRemindersAsync(
        ApplicationDbContext context,
        INotificationService notificationService,
        DateTime now, DateTime d1, DateTime d3,
        CancellationToken ct)
    {
        var tickets = await context.Tickets
            .AsNoTracking()
            .Where(t => t.Deadline.HasValue
                && t.Deadline.Value.Date >= d1
                && t.Deadline.Value.Date <= d3
                && t.Status != TicketStatus.Closed
                && t.Status != TicketStatus.Resolved
                && t.Status != TicketStatus.Cancelled)
            .Select(t => new { t.TicketId, t.Title, t.Deadline, t.CreatedBy })
            .ToListAsync(ct);

        if (tickets.Count == 0) return;

        var ticketIds = tickets.Select(t => t.TicketId).ToList();

        // Recipients who haven't completed their part
        var recipientMap = await context.TicketRecipients
            .AsNoTracking()
            .Where(r => ticketIds.Contains(r.TicketId) && r.Status != TicketRecipientStatus.Completed)
            .GroupBy(r => r.TicketId)
            .ToDictionaryAsync(g => g.Key, g => g.Select(r => r.EmployeeId).ToList(), ct);

        // Collect all employee IDs (creators + non-completed recipients)
        var creatorIds = tickets.Select(t => t.CreatedBy).Distinct().ToList();
        var recipientIds = recipientMap.Values.SelectMany(ids => ids).Distinct().ToList();
        var allEmployeeIds = creatorIds.Concat(recipientIds).Distinct().ToList();

        var employeeUserMap = await context.Users
            .AsNoTracking()
            .Where(u => u.EmployeeId != null && allEmployeeIds.Contains(u.EmployeeId!.Value))
            .ToDictionaryAsync(u => u.EmployeeId!.Value, u => u.UserId, ct);

        foreach (var ticket in tickets)
        {
            var remaining = ticket.Deadline!.Value - now;
            var timeLeft = FormatTimeLeft(remaining);
            var deadlineStr = ticket.Deadline.Value.ToString("dd/MM/yyyy HH:mm");
            var targetUrl = $"/Ticket/Details/{ticket.TicketId}";

            var title = $"Sắp hết hạn yêu cầu: {ticket.Title}";
            var message = $"Yêu cầu \"{ticket.Title}\" sẽ hết hạn vào {deadlineStr} ({timeLeft}).";
            var tg =
                $"⏰ <b>Nhắc nhở deadline yêu cầu</b>\n" +
                $"📨 Ticket: {H(ticket.Title)}\n" +
                $"📅 Hạn xử lý: {deadlineStr}\n" +
                $"⌛ Thời gian còn lại: {timeLeft}";

            var notifiedUserIds = new HashSet<int>();

            // Notify creator
            if (employeeUserMap.TryGetValue(ticket.CreatedBy, out var creatorUserId))
            {
                await notificationService.CreateAsync(creatorUserId, title, message, targetUrl, telegramText: tg);
                notifiedUserIds.Add(creatorUserId);
            }

            // Notify non-completed recipients (dedup by userId)
            if (recipientMap.TryGetValue(ticket.TicketId, out var empIds))
            {
                foreach (var empId in empIds)
                {
                    if (!employeeUserMap.TryGetValue(empId, out var userId)) continue;
                    if (!notifiedUserIds.Add(userId)) continue;
                    await notificationService.CreateAsync(userId, title, message, targetUrl, telegramText: tg);
                }
            }
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string FormatTimeLeft(TimeSpan ts)
    {
        if (ts <= TimeSpan.Zero) return "đã hết hạn";

        var days = (int)ts.TotalDays;
        var hours = ts.Hours;
        var minutes = ts.Minutes;

        var parts = new List<string>();
        if (days > 0) parts.Add($"{days} ngày");
        if (hours > 0) parts.Add($"{hours} giờ");
        if (minutes > 0 || parts.Count == 0) parts.Add($"{minutes} phút");

        return "còn " + string.Join(" ", parts);
    }

    private static string H(string s) => s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
}
