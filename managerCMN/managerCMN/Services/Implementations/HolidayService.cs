using System.Security.Claims;
using managerCMN.Models.Entities;
using managerCMN.Repositories.Interfaces;
using managerCMN.Services.Interfaces;
using Microsoft.AspNetCore.Http;

namespace managerCMN.Services.Implementations;

public class HolidayService : IHolidayService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISystemLogService _logService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HolidayService(IUnitOfWork unitOfWork, ISystemLogService logService, IHttpContextAccessor httpContextAccessor)
    {
        _unitOfWork = unitOfWork;
        _logService = logService;
        _httpContextAccessor = httpContextAccessor;
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var id) ? id : null;
    }

    private string? GetClientIP() => _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();

    public async Task<IEnumerable<Holiday>> GetByDateRangeAsync(DateOnly startDate, DateOnly endDate)
        => await _unitOfWork.Holidays.GetByDateRangeAsync(startDate, endDate);

    public async Task<Holiday?> GetByDateAsync(DateOnly date)
        => await _unitOfWork.Holidays.GetByDateAsync(date);

    public async Task<bool> IsHolidayAsync(DateOnly date)
        => await _unitOfWork.Holidays.IsHolidayAsync(date);

    public async Task<Holiday> CreateAsync(Holiday holiday)
    {
        await _unitOfWork.Holidays.AddAsync(holiday);
        await _unitOfWork.SaveChangesAsync();

        await _logService.LogAsync(
            GetCurrentUserId(),
            "Tạo mới Ngày nghỉ lễ",
            "Holiday",
            null,
            new { holiday.HolidayId, holiday.Date, holiday.Name, holiday.Description },
            GetClientIP()
        );

        return holiday;
    }

    public async Task UpdateAsync(Holiday holiday)
    {
        var existing = await _unitOfWork.Holidays.GetByIdAsync(holiday.HolidayId);
        var dataBefore = existing != null ? new { existing.HolidayId, existing.Date, existing.Name, existing.Description } : null;

        _unitOfWork.Holidays.Update(holiday);
        await _unitOfWork.SaveChangesAsync();

        await _logService.LogAsync(
            GetCurrentUserId(),
            "Cập nhật Ngày nghỉ lễ",
            "Holiday",
            dataBefore,
            new { holiday.HolidayId, holiday.Date, holiday.Name, holiday.Description },
            GetClientIP()
        );
    }

    public async Task DeleteAsync(int holidayId)
    {
        var holiday = await _unitOfWork.Holidays.GetByIdAsync(holidayId);
        if (holiday != null)
        {
            var dataBefore = new { holiday.HolidayId, holiday.Date, holiday.Name, holiday.Description };

            _unitOfWork.Holidays.Remove(holiday);
            await _unitOfWork.SaveChangesAsync();

            await _logService.LogAsync(
                GetCurrentUserId(),
                "Xóa Ngày nghỉ lễ",
                "Holiday",
                dataBefore,
                null,
                GetClientIP()
            );
        }
    }
}
