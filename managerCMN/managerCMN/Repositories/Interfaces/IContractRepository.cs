using managerCMN.Models.Entities;

namespace managerCMN.Repositories.Interfaces;

public interface IContractRepository : IRepository<Contract>
{
    Task<IEnumerable<Contract>> GetExpiringContractsAsync(int daysBeforeExpiry = 30);
    Task<IEnumerable<Contract>> GetByEmployeeAsync(int employeeId);
}
