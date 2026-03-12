using managerCMN.Models.Entities;

namespace managerCMN.Services.Interfaces;

public interface IContractService
{
    Task<IEnumerable<Contract>> GetAllAsync();
    Task<Contract?> GetByIdAsync(int id);
    Task<IEnumerable<Contract>> GetByEmployeeAsync(int employeeId);
    Task<IEnumerable<Contract>> GetExpiringContractsAsync(int daysBeforeExpiry = 30);
    Task CreateAsync(Contract contract);
    Task UpdateAsync(Contract contract);
    Task DeleteAsync(int id);
}
