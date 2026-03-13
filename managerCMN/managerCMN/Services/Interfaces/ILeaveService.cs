using managerCMN.Models.Entities;
using managerCMN.Models.ViewModels;

namespace managerCMN.Services.Interfaces;

public interface ILeaveService
{
    Task<LeaveBalance?> GetBalanceAsync(int employeeId, int year);
    Task<LeaveBalanceSummaryViewModel> GetBalanceSummaryAsync(int employeeId, DateTime? asOfDate = null);
    Task<IReadOnlyDictionary<int, LeaveBalanceSummaryViewModel>> GetBalanceSummariesAsync(IEnumerable<int> employeeIds, DateTime? asOfDate = null);
    Task<IEnumerable<LeaveRequest>> GetRequestsByEmployeeAsync(int employeeId);
    Task<IEnumerable<LeaveRequest>> GetPendingRequestsAsync();
    Task<LeaveRequest?> GetRequestByIdAsync(int requestId);
    Task CreateRequestAsync(LeaveRequest request);
    Task ApproveRequestAsync(int requestId, int approverId);
    Task RejectRequestAsync(int requestId, int approverId);
    Task AdjustBalanceAsync(int employeeId, int year, decimal currentYearAdjustment, decimal carryForwardAdjustment);
    Task AllocateQuarterlyLeaveAsync();
    Task ProcessCarryForwardAsync();
}
