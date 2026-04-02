using System.Text.Json;
using System.Text.Encodings.Web;
using managerCMN.Models.Entities;
using managerCMN.Repositories.Interfaces;
using managerCMN.Services.Interfaces;
using managerCMN.Helpers;
using Microsoft.AspNetCore.Http;

namespace managerCMN.Services.Implementations;

public class SystemLogService : ISystemLogService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private static readonly JsonSerializerOptions LogSerializerOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public SystemLogService(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor)
    {
        _unitOfWork = unitOfWork;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task LogAsync(int? userId, string action, string? module, object? dataBefore, object? dataAfter, string? ipAddress)
    {
        var log = new SystemLog
        {
            UserId = userId,
            Action = action,
            Module = module,
            DataBefore = dataBefore != null ? JsonSerializer.Serialize(dataBefore, LogSerializerOptions) : null,
            DataAfter = dataAfter != null ? JsonSerializer.Serialize(dataAfter, LogSerializerOptions) : null,
            IPAddress = ipAddress,
            CreatedDate = VietnamTimeHelper.Now
        };

        await _unitOfWork.SystemLogs.AddAsync(log);
        await _unitOfWork.SaveChangesAsync();
        SystemLogRequestContext.MarkWritten(_httpContextAccessor.HttpContext);
    }

    public async Task<IEnumerable<SystemLog>> GetByModuleAsync(string module)
        => await _unitOfWork.SystemLogs.GetByModuleAsync(module);

    public async Task<IEnumerable<SystemLog>> GetByUserAsync(int userId)
        => await _unitOfWork.SystemLogs.GetByUserAsync(userId);

    public async Task<IEnumerable<SystemLog>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
        => await _unitOfWork.SystemLogs.GetByDateRangeAsync(startDate, endDate);

    public async Task<IEnumerable<SystemLog>> GetAllAsync()
        => await _unitOfWork.SystemLogs.GetAllAsync();

    public async Task<IReadOnlyList<SystemLog>> SearchAsync(
        string? module,
        string? logAction,
        DateTime? startDate,
        DateTime? endDate)
        => await _unitOfWork.SystemLogs.SearchAsync(module, logAction, startDate, endDate);
}
