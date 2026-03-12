using System.Text.Json;
using managerCMN.Models.Entities;
using managerCMN.Repositories.Interfaces;
using managerCMN.Services.Interfaces;

namespace managerCMN.Services.Implementations;

public class SystemLogService : ISystemLogService
{
    private readonly IUnitOfWork _unitOfWork;

    public SystemLogService(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    public async Task LogAsync(int? userId, string action, string? module, object? dataBefore, object? dataAfter, string? ipAddress)
    {
        var log = new SystemLog
        {
            UserId = userId,
            Action = action,
            Module = module,
            DataBefore = dataBefore != null ? JsonSerializer.Serialize(dataBefore) : null,
            DataAfter = dataAfter != null ? JsonSerializer.Serialize(dataAfter) : null,
            IPAddress = ipAddress,
            CreatedDate = DateTime.UtcNow
        };

        await _unitOfWork.SystemLogs.AddAsync(log);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<IEnumerable<SystemLog>> GetByModuleAsync(string module)
        => await _unitOfWork.SystemLogs.GetByModuleAsync(module);

    public async Task<IEnumerable<SystemLog>> GetByUserAsync(int userId)
        => await _unitOfWork.SystemLogs.GetByUserAsync(userId);

    public async Task<IEnumerable<SystemLog>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
        => await _unitOfWork.SystemLogs.GetByDateRangeAsync(startDate, endDate);

    public async Task<IEnumerable<SystemLog>> GetAllAsync()
        => await _unitOfWork.SystemLogs.GetAllAsync();
}
