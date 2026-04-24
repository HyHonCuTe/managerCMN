using managerCMN.Models.Entities;
using managerCMN.Models.Enums;

namespace managerCMN.Services.Interfaces;

public interface INotificationService
{
    Task<IEnumerable<Notification>> GetByUserAsync(int userId);
    Task<IEnumerable<Notification>> GetUnreadByUserAsync(int userId);
    Task<int> GetUnreadCountAsync(int userId);
    Task<int> GetAllUnreadCountAsync();
    Task<IEnumerable<Notification>> GetAllAsync();
    Task CreateAsync(int userId, string title, string message, string? targetUrl = null, bool isPersonal = true, string? telegramText = null, TelegramNotificationCategory telegramCategory = TelegramNotificationCategory.General);
    Task<string?> TryOpenAsync(int notificationId, int currentUserId, bool canViewAll);
    Task<bool> TryMarkAsReadAsync(int notificationId, int currentUserId, bool canViewAll);
    Task MarkAllAsReadAsync(int userId);
    Task MarkAllAsReadAsync();
}
