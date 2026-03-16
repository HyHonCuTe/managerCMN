using Microsoft.EntityFrameworkCore;
using managerCMN.Data;
using managerCMN.Models.Entities;
using managerCMN.Models.Enums;
using managerCMN.Repositories.Interfaces;

namespace managerCMN.Repositories.Implementations;

public class RequestApprovalRepository : Repository<RequestApproval>, IRequestApprovalRepository
{
    public RequestApprovalRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IEnumerable<RequestApproval>> GetByRequestAsync(int requestId)
        => await _dbSet
            .Include(ra => ra.Approver)
            .Where(ra => ra.RequestId == requestId)
            .OrderBy(ra => ra.ApproverOrder)
            .ToListAsync();

    public async Task<IEnumerable<RequestApproval>> GetPendingByApproverAsync(int approverId)
        => await _dbSet
            .Include(ra => ra.Request)
                .ThenInclude(r => r.Employee)
            .Where(ra => ra.ApproverId == approverId && ra.Status == ApprovalStatus.Pending)
            .Where(ra => ra.Request.Status != RequestStatus.Rejected && ra.Request.Status != RequestStatus.Cancelled)
            .OrderBy(ra => ra.Request.CreatedDate)
            .ToListAsync();

    public async Task<RequestApproval?> GetByRequestAndOrderAsync(int requestId, int approverOrder)
        => await _dbSet
            .Include(ra => ra.Approver)
            .FirstOrDefaultAsync(ra => ra.RequestId == requestId && ra.ApproverOrder == approverOrder);
}
