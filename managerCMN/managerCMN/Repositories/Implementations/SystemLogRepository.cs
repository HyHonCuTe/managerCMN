using Microsoft.EntityFrameworkCore;
using managerCMN.Data;
using managerCMN.Models.Entities;
using managerCMN.Repositories.Interfaces;

namespace managerCMN.Repositories.Implementations;

public class SystemLogRepository : Repository<SystemLog>, ISystemLogRepository
{
    public SystemLogRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IEnumerable<SystemLog>> GetByModuleAsync(string module)
        => await _dbSet.AsNoTracking().Where(sl => sl.Module == module)
            .OrderByDescending(sl => sl.CreatedDate)
            .ToListAsync();

    public async Task<IEnumerable<SystemLog>> GetByUserAsync(int userId)
        => await _dbSet.AsNoTracking().Where(sl => sl.UserId == userId)
            .OrderByDescending(sl => sl.CreatedDate)
            .ToListAsync();

    public async Task<IEnumerable<SystemLog>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
        => await _dbSet.AsNoTracking()
            .Where(sl => sl.CreatedDate >= startDate && sl.CreatedDate <= endDate)
            .OrderByDescending(sl => sl.CreatedDate)
            .ToListAsync();

    public async Task<IReadOnlyList<SystemLog>> SearchAsync(
        string? module,
        string? logAction,
        DateTime? startDate,
        DateTime? endDate)
    {
        var query = _dbSet.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(module))
        {
            var trimmedModule = module.Trim();
            query = query.Where(sl =>
                sl.Module != null &&
                sl.Module.Contains(trimmedModule));
        }

        if (!string.IsNullOrWhiteSpace(logAction))
        {
            var trimmedAction = logAction.Trim();
            query = query.Where(sl =>
                sl.Action.Contains(trimmedAction));
        }

        if (startDate.HasValue)
        {
            query = query.Where(sl => sl.CreatedDate >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            var inclusiveEnd = endDate.Value.Date.AddDays(1);
            query = query.Where(sl => sl.CreatedDate < inclusiveEnd);
        }

        return await query
            .OrderByDescending(sl => sl.CreatedDate)
            .ToListAsync();
    }

    // Override GetAllAsync to ensure latest logs appear first
    public override async Task<IEnumerable<SystemLog>> GetAllAsync()
        => await _dbSet.AsNoTracking()
            .OrderByDescending(sl => sl.CreatedDate)
            .ToListAsync();
}
