using managerCMN.Models.Entities;

namespace managerCMN.Services.Interfaces;

public interface INotificationService
{
    Task<IEnumerable<Notification>> GetByUserAsync(int userId);
    Task<IEnumerable<Notification>> GetUnreadByUserAsync(int userId);
    Task<int> GetUnreadCountAsync(int userId);
    Task<IEnumerable<Notification>> GetAllAsync(); // For Admin to view all notifications
    Task CreateAsync(int userId, string title, string message);
    Task MarkAsReadAsync(int notificationId);
    Task MarkAllAsReadAsync(int userId);
    Task MarkAllAsReadAsync(); // For Admin to mark all notifications as read
}
