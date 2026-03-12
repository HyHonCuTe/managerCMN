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
        => await _dbSet.Where(r => r.EmployeeId == employeeId)
            .OrderByDescending(r => r.CreatedDate)
            .ToListAsync();

    public async Task<IEnumerable<Request>> GetByStatusAsync(RequestStatus status)
        => await _dbSet
            .Include(r => r.Employee)
            .Where(r => r.Status == status)
            .OrderBy(r => r.CreatedDate)
            .ToListAsync();

    public async Task<Request?> GetWithAttachmentsAsync(int requestId)
        => await _dbSet
            .Include(r => r.Attachments)
            .Include(r => r.Employee)
            .FirstOrDefaultAsync(r => r.RequestId == requestId);
}
