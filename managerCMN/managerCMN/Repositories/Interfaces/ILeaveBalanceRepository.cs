using managerCMN.Models.Entities;

namespace managerCMN.Repositories.Interfaces;

public interface ILeaveBalanceRepository : IRepository<LeaveBalance>
{
    Task<LeaveBalance?> GetByEmployeeAndYearAsync(int employeeId, int year);
}
