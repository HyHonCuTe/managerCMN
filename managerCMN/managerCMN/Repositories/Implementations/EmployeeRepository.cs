using Microsoft.EntityFrameworkCore;
using managerCMN.Data;
using managerCMN.Models.Entities;
using managerCMN.Repositories.Interfaces;

namespace managerCMN.Repositories.Implementations;

public class EmployeeRepository : Repository<Employee>, IEmployeeRepository
{
    public EmployeeRepository(ApplicationDbContext context) : base(context) { }

    public async Task<Employee?> GetByCodeAsync(string employeeCode)
        => await _dbSet.FirstOrDefaultAsync(e => e.EmployeeCode == employeeCode);

    public async Task<Employee?> GetByEmailAsync(string email)
        => await _dbSet.FirstOrDefaultAsync(e => e.Email == email);

    public async Task<Employee?> GetWithDetailsAsync(int id)
        => await _dbSet
            .Include(e => e.Department)
            .Include(e => e.EmergencyContacts)
            .Include(e => e.Contracts)
            .FirstOrDefaultAsync(e => e.EmployeeId == id);

    public async Task<IEnumerable<Employee>> GetByDepartmentAsync(int departmentId)
        => await _dbSet.Where(e => e.DepartmentId == departmentId)
            .Include(e => e.Department)
            .ToListAsync();
}
