using managerCMN.Models.Entities;

namespace managerCMN.Repositories.Interfaces;

public interface IAttendanceRepository : IRepository<Attendance>
{
    Task<IEnumerable<Attendance>> GetByEmployeeAndMonthAsync(int employeeId, int year, int month);
    Task<IEnumerable<Attendance>> GetByEmployeeAndDateRangeAsync(int employeeId, DateOnly startDate, DateOnly endDate);
    Task<IEnumerable<Attendance>> GetByDateRangeAsync(DateOnly startDate, DateOnly endDate);
    Task<IEnumerable<Attendance>> GetLateCheckInsAsync(int year, int month, TimeOnly lateThreshold);
}
