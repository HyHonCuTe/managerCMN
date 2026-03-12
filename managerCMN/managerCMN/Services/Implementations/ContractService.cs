using managerCMN.Models.Entities;
using managerCMN.Repositories.Interfaces;
using managerCMN.Services.Interfaces;

namespace managerCMN.Services.Implementations;

public class ContractService : IContractService
{
    private readonly IUnitOfWork _unitOfWork;

    public ContractService(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    public async Task<IEnumerable<Contract>> GetAllAsync()
        => await _unitOfWork.Contracts.GetAllAsync();

    public async Task<Contract?> GetByIdAsync(int id)
        => await _unitOfWork.Contracts.GetByIdAsync(id);

    public async Task<IEnumerable<Contract>> GetByEmployeeAsync(int employeeId)
        => await _unitOfWork.Contracts.GetByEmployeeAsync(employeeId);

    public async Task<IEnumerable<Contract>> GetExpiringContractsAsync(int daysBeforeExpiry = 30)
        => await _unitOfWork.Contracts.GetExpiringContractsAsync(daysBeforeExpiry);

    public async Task CreateAsync(Contract contract)
    {
        await _unitOfWork.Contracts.AddAsync(contract);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task UpdateAsync(Contract contract)
    {
        _unitOfWork.Contracts.Update(contract);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var contract = await _unitOfWork.Contracts.GetByIdAsync(id);
        if (contract != null)
        {
            _unitOfWork.Contracts.Remove(contract);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}
