using managerCMN.Helpers;
using managerCMN.Models.Entities;
using managerCMN.Models.Enums;
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
        contract.ModifiedAt = DateTime.UtcNow;
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

    public async Task SyncExpiredAsync()
    {
        var today = DateTimeHelper.VietnamToday;
        var expired = await _unitOfWork.Contracts.FindAsync(c =>
            c.Status == ContractStatus.Active &&
            c.EndDate != null &&
            c.EndDate.Value.Date < today);

        foreach (var c in expired)
            c.Status = ContractStatus.Expired;

        if (expired.Any())
            await _unitOfWork.SaveChangesAsync();
    }

    public async Task<bool> IsContractNumberUniqueAsync(string contractNumber, int? excludeContractId = null)
    {
        var existing = await _unitOfWork.Contracts.FindAsync(c =>
            c.ContractNumber.ToLower() == contractNumber.ToLower());

        if (excludeContractId.HasValue)
            existing = existing.Where(c => c.ContractId != excludeContractId.Value);

        return !existing.Any();
    }

    public async Task FixEmptyContractNumbersAsync()
    {
        var contractsWithEmptyNumber = await _unitOfWork.Contracts.FindAsync(c =>
            string.IsNullOrEmpty(c.ContractNumber));

        if (!contractsWithEmptyNumber.Any())
            return;

        foreach (var contract in contractsWithEmptyNumber)
        {
            contract.ContractNumber = $"LEGACY-{contract.ContractId}-{contract.StartDate.Year}";
            _unitOfWork.Contracts.Update(contract);
        }

        await _unitOfWork.SaveChangesAsync();
    }
}
