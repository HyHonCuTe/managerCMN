using Microsoft.EntityFrameworkCore;
using managerCMN.Data;
using managerCMN.Models.Entities;
using managerCMN.Repositories.Interfaces;

namespace managerCMN.Repositories.Implementations;

public class HolidayRepository : Repository<Holiday>, IHolidayRepository
{
    public HolidayRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IEnumerable<Holiday>> GetByDateRangeAsync(DateOnly startDate, DateOnly endDate)
        => await _dbSet
            .Where(h => h.IsActive && h.Date >= startDate && h.Date <= endDate)
            .OrderBy(h => h.Date)
            .ToListAsync();

    public async Task<Holiday?> GetByDateAsync(DateOnly date)
        => await _dbSet.FirstOrDefaultAsync(h => h.IsActive && h.Date == date);

    public async Task<bool> IsHolidayAsync(DateOnly date)
        => await _dbSet.AnyAsync(h => h.IsActive && h.Date == date);
}