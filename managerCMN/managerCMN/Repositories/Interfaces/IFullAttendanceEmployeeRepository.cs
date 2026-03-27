using managerCMN.Models.Entities;

namespace managerCMN.Repositories.Interfaces;

public interface IFullAttendanceEmployeeRepository : IRepository<FullAttendanceEmployee>
{
    Task<IEnumerable<FullAttendanceEmployee>> GetAllWithEmployeeAsync();
    Task<FullAttendanceEmployee?> GetByEmployeeIdAsync(int employeeId);
    Task<bool> IsFullAttendanceEmployeeAsync(int employeeId);
}
