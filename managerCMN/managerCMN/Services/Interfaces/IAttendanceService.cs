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

    /// <summary>
    /// Get all punch records for a specific employee and date
    /// </summary>
    Task<IEnumerable<PunchRecord>> GetPunchRecordsByDateAsync(int employeeId, DateOnly date);

    /// <summary>
    /// Updates LateMinutes for existing attendance records that have IsLate = true but LateMinutes = 0
    /// </summary>
    /// <returns>Number of records updated</returns>
    Task<int> UpdateExistingLateMinutesAsync();
}
