using managerCMN.Models.Entities;
using managerCMN.Repositories.Interfaces;
using managerCMN.Services.Interfaces;

namespace managerCMN.Services.Implementations;

public class NotificationService : INotificationService
{
    private readonly IUnitOfWork _unitOfWork;

    public NotificationService(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    public async Task<IEnumerable<Notification>> GetByUserAsync(int userId)
        => await _unitOfWork.Notifications.GetByUserAsync(userId);

    public async Task<IEnumerable<Notification>> GetUnreadByUserAsync(int userId)
        => await _unitOfWork.Notifications.GetUnreadByUserAsync(userId);

    public async Task<int> GetUnreadCountAsync(int userId)
        => await _unitOfWork.Notifications.GetUnreadCountAsync(userId);

    public async Task<IEnumerable<Notification>> GetAllAsync()
        => await _unitOfWork.Notifications.GetAllAsync();

    public async Task CreateAsync(int userId, string title, string message)
    {
        var notification = new Notification
        {
            UserId = userId,
            Title = title,
            Message = message,
            IsRead = false,
            CreatedDate = DateTime.UtcNow
        };
        await _unitOfWork.Notifications.AddAsync(notification);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task MarkAsReadAsync(int notificationId)
    {
        var notification = await _unitOfWork.Notifications.GetByIdAsync(notificationId);
        if (notification == null) return;

        notification.IsRead = true;
        _unitOfWork.Notifications.Update(notification);
        await _unitOfWork.SaveChangesAsync();
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
