using managerCMN.Models.Entities;

namespace managerCMN.Services.Interfaces;

public interface IHolidayService
{
    Task<IEnumerable<Holiday>> GetByDateRangeAsync(DateOnly startDate, DateOnly endDate);
    Task<Holiday?> GetByDateAsync(DateOnly date);
    Task<bool> IsHolidayAsync(DateOnly date);
    Task<Holiday> CreateAsync(Holiday holiday);
    Task UpdateAsync(Holiday holiday);
    Task DeleteAsync(int holidayId);
}