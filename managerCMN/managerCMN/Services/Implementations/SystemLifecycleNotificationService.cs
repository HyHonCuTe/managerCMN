using managerCMN.Data;
using managerCMN.Helpers;
using managerCMN.Models.Enums;
using managerCMN.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace managerCMN.Services.Implementations;

public class SystemLifecycleNotificationService : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ITelegramService _telegram;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ILogger<SystemLifecycleNotificationService> _logger;

    // Cached at startup; refreshed on shutdown for accuracy
    private List<string> _cachedChatIds = new();

    public SystemLifecycleNotificationService(
        IServiceScopeFactory scopeFactory,
        ITelegramService telegram,
        IHostApplicationLifetime lifetime,
        ILogger<SystemLifecycleNotificationService> logger)
    {
        _scopeFactory = scopeFactory;
        _telegram = telegram;
        _lifetime = lifetime;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // ApplicationStarted fires after the app is fully ready (HTTP server listening)
        _lifetime.ApplicationStarted.Register(() => _ = NotifyStartedAsync());
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (!_telegram.IsConfigured) return;

        // Refresh list in case users linked/unlinked since startup
        try { _cachedChatIds = await LoadChatIdsAsync(); }
        catch { /* fall back to list cached at startup */ }

        if (_cachedChatIds.Count == 0) return;

        const string msg =
            "🔧 <b>Hệ thống đang cập nhật</b>\n" +
            "Dịch vụ tạm thời ngừng hoạt động.\n" +
            "Vui lòng chờ, hệ thống sẽ quay lại ngay sau khi cập nhật xong.";

        await BroadcastAsync(_cachedChatIds, msg);
    }

    private async Task NotifyStartedAsync()
    {
        if (!_telegram.IsConfigured) return;

        try
        {
            _cachedChatIds = await LoadChatIdsAsync();
            if (_cachedChatIds.Count == 0) return;

            const string msg =
                "✅ <b>Hệ thống đã sẵn sàng</b>\n" +
                "Cập nhật hoàn tất, bạn có thể sử dụng bình thường.";

            await BroadcastAsync(_cachedChatIds, msg);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send system-started Telegram notifications");
        }
    }

    private async Task BroadcastAsync(IReadOnlyList<string> chatIds, string text)
    {
        var sends = chatIds.Select(async chatId =>
        {
            try { await _telegram.SendMessageAsync(chatId, text); }
            catch { /* ignore per-user send failures */ }
        });
        await Task.WhenAll(sends);
    }

    private async Task<List<string>> LoadChatIdsAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var users = await db.Users
            .AsNoTracking()
            .Where(u => !string.IsNullOrEmpty(u.TelegramChatId))
            .ToListAsync();

        return users
            .Where(u => TelegramNotificationPreferenceHelper.IsEnabled(u, TelegramNotificationCategory.SystemLifecycle))
            .Select(u => u.TelegramChatId!)
            .ToList();
    }
}
