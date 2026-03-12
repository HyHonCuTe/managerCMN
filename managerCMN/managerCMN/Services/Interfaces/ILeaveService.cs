using managerCMN.Models.Entities;

namespace managerCMN.Services.Interfaces;

public interface ILeaveService
{
    Task<LeaveBalance?> GetBalanceAsync(int employeeId, int year);
    Task<IEnumerable<LeaveRequest>> GetRequestsByEmployeeAsync(int employeeId);
    Task<IEnumerable<LeaveRequest>> GetPendingRequestsAsync();
    Task<LeaveRequest?> GetRequestByIdAsync(int requestId);
    Task CreateRequestAsync(LeaveRequest request);
    Task ApproveRequestAsync(int requestId, int approverId);
    Task RejectRequestAsync(int requestId, int approverId);
    Task AllocateQuarterlyLeaveAsync();
    Task ProcessCarryForwardAsync();
}
