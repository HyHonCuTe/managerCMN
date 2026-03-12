using managerCMN.Models.Entities;

namespace managerCMN.Services.Interfaces;

public interface IDashboardService
{
    Task<int> GetTotalEmployeesAsync();
    Task<int> GetPendingRequestsCountAsync();
    Task<int> GetActiveTicketsCountAsync();
    Task<int> GetTotalAssetsAsync();
    Task<IEnumerable<Contract>> GetExpiringContractsAsync();
}
