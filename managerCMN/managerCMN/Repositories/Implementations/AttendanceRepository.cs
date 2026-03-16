using Microsoft.EntityFrameworkCore;
using managerCMN.Data;
using managerCMN.Models.Entities;
using managerCMN.Repositories.Interfaces;

namespace managerCMN.Repositories.Implementations;

public class AttendanceRepository : Repository<Attendance>, IAttendanceRepository
{
    public AttendanceRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IEnumerable<Attendance>> GetByEmployeeAndMonthAsync(int employeeId, int year, int month)
        => await _dbSet
            .Where(a => a.EmployeeId == employeeId
                && a.Date.Year == year
                && a.Date.Month == month)
            .OrderBy(a => a.Date)
            .ToListAsync();

    public async Task<IEnumerable<Attendance>> GetByEmployeeAndDateRangeAsync(int employeeId, DateOnly startDate, DateOnly endDate)
        => await _dbSet
            .Where(a => a.EmployeeId == employeeId && a.Date >= startDate && a.Date <= endDate)
            .OrderBy(a => a.Date)
            .ToListAsync();

    public async Task<IEnumerable<Attendance>> GetByDateRangeAsync(DateOnly startDate, DateOnly endDate)
        => await _dbSet
            .Include(a => a.Employee)
            .Where(a => a.Date >= startDate && a.Date <= endDate)
            .OrderBy(a => a.Date)
            .ToListAsync();

    public async Task<IEnumerable<Attendance>> GetLateCheckInsAsync(int year, int month, TimeOnly lateThreshold)
        => await _dbSet
            .Include(a => a.Employee)
            .Where(a => a.Date.Year == year
                && a.Date.Month == month
                && a.CheckIn != null
                && a.CheckIn > lateThreshold)
            .OrderBy(a => a.Date)
            .ToListAsync();
}
