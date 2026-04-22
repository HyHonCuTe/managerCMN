using Microsoft.EntityFrameworkCore;
using managerCMN.Data;
using managerCMN.Models.Entities;
using managerCMN.Repositories.Interfaces;

namespace managerCMN.Repositories.Implementations;

public class ProjectTemplateRepository : Repository<ProjectTemplate>, IProjectTemplateRepository
{
    public ProjectTemplateRepository(ApplicationDbContext context) : base(context) { }

    public override async Task<IEnumerable<ProjectTemplate>> GetAllAsync()
        => await _context.ProjectTemplates
            .Include(t => t.CreatedByEmployee)
            .Include(t => t.Tasks)
            .OrderByDescending(t => t.CreatedDate)
            .ToListAsync();

    public async Task<IEnumerable<ProjectTemplate>> GetAllActiveAsync()
        => await _context.ProjectTemplates
            .Where(t => t.IsActive)
            .Include(t => t.Tasks)
            .OrderBy(t => t.Name)
            .ToListAsync();

    public async Task<ProjectTemplate?> GetWithTasksAsync(int templateId)
        => await _context.ProjectTemplates
            .Include(t => t.CreatedByEmployee)
            .Include(t => t.Tasks.OrderBy(tt => tt.SortOrder))
            .FirstOrDefaultAsync(t => t.ProjectTemplateId == templateId);
}
