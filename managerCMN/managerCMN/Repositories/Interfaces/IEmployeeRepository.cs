using managerCMN.Models.Entities;

namespace managerCMN.Repositories.Interfaces;

public interface IEmployeeRepository : IRepository<Employee>
{
    Task<Employee?> GetByCodeAsync(string employeeCode);
    Task<Employee?> GetByEmailAsync(string email);
    Task<Employee?> GetWithDetailsAsync(int id);
    Task<IEnumerable<Employee>> GetByDepartmentAsync(int departmentId);
}
