using managerCMN.Models.Entities;
using managerCMN.Models.Enums;

namespace managerCMN.Services.Interfaces;

public interface IRequestService
{
    Task<IEnumerable<Request>> GetAllAsync();
    Task<Request?> GetByIdAsync(int id);
    Task<Request?> GetWithDetailsAsync(int id);
    Task<Request?> GetWithAttachmentsAsync(int id);
    Task<IEnumerable<Request>> GetByEmployeeAsync(int employeeId);
    Task<IEnumerable<Request>> GetByStatusAsync(RequestStatus status);
    Task<IEnumerable<Request>> GetPendingForApproverAsync(int approverEmployeeId);
    Task<IEnumerable<Request>> GetAllForApproverAsync(int approverEmployeeId);
    Task<IEnumerable<Request>> FilterAsync(RequestStatus? status, RequestType? type);

    Task CreateAsync(Request request, int approver1Id, int approver2Id);
    Task UpdateAsync(Request request);
    Task ApproveAsync(int requestId, int approverEmployeeId, string? comment = null);
    Task RejectAsync(int requestId, int approverEmployeeId, string? comment = null);
    Task ForceApproveAsync(int requestId, int adminEmployeeId, string? comment = null);
    Task ForceRejectAsync(int requestId, int adminEmployeeId, string? comment = null);
    Task RevertApprovalAsync(int requestId, int adminEmployeeId, string? comment = null);
    Task CancelAsync(int requestId, int employeeId);

    Task<int?> GetDefaultApprover1Async(int employeeId);
    Task<bool> NeedsManualApprover1SelectionAsync(int employeeId);
    Task<IEnumerable<Employee>> GetDepartmentManagersAsync(int employeeId);
    Task<IEnumerable<Employee>> GetAvailableApprover2ListAsync();
    Task<int> CountAbsenceRequestsInMonthAsync(int employeeId, DateTime date);
    Task<int> CountCheckInOutRequestsInMonthAsync(int employeeId, DateTime date);
    Task<decimal> CalculateTotalDaysAsync(DateTime start, DateTime end, bool halfDayStart, bool halfDayEnd);
}
