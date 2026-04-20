using managerCMN.Models.Entities;

namespace managerCMN.Repositories.Interfaces;

public interface IProjectRepository : IRepository<Project>
{
    Task<Project?> GetWithDetailsAsync(int projectId);
    Task<IEnumerable<Project>> GetProjectsByMemberAsync(int employeeId);
    Task<bool> IsMemberAsync(int projectId, int employeeId);
}
