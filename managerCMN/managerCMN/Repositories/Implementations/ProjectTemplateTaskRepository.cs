using managerCMN.Data;
using managerCMN.Models.Entities;
using managerCMN.Repositories.Interfaces;

namespace managerCMN.Repositories.Implementations;

public class ProjectTemplateTaskRepository : Repository<ProjectTemplateTask>, IProjectTemplateTaskRepository
{
    public ProjectTemplateTaskRepository(ApplicationDbContext context) : base(context) { }
}
