using managerCMN.Models.Entities;
using managerCMN.Models.Enums;

namespace managerCMN.Services.Interfaces;

public interface IRequestService
{
    Task<IEnumerable<Request>> GetAllAsync();
    Task<Request?> GetByIdAsync(int id);
    Task<Request?> GetWithAttachmentsAsync(int id);
    Task<IEnumerable<Request>> GetByEmployeeAsync(int employeeId);
    Task<IEnumerable<Request>> GetByStatusAsync(RequestStatus status);
    Task CreateAsync(Request request);
    Task ManagerApproveAsync(int requestId, int approverId);
    Task HRApproveAsync(int requestId, int approverId);
    Task RejectAsync(int requestId, int approverId);
}
