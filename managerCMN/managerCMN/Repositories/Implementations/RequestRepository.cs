using Microsoft.EntityFrameworkCore;
using managerCMN.Data;
using managerCMN.Models.Entities;
using managerCMN.Models.Enums;
using managerCMN.Repositories.Interfaces;

namespace managerCMN.Repositories.Implementations;

public class RequestRepository : Repository<Request>, IRequestRepository
{
    public RequestRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IEnumerable<Request>> GetByEmployeeAsync(int employeeId)
        => await _dbSet
            .Include(r => r.Approvals).ThenInclude(a => a.Approver)
            .Where(r => r.EmployeeId == employeeId)
            .OrderByDescending(r => r.CreatedDate)
            .ToListAsync();

    public async Task<IEnumerable<Request>> GetByStatusAsync(RequestStatus status)
        => await _dbSet
            .Include(r => r.Employee)
            .Include(r => r.Approvals).ThenInclude(a => a.Approver)
            .Where(r => r.Status == status)
            .OrderBy(r => r.CreatedDate)
            .ToListAsync();

    public async Task<Request?> GetWithAttachmentsAsync(int requestId)
        => await _dbSet
            .Include(r => r.Attachments)
            .Include(r => r.Employee)
            .Include(r => r.Approvals).ThenInclude(a => a.Approver)
            .FirstOrDefaultAsync(r => r.RequestId == requestId);

    public async Task<Request?> GetWithApprovalsAsync(int requestId)
        => await _dbSet
            .Include(r => r.Employee)
                .ThenInclude(e => e.Department)
            .Include(r => r.Attachments)
            .Include(r => r.Approvals)
                .ThenInclude(a => a.Approver)
            .FirstOrDefaultAsync(r => r.RequestId == requestId);

    public async Task<IEnumerable<Request>> GetPendingForApproverAsync(int approverId)
        => await _context.Set<RequestApproval>()
            .Where(ra => ra.ApproverId == approverId && ra.Status == ApprovalStatus.Pending)
            .Where(ra => ra.Request.Status != RequestStatus.Rejected && ra.Request.Status != RequestStatus.Cancelled)
            .Include(ra => ra.Request)
                .ThenInclude(r => r.Employee)
            .Include(ra => ra.Request)
                .ThenInclude(r => r.Approvals)
                    .ThenInclude(a => a.Approver)
            .Select(ra => ra.Request)
            .Distinct()
            .OrderBy(r => r.CreatedDate)
            .ToListAsync();

    public async Task<IEnumerable<Request>> GetAllForApproverAsync(int approverId)
        => await _context.Set<RequestApproval>()
            .Where(ra => ra.ApproverId == approverId)
            .Include(ra => ra.Request)
                .ThenInclude(r => r.Employee)
            .Include(ra => ra.Request)
                .ThenInclude(r => r.Approvals)
                    .ThenInclude(a => a.Approver)
            .Select(ra => ra.Request)
            .Distinct()
            .OrderByDescending(r => r.CreatedDate)
            .ToListAsync();

    public async Task<IEnumerable<Request>> GetByStatusAndTypeAsync(RequestStatus? status, RequestType? type)
        => await _dbSet
            .Include(r => r.Employee)
            .Include(r => r.Approvals).ThenInclude(a => a.Approver)
            .Where(r => (!status.HasValue || r.Status == status.Value)
                     && (!type.HasValue || r.RequestType == type.Value))
            .OrderByDescending(r => r.CreatedDate)
            .ToListAsync();
}
