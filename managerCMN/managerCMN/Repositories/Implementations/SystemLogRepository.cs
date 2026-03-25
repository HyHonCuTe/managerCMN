using Microsoft.EntityFrameworkCore;
using managerCMN.Data;
using managerCMN.Models.Entities;
using managerCMN.Repositories.Interfaces;

namespace managerCMN.Repositories.Implementations;

public class SystemLogRepository : Repository<SystemLog>, ISystemLogRepository
{
    public SystemLogRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IEnumerable<SystemLog>> GetByModuleAsync(string module)
        => await _dbSet.Where(sl => sl.Module == module)
            .OrderByDescending(sl => sl.CreatedDate)
            .ToListAsync();

    public async Task<IEnumerable<SystemLog>> GetByUserAsync(int userId)
        => await _dbSet.Where(sl => sl.UserId == userId)
            .OrderByDescending(sl => sl.CreatedDate)
            .ToListAsync();

    public async Task<IEnumerable<SystemLog>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
        => await _dbSet
            .Where(sl => sl.CreatedDate >= startDate && sl.CreatedDate <= endDate)
            .OrderByDescending(sl => sl.CreatedDate)
            .ToListAsync();

    // Override GetAllAsync to ensure latest logs appear first
    public override async Task<IEnumerable<SystemLog>> GetAllAsync()
        => await _dbSet
            .OrderByDescending(sl => sl.CreatedDate)
            .ToListAsync();
}
