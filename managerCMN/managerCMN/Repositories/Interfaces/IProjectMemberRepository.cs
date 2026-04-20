using managerCMN.Models.Entities;
using managerCMN.Models.Enums;

namespace managerCMN.Repositories.Interfaces;

public interface IProjectMemberRepository : IRepository<ProjectMember>
{
    Task<ProjectMember?> GetMemberAsync(int projectId, int employeeId);
    Task<IEnumerable<ProjectMember>> GetProjectMembersAsync(int projectId);
    Task<ProjectMemberRole?> GetMemberRoleAsync(int projectId, int employeeId);
}
