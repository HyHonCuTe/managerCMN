using managerCMN.Models.Entities;
using managerCMN.Models.Enums;

namespace managerCMN.Repositories.Interfaces;

public interface IRequestRepository : IRepository<Request>
{
    Task<IEnumerable<Request>> GetByEmployeeAsync(int employeeId);
    Task<IEnumerable<Request>> GetByStatusAsync(RequestStatus status);
    Task<Request?> GetWithAttachmentsAsync(int requestId);
    Task<Request?> GetWithApprovalsAsync(int requestId);
    Task<IEnumerable<Request>> GetPendingForApproverAsync(int approverId);
    Task<IEnumerable<Request>> GetAllForApproverAsync(int approverId);
    Task<IEnumerable<Request>> GetByStatusAndTypeAsync(RequestStatus? status, RequestType? type);
}
