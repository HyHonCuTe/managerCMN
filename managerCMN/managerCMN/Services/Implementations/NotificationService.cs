using managerCMN.Models.Entities;
using managerCMN.Repositories.Interfaces;
using managerCMN.Services.Interfaces;

namespace managerCMN.Services.Implementations;

public class NotificationService : INotificationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITelegramService _telegram;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(IUnitOfWork unitOfWork, ITelegramService telegram, ILogger<NotificationService> logger)
    {
        _unitOfWork = unitOfWork;
        _telegram = telegram;
        _logger = logger;
    }

    public async Task<IEnumerable<Notification>> GetByUserAsync(int userId)
        => await _unitOfWork.Notifications.GetByUserAsync(userId);

    public async Task<IEnumerable<Notification>> GetUnreadByUserAsync(int userId)
        => await _unitOfWork.Notifications.GetUnreadByUserAsync(userId);

    public async Task<int> GetUnreadCountAsync(int userId)
        => await _unitOfWork.Notifications.GetUnreadCountAsync(userId);

    public async Task<int> GetAllUnreadCountAsync()
        => await _unitOfWork.Notifications.CountAsync(n => !n.IsRead);

    public async Task<IEnumerable<Notification>> GetAllAsync()
        => await _unitOfWork.Notifications.GetAllAsync();

    public async Task CreateAsync(int userId, string title, string message, string? targetUrl = null)
    {
        var notification = new Notification
        {
            UserId = userId,
            Title = title,
            Message = message,
            TargetUrl = targetUrl,
            IsRead = false,
            CreatedDate = DateTime.UtcNow
        };
        await _unitOfWork.Notifications.AddAsync(notification);
        await _unitOfWork.SaveChangesAsync();

        if (_telegram.IsConfigured)
        {
            try
            {
                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (!string.IsNullOrWhiteSpace(user?.TelegramChatId))
                {
                    var text = $"<b>{EscapeHtml(title)}</b>\n{EscapeHtml(message)}";
                    await _telegram.SendMessageAsync(user.TelegramChatId, text);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Telegram notification failed for user {UserId}", userId);
            }
        }
    }

    private static string EscapeHtml(string text)
        => text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

    public async Task<string?> TryOpenAsync(int notificationId, int currentUserId, bool canViewAll)
    {
        var notification = await _unitOfWork.Notifications.GetByIdAsync(notificationId);
        if (notification == null)
            return null;

        if (!canViewAll && notification.UserId != currentUserId)
            return null;

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            _unitOfWork.Notifications.Update(notification);
            await _unitOfWork.SaveChangesAsync();
        }

        return notification.TargetUrl;
    }

    public async Task<bool> TryMarkAsReadAsync(int notificationId, int currentUserId, bool canViewAll)
    {
        var notification = await _unitOfWork.Notifications.GetByIdAsync(notificationId);
        if (notification == null)
        {
            return false;
        }

        if (!canViewAll && notification.UserId != currentUserId)
        {
            return false;
        }

        notification.IsRead = true;
        _unitOfWork.Notifications.Update(notification);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task MarkAllAsReadAsync(int userId)
    {
        var unread = await _unitOfWork.Notifications.GetUnreadByUserAsync(userId);
        foreach (var n in unread)
        {
            n.IsRead = true;
            _unitOfWork.Notifications.Update(n);
        }
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task MarkAllAsReadAsync()
    {
        var allUnread = await _unitOfWork.Notifications.FindAsync(n => !n.IsRead);
        foreach (var n in allUnread)
        {
            n.IsRead = true;
            _unitOfWork.Notifications.Update(n);
        }
        await _unitOfWork.SaveChangesAsync();
    }
}
