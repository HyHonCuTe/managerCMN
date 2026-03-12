using managerCMN.Models.Entities;
using managerCMN.Models.Enums;

namespace managerCMN.Repositories.Interfaces;

public interface IRequestRepository : IRepository<Request>
{
    Task<IEnumerable<Request>> GetByEmployeeAsync(int employeeId);
    Task<IEnumerable<Request>> GetByStatusAsync(RequestStatus status);
    Task<Request?> GetWithAttachmentsAsync(int requestId);
}
