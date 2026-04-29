using System.Text.Json;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
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
    private static readonly Dictionary<string, string> ActionLabelMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Tu choi dang nhap Google"] = "Từ chối đăng nhập Google",
        ["Dang nhap Google thanh cong"] = "Đăng nhập Google thành công",
        ["Dang nhap dev thanh cong"] = "Đăng nhập dev thành công",
        ["Dang xuat"] = "Đăng xuất",
        ["Cap nhat nguoi duyet 1 theo phong ban"] = "Cập nhật người duyệt 1 theo phòng ban",
        ["Cap nhat vai tro nguoi dung"] = "Cập nhật vai trò người dùng",
        ["Cap quyen Admin"] = "Cấp quyền Admin",
        ["Xem nhat ky he thong"] = "Xem nhật ký hệ thống",
        ["Dang dau sao Ticket"] = "Đánh dấu sao Ticket",
        ["Bo danh dau sao Ticket"] = "Bỏ đánh dấu sao Ticket",
        ["Create"] = "Tạo mới",
        ["Update"] = "Cập nhật",
        ["Delete"] = "Xóa",
        ["Archive"] = "Lưu trữ",
        ["Restore"] = "Khôi phục",
        ["AddMember"] = "Thêm thành viên",
        ["TaskUpdate"] = "Cập nhật công việc"
    };

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
        var httpContext = _httpContextAccessor.HttpContext;
        var normalizedAction = NormalizeActionLabel(action, httpContext);
        var safeDataAfter = dataAfter;

        if (dataBefore == null && safeDataAfter == null)
        {
            safeDataAfter = BuildRequestFallbackData(httpContext);
        }

        var log = new SystemLog
        {
            UserId = userId,
            Action = normalizedAction,
            Module = module,
            DataBefore = dataBefore != null ? JsonSerializer.Serialize(dataBefore, LogSerializerOptions) : null,
            DataAfter = safeDataAfter != null ? JsonSerializer.Serialize(safeDataAfter, LogSerializerOptions) : null,
            IPAddress = ipAddress,
            CreatedDate = DateTimeHelper.VietnamNow
        };

        await _unitOfWork.SystemLogs.AddAsync(log);
        await _unitOfWork.SaveChangesAsync();
        SystemLogRequestContext.MarkWritten(httpContext);
    }

    private static object? BuildRequestFallbackData(HttpContext? httpContext)
    {
        if (httpContext == null)
            return null;

        return new
        {
            Method = httpContext.Request.Method,
            Path = httpContext.Request.Path.Value,
            QueryString = httpContext.Request.QueryString.HasValue ? httpContext.Request.QueryString.Value : null,
            UserAgent = httpContext.Request.Headers.UserAgent.ToString()
        };
    }

    private static string NormalizeActionLabel(string action, HttpContext? httpContext)
    {
        var trimmed = (action ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
            return "Hành động không xác định";

        if (ActionLabelMap.TryGetValue(trimmed, out var mapped))
            return mapped;

        if (Regex.IsMatch(trimmed, "^[A-Za-z]+/[A-Za-z]+$"))
        {
            var verb = httpContext?.Request.Method?.ToUpperInvariant() switch
            {
                "GET" => "Xem",
                "POST" => "Thực hiện",
                "PUT" => "Cập nhật",
                "PATCH" => "Cập nhật",
                "DELETE" => "Xóa",
                _ => "Thao tác"
            };

            return $"{verb} {trimmed}";
        }

        return trimmed;
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
