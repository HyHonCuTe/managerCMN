using managerCMN.Models.Entities;
using managerCMN.Repositories.Interfaces;
using managerCMN.Services.Interfaces;

namespace managerCMN.Services.Implementations;

public class HolidayService : IHolidayService
{
    private readonly IUnitOfWork _unitOfWork;

    public HolidayService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

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
        return holiday;
    }

    public async Task UpdateAsync(Holiday holiday)
    {
        _unitOfWork.Holidays.Update(holiday);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task DeleteAsync(int holidayId)
    {
        var holiday = await _unitOfWork.Holidays.GetByIdAsync(holidayId);
        if (holiday != null)
        {
            _unitOfWork.Holidays.Remove(holiday);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}