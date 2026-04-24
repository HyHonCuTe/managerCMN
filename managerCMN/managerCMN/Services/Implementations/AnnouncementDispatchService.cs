using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using managerCMN.Data;
using managerCMN.Helpers;
using managerCMN.Models.Enums;
using managerCMN.Services.Interfaces;

namespace managerCMN.Services.Implementations;

public class AnnouncementDispatchService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AnnouncementDispatchService> _logger;

    public AnnouncementDispatchService(IServiceScopeFactory scopeFactory, ILogger<AnnouncementDispatchService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // Align to the next full minute (VN time)
            var now = DateTimeHelper.VietnamNow;
            var nextMinute = now.AddSeconds(60 - now.Second).AddMilliseconds(-now.Millisecond);
            var delay = nextMinute - DateTimeHelper.VietnamNow;
            if (delay > TimeSpan.Zero)
                await Task.Delay(delay, stoppingToken);

            if (stoppingToken.IsCancellationRequested) break;

            try
            {
                await DispatchPendingAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error dispatching scheduled announcements");
            }
        }
    }

    private async Task DispatchPendingAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var telegram = scope.ServiceProvider.GetRequiredService<ITelegramService>();

        if (!telegram.IsConfigured) return;

        var now = DateTimeHelper.VietnamNow;

        var due = await db.ScheduledAnnouncements
            .Where(a => a.Status == AnnouncementStatus.Pending && a.ScheduledAt <= now)
            .ToListAsync(ct);

        if (due.Count == 0) return;

        foreach (var announcement in due)
        {
            try
            {
                var chatIds = await ResolveChatIdsAsync(db, announcement, ct);
                if (chatIds.Count > 0)
                {
                    var text = BuildTelegramText(announcement);
                    await Task.WhenAll(chatIds.Select(cid => telegram.SendMessageAsync(cid, text)));
                }

                announcement.Status = AnnouncementStatus.Sent;
                announcement.SentAt = DateTimeHelper.VietnamNow;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send announcement {Id}", announcement.AnnouncementId);
                announcement.Status = AnnouncementStatus.Sent;
                announcement.SentAt = DateTimeHelper.VietnamNow;
            }
        }

        await db.SaveChangesAsync(ct);
    }

    private static async Task<List<string>> ResolveChatIdsAsync(
        ApplicationDbContext db,
        Models.Entities.ScheduledAnnouncement ann,
        CancellationToken ct)
    {
        // Specific employee IDs override all other filters
        if (!string.IsNullOrWhiteSpace(ann.FilterEmployeeIds))
        {
            List<int>? ids = null;
            try { ids = JsonSerializer.Deserialize<List<int>>(ann.FilterEmployeeIds); } catch { }
            if (ids == null || ids.Count == 0) return [];

            var users = await db.Users
                .AsNoTracking()
                .Where(u => u.IsActive
                    && u.TelegramChatId != null
                    && u.EmployeeId != null
                    && ids.Contains(u.EmployeeId!.Value))
                .ToListAsync(ct);

            return users
                .Where(u => TelegramNotificationPreferenceHelper.IsEnabled(u, TelegramNotificationCategory.Announcement))
                .Select(u => u.TelegramChatId!)
                .ToList();
        }

        // Broad filter: all active employees, optionally narrowed by department/gender.
        var query = db.Users
            .AsNoTracking()
            .Include(u => u.Employee)
            .Where(u => u.IsActive
                && u.TelegramChatId != null
                && u.EmployeeId != null
                && u.Employee != null
                && u.Employee.Status == EmployeeStatus.Active);

        if (ann.FilterDepartmentId.HasValue)
            query = query.Where(u => u.Employee!.DepartmentId == ann.FilterDepartmentId.Value);

        if (ann.FilterGender.HasValue)
            query = query.Where(u => (int)u.Employee!.Gender == ann.FilterGender.Value);

        var recipients = await query.ToListAsync(ct);
        return recipients
            .Where(u => TelegramNotificationPreferenceHelper.IsEnabled(u, TelegramNotificationCategory.Announcement))
            .Select(u => u.TelegramChatId!)
            .ToList();
    }

    private static string BuildTelegramText(Models.Entities.ScheduledAnnouncement ann)
    {
        var scheduledLocal = ann.ScheduledAt.ToString("dd/MM/yyyy HH:mm");
        return
            $"📢 <b>Thông báo</b>\n" +
            $"━━━━━━━━━━━━━━━━━━━━━━━\n" +
            $"<b>{H(ann.Title)}</b>\n\n" +
            $"{H(ann.Content)}\n\n" +
            $"🌻🌼🌷🪻🥀🪻🌷🌼🌻";
    }

    private static string H(string s) => s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
}
