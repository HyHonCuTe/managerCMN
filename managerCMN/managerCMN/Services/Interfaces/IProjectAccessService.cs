using managerCMN.Models.Enums;

namespace managerCMN.Services.Interfaces;

public interface IProjectAccessService
{
    Task<ProjectMemberRole?> GetRoleAsync(int projectId, int employeeId);
    Task<bool> IsMemberAsync(int projectId, int employeeId);
    Task EnsureIsMemberAsync(int projectId, int employeeId);
    Task EnsureCanManageMembersAsync(int projectId, int employeeId);
    Task EnsureCanManageManagersAsync(int projectId, int employeeId);
    Task EnsureCanManageTaskAsync(int projectId, int employeeId);
}
