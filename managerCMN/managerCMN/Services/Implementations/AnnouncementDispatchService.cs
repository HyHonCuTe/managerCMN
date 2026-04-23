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

            return await db.Users
                .AsNoTracking()
                .Where(u => u.IsActive
                    && u.TelegramChatId != null
                    && !u.TelegramMuteBroadcast
                    && u.EmployeeId != null
                    && ids.Contains(u.EmployeeId!.Value))
                .Select(u => u.TelegramChatId!)
                .ToListAsync(ct);
        }

        // Broad filter: all active employees, optionally narrowed by department/gender
        var query = db.Users
            .AsNoTracking()
            .Where(u => u.IsActive && u.TelegramChatId != null && !u.TelegramMuteBroadcast && u.EmployeeId != null)
            .Join(db.Employees.AsNoTracking().Where(e => e.Status == EmployeeStatus.Active),
                  u => u.EmployeeId,
                  e => e.EmployeeId,
                  (u, e) => new { u.TelegramChatId, e.DepartmentId, GenderVal = (int)e.Gender });

        if (ann.FilterDepartmentId.HasValue)
            query = query.Where(x => x.DepartmentId == ann.FilterDepartmentId.Value);

        if (ann.FilterGender.HasValue)
            query = query.Where(x => x.GenderVal == ann.FilterGender.Value);

        return await query.Select(x => x.TelegramChatId!).ToListAsync(ct);
    }

    private static string BuildTelegramText(Models.Entities.ScheduledAnnouncement ann)
    {
        var scheduledLocal = ann.ScheduledAt.ToString("dd/MM/yyyy HH:mm");
        return
            $"📢 <b>Thông báo nội bộ</b>\n" +
            $"━━━━━━━━━━━━━━━━━━\n" +
            $"<b>{H(ann.Title)}</b>\n\n" +
            $"{H(ann.Content)}\n\n" +
            $"<i>🕐 Gửi lúc: {scheduledLocal}</i>";
    }

    private static string H(string s) => s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
}
