using managerCMN.Models.Entities;

namespace managerCMN.Repositories.Interfaces;

public interface IProjectTemplateRepository : IRepository<ProjectTemplate>
{
    Task<IEnumerable<ProjectTemplate>> GetAllActiveAsync();
    Task<ProjectTemplate?> GetWithTasksAsync(int templateId);
}
