using managerCMN.Models.Entities;

namespace managerCMN.Repositories.Interfaces;

public interface ISystemLogRepository : IRepository<SystemLog>
{
    Task<IEnumerable<SystemLog>> GetByModuleAsync(string module);
    Task<IEnumerable<SystemLog>> GetByUserAsync(int userId);
    Task<IEnumerable<SystemLog>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<IReadOnlyList<SystemLog>> SearchAsync(
        string? module,
        string? logAction,
        DateTime? startDate,
        DateTime? endDate);
}
