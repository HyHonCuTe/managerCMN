using Microsoft.EntityFrameworkCore;
using managerCMN.Data;
using managerCMN.Models.Entities;
using managerCMN.Repositories.Interfaces;

namespace managerCMN.Repositories.Implementations;

public class DepartmentRepository : Repository<Department>, IDepartmentRepository
{
    public DepartmentRepository(ApplicationDbContext context) : base(context) { }

    public async Task<Department?> GetWithEmployeesAsync(int id)
        => await _dbSet
            .Include(d => d.Employees)
            .Include(d => d.Manager)
            .FirstOrDefaultAsync(d => d.DepartmentId == id);
}
