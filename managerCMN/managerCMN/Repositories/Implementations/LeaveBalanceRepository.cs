using Microsoft.EntityFrameworkCore;
using managerCMN.Data;
using managerCMN.Models.Entities;
using managerCMN.Repositories.Interfaces;

namespace managerCMN.Repositories.Implementations;

public class LeaveBalanceRepository : Repository<LeaveBalance>, ILeaveBalanceRepository
{
    public LeaveBalanceRepository(ApplicationDbContext context) : base(context) { }

    public async Task<LeaveBalance?> GetByEmployeeAndYearAsync(int employeeId, int year)
        => await _dbSet.FirstOrDefaultAsync(lb => lb.EmployeeId == employeeId && lb.Year == year);
}
