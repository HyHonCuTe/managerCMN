using Microsoft.EntityFrameworkCore;
using managerCMN.Data;
using managerCMN.Models.Entities;
using managerCMN.Repositories.Interfaces;

namespace managerCMN.Repositories.Implementations;

public class FullAttendanceEmployeeRepository : Repository<FullAttendanceEmployee>, IFullAttendanceEmployeeRepository
{
    public FullAttendanceEmployeeRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IEnumerable<FullAttendanceEmployee>> GetAllWithEmployeeAsync()
        => await _dbSet.Include(f => f.Employee)
            .OrderBy(f => f.Employee.FullName)
            .ToListAsync();

    public async Task<FullAttendanceEmployee?> GetByEmployeeIdAsync(int employeeId)
        => await _dbSet.FirstOrDefaultAsync(f => f.EmployeeId == employeeId);

    public async Task<bool> IsFullAttendanceEmployeeAsync(int employeeId)
        => await _dbSet.AnyAsync(f => f.EmployeeId == employeeId);
}
