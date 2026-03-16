using managerCMN.Models.Entities;
using managerCMN.Models.Enums;

namespace managerCMN.Repositories.Interfaces;

public interface IRequestApprovalRepository : IRepository<RequestApproval>
{
    Task<IEnumerable<RequestApproval>> GetByRequestAsync(int requestId);
    Task<IEnumerable<RequestApproval>> GetPendingByApproverAsync(int approverId);
    Task<RequestApproval?> GetByRequestAndOrderAsync(int requestId, int approverOrder);
}
