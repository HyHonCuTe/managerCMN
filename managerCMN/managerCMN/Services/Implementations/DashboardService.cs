using managerCMN.Models.Entities;
using managerCMN.Models.Enums;
using managerCMN.Repositories.Interfaces;
using managerCMN.Services.Interfaces;

namespace managerCMN.Services.Implementations;

public class DashboardService : IDashboardService
{
    private readonly IUnitOfWork _unitOfWork;

    public DashboardService(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    public async Task<int> GetTotalEmployeesAsync()
        => await _unitOfWork.Employees.CountAsync(e => e.Status == EmployeeStatus.Active);

    public async Task<int> GetPendingRequestsCountAsync()
        => await _unitOfWork.Requests.CountAsync(r =>
            r.Status == RequestStatus.Pending || r.Status == RequestStatus.Approver1Approved);

    public async Task<int> GetActiveTicketsCountAsync()
        => await _unitOfWork.Tickets.CountAsync(t =>
            t.Status == TicketStatus.Open || t.Status == TicketStatus.InProgress);

    public async Task<int> GetTotalAssetsAsync()
        => await _unitOfWork.Assets.CountAsync();

    public async Task<IEnumerable<Contract>> GetExpiringContractsAsync()
        => await _unitOfWork.Contracts.GetExpiringContractsAsync(30);
}
