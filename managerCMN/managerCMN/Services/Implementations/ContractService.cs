using System.Security.Claims;
using managerCMN.Helpers;
using managerCMN.Models.Entities;
using managerCMN.Models.Enums;
using managerCMN.Repositories.Interfaces;
using managerCMN.Services.Interfaces;
using Microsoft.AspNetCore.Http;

namespace managerCMN.Services.Implementations;

public class ContractService : IContractService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISystemLogService _logService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ContractService(IUnitOfWork unitOfWork, ISystemLogService logService, IHttpContextAccessor httpContextAccessor)
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

        await _logService.LogAsync(
            GetCurrentUserId(),
            "Tạo mới Hợp đồng",
            "Contract",
            null,
            new { contract.ContractId, contract.ContractNumber, contract.EmployeeId, contract.ContractType, contract.StartDate, contract.EndDate, contract.Salary },
            GetClientIP()
        );
    }

    public async Task UpdateAsync(Contract contract)
    {
        var existing = await _unitOfWork.Contracts.GetByIdAsync(contract.ContractId);
        var dataBefore = existing != null ? new { existing.ContractId, existing.ContractNumber, existing.EmployeeId, existing.ContractType, existing.StartDate, existing.EndDate, existing.Salary, existing.Status } : null;

        contract.ModifiedAt = DateTime.UtcNow;
        _unitOfWork.Contracts.Update(contract);
        await _unitOfWork.SaveChangesAsync();

        await _logService.LogAsync(
            GetCurrentUserId(),
            "Cập nhật Hợp đồng",
            "Contract",
            dataBefore,
            new { contract.ContractId, contract.ContractNumber, contract.EmployeeId, contract.ContractType, contract.StartDate, contract.EndDate, contract.Salary, contract.Status },
            GetClientIP()
        );
    }

    public async Task DeleteAsync(int id)
    {
        var contract = await _unitOfWork.Contracts.GetByIdAsync(id);
        if (contract != null)
        {
            var dataBefore = new { contract.ContractId, contract.ContractNumber, contract.EmployeeId, contract.ContractType, contract.StartDate, contract.EndDate };

            _unitOfWork.Contracts.Remove(contract);
            await _unitOfWork.SaveChangesAsync();

            await _logService.LogAsync(
                GetCurrentUserId(),
                "Xóa Hợp đồng",
                "Contract",
                dataBefore,
                null,
                GetClientIP()
            );
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
        {
            await _unitOfWork.SaveChangesAsync();
            await _logService.LogAsync(
                GetCurrentUserId(),
                "Dong bo hop dong het han",
                "Contract",
                null,
                new
                {
                    UpdatedCount = expired.Count(),
                    ContractIds = expired.Select(c => c.ContractId).ToArray(),
                    Date = today
                },
                GetClientIP());
        }
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
        await _logService.LogAsync(
            GetCurrentUserId(),
            "Sinh ma hop dong cu thieu du lieu",
            "Contract",
            null,
            new
            {
                UpdatedCount = contractsWithEmptyNumber.Count(),
                Contracts = contractsWithEmptyNumber
                    .Select(contract => new
                    {
                        contract.ContractId,
                        contract.ContractNumber
                    })
                    .ToArray()
            },
            GetClientIP());
    }
}
