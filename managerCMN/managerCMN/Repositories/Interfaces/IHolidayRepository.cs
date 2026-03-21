using managerCMN.Models.Entities;

namespace managerCMN.Repositories.Interfaces;

public interface IHolidayRepository : IRepository<Holiday>
{
    Task<IEnumerable<Holiday>> GetByDateRangeAsync(DateOnly startDate, DateOnly endDate);
    Task<Holiday?> GetByDateAsync(DateOnly date);
    Task<bool> IsHolidayAsync(DateOnly date);
}