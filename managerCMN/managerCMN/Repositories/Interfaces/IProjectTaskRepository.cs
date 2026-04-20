using managerCMN.Models.Entities;

namespace managerCMN.Repositories.Interfaces;

public interface IProjectTaskRepository : IRepository<ProjectTask>
{
    Task<ProjectTask?> GetWithDetailsAsync(int taskId);
    Task<IEnumerable<ProjectTask>> GetRootTasksAsync(int projectId);
    Task<IEnumerable<ProjectTask>> GetSubTasksAsync(int parentTaskId);
    Task<IEnumerable<ProjectTask>> GetTasksByAssigneeAsync(int projectId, int employeeId);
}
