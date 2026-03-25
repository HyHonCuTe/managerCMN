using managerCMN.Models.Entities;

namespace managerCMN.Repositories.Interfaces;

public interface IPunchRecordRepository : IRepository<PunchRecord>
{
    /// <summary>
    /// Get all punch records for a specific employee and date, ordered by sequence number
    /// </summary>
    Task<IEnumerable<PunchRecord>> GetByEmployeeAndDateAsync(int employeeId, DateOnly date);

    /// <summary>
    /// Get all punch records for a specific employee within a date range
    /// </summary>
    Task<IEnumerable<PunchRecord>> GetByEmployeeAndDateRangeAsync(int employeeId, DateOnly startDate, DateOnly endDate);

    /// <summary>
    /// Get the maximum sequence number for a specific employee and date (returns 0 if no records exist)
    /// </summary>
    Task<int> GetMaxSequenceNumberAsync(int employeeId, DateOnly date);

    /// <summary>
    /// Check if a punch record with the exact punch time already exists (for duplicate detection)
    /// </summary>
    Task<bool> ExistsAsync(int employeeId, DateOnly date, TimeOnly punchTime);
}
