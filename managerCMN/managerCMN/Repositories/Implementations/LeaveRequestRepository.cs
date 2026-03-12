using Microsoft.EntityFrameworkCore;
using managerCMN.Data;
using managerCMN.Models.Entities;
using managerCMN.Models.Enums;
using managerCMN.Repositories.Interfaces;

namespace managerCMN.Repositories.Implementations;

public class LeaveRequestRepository : Repository<LeaveRequest>, ILeaveRequestRepository
{
    public LeaveRequestRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IEnumerable<LeaveRequest>> GetByEmployeeAsync(int employeeId)
        => await _dbSet.Where(lr => lr.EmployeeId == employeeId)
            .OrderByDescending(lr => lr.CreatedAt)
            .ToListAsync();

    public async Task<IEnumerable<LeaveRequest>> GetPendingRequestsAsync()
        => await _dbSet
            .Include(lr => lr.Employee)
            .Where(lr => lr.Status == RequestStatus.Pending || lr.Status == RequestStatus.ManagerApproved)
            .OrderBy(lr => lr.CreatedAt)
            .ToListAsync();
}
