using Microsoft.EntityFrameworkCore;
using managerCMN.Data;
using managerCMN.Models.Entities;
using managerCMN.Repositories.Interfaces;

namespace managerCMN.Repositories.Implementations;

public class PunchRecordRepository : Repository<PunchRecord>, IPunchRecordRepository
{
    public PunchRecordRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IEnumerable<PunchRecord>> GetByEmployeeAndDateAsync(int employeeId, DateOnly date)
    {
        return await _context.PunchRecords
            .Where(pr => pr.EmployeeId == employeeId && pr.Date == date)
            .OrderBy(pr => pr.SequenceNumber)
            .ToListAsync();
    }

    public async Task<IEnumerable<PunchRecord>> GetByEmployeeAndDateRangeAsync(int employeeId, DateOnly startDate, DateOnly endDate)
    {
        return await _context.PunchRecords
            .Where(pr => pr.EmployeeId == employeeId && pr.Date >= startDate && pr.Date <= endDate)
            .OrderBy(pr => pr.Date)
            .ThenBy(pr => pr.SequenceNumber)
            .ToListAsync();
    }

    public async Task<int> GetMaxSequenceNumberAsync(int employeeId, DateOnly date)
    {
        var max = await _context.PunchRecords
            .Where(pr => pr.EmployeeId == employeeId && pr.Date == date)
            .MaxAsync(pr => (int?)pr.SequenceNumber);

        return max ?? 0;
    }

    public async Task<bool> ExistsAsync(int employeeId, DateOnly date, TimeOnly punchTime)
    {
        return await _context.PunchRecords
            .AnyAsync(pr => pr.EmployeeId == employeeId
                         && pr.Date == date
                         && pr.PunchTime == punchTime);
    }
}
