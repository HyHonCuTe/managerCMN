using Microsoft.EntityFrameworkCore;
using managerCMN.Data;
using managerCMN.Models.Entities;
using managerCMN.Repositories.Interfaces;

namespace managerCMN.Repositories.Implementations;

public class NotificationRepository : Repository<Notification>, INotificationRepository
{
    public NotificationRepository(ApplicationDbContext context) : base(context) { }

    public override async Task<IEnumerable<Notification>> GetAllAsync()
        => await _dbSet.OrderByDescending(n => n.CreatedDate).ToListAsync();

    public async Task<IEnumerable<Notification>> GetByUserAsync(int userId)
        => await _dbSet.Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedDate)
            .ToListAsync();

    public async Task<IEnumerable<Notification>> GetUnreadByUserAsync(int userId)
        => await _dbSet.Where(n => n.UserId == userId && !n.IsRead)
            .OrderByDescending(n => n.CreatedDate)
            .ToListAsync();

    public async Task<int> GetUnreadCountAsync(int userId)
        => await _dbSet.CountAsync(n => n.UserId == userId && !n.IsRead);
}
