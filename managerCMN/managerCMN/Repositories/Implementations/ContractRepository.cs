using Microsoft.EntityFrameworkCore;
using managerCMN.Data;
using managerCMN.Helpers;
using managerCMN.Models.Entities;
using managerCMN.Models.Enums;
using managerCMN.Repositories.Interfaces;

namespace managerCMN.Repositories.Implementations;

public class ContractRepository : Repository<Contract>, IContractRepository
{
    public ContractRepository(ApplicationDbContext context) : base(context) { }

    public new async Task<IEnumerable<Contract>> GetAllAsync()
        => await _dbSet
            .Include(c => c.Employee)
            .OrderBy(c => c.EndDate.HasValue ? 0 : 1)
            .ThenBy(c => c.EndDate)
            .ThenByDescending(c => c.StartDate)
            .ToListAsync();

    public async Task<IEnumerable<Contract>> GetExpiringContractsAsync(int daysBeforeExpiry = 30)
    {
        var today = DateTimeHelper.VietnamToday;
        var threshold = today.AddDays(daysBeforeExpiry);
        return await _dbSet
            .Include(c => c.Employee)
            .Where(c => c.Status == ContractStatus.Active
                && c.EndDate != null
                && c.EndDate.Value.Date <= threshold
                && c.EndDate.Value.Date >= today)
            .ToListAsync();
    }

    public async Task<IEnumerable<Contract>> GetByEmployeeAsync(int employeeId)
        => await _dbSet
            .Include(c => c.Employee)
            .Where(c => c.EmployeeId == employeeId)
            .OrderByDescending(c => c.StartDate)
            .ToListAsync();
}
