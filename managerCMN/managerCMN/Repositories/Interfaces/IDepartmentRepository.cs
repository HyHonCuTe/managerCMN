using managerCMN.Models.Entities;

namespace managerCMN.Repositories.Interfaces;

public interface IDepartmentRepository : IRepository<Department>
{
    Task<Department?> GetWithEmployeesAsync(int id);
}
