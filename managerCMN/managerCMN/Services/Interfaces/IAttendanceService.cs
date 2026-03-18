using managerCMN.Models.Entities;

namespace managerCMN.Services.Interfaces;

public interface IAttendanceService
{
    Task<IEnumerable<Attendance>> GetByEmployeeAndMonthAsync(int employeeId, int year, int month);
    Task<IEnumerable<Attendance>> GetByEmployeeAndDateRangeAsync(int employeeId, DateOnly startDate, DateOnly endDate);
    Task<IEnumerable<Attendance>> GetByDateRangeAsync(DateOnly startDate, DateOnly endDate);
    Task<IEnumerable<Attendance>> GetLateCheckInsAsync(int year, int month);
    Task ImportFromExcelAsync(Stream excelStream);
    Task ProcessPunchRecordsAsync(IEnumerable<(string AttendanceCode, DateTime PunchTime)> punchRecords);
    Task<Attendance?> GetByIdAsync(int id);
    Task<byte[]> ExportToExcelAsync(int year, int month);
}
