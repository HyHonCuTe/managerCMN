using managerCMN.Models.Entities;

namespace managerCMN.Repositories.Interfaces;

public interface INotificationRepository : IRepository<Notification>
{
    Task<IEnumerable<Notification>> GetByUserAsync(int userId);
    Task<IEnumerable<Notification>> GetUnreadByUserAsync(int userId);
    Task<int> GetUnreadCountAsync(int userId);
}
