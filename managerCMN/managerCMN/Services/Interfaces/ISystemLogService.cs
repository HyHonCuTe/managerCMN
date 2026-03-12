namespace managerCMN.Services.Interfaces;

public interface ISystemLogService
{
    Task LogAsync(int? userId, string action, string? module, object? dataBefore, object? dataAfter, string? ipAddress);
    Task<IEnumerable<Models.Entities.SystemLog>> GetByModuleAsync(string module);
    Task<IEnumerable<Models.Entities.SystemLog>> GetByUserAsync(int userId);
    Task<IEnumerable<Models.Entities.SystemLog>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<IEnumerable<Models.Entities.SystemLog>> GetAllAsync();
}
