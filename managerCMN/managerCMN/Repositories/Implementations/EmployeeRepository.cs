using Microsoft.EntityFrameworkCore;
using managerCMN.Data;
using managerCMN.Models.Entities;
using managerCMN.Repositories.Interfaces;

namespace managerCMN.Repositories.Implementations;

public class EmployeeRepository : Repository<Employee>, IEmployeeRepository
{
    public EmployeeRepository(ApplicationDbContext context) : base(context) { }

    public new async Task<IEnumerable<Employee>> GetAllAsync()
        => await _dbSet.Include(e => e.Department).Include(e => e.Position).ToListAsync();

    public async Task<Employee?> GetByCodeAsync(string employeeCode)
        => await _dbSet.FirstOrDefaultAsync(e => e.EmployeeCode == employeeCode);

    public async Task<Employee?> GetByAttendanceCodeAsync(string attendanceCode)
    {
        // Direct match first
        var emp = await _dbSet.FirstOrDefaultAsync(e => e.AttendanceCode == attendanceCode);
        if (emp != null) return emp;

        // Try numeric match: input "1" -> match "AC0001", "AC001", "AC01", "AC1", etc.
        if (int.TryParse(attendanceCode, out var num))
        {
            var padded = $"AC{num:D4}";
            return await _dbSet.FirstOrDefaultAsync(e => e.AttendanceCode == padded);
        }
        return null;
    }

    public async Task<Employee?> GetByEmailAsync(string email)
        => await _dbSet.FirstOrDefaultAsync(e => e.Email == email);

    public async Task<Employee?> GetWithDetailsAsync(int id)
        => await _dbSet
            .Include(e => e.Department)
            .Include(e => e.Position)
            .Include(e => e.EmergencyContacts)
            .Include(e => e.Contracts)
            .FirstOrDefaultAsync(e => e.EmployeeId == id);

    public async Task<IEnumerable<Employee>> GetByDepartmentAsync(int departmentId)
        => await _dbSet.Where(e => e.DepartmentId == departmentId)
            .Include(e => e.Department)
            .ToListAsync();
}
