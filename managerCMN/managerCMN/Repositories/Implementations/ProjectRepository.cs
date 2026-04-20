using Microsoft.EntityFrameworkCore;
using managerCMN.Data;
using managerCMN.Models.Entities;
using managerCMN.Repositories.Interfaces;

namespace managerCMN.Repositories.Implementations;

public class ProjectRepository : Repository<Project>, IProjectRepository
{
    public ProjectRepository(ApplicationDbContext context) : base(context) { }

    public async Task<Project?> GetWithDetailsAsync(int projectId)
        => await _context.Projects
            .Include(p => p.CreatedByEmployee)
            .Include(p => p.Members)
                .ThenInclude(m => m.Employee)
                    .ThenInclude(e => e.Department)
            .FirstOrDefaultAsync(p => p.ProjectId == projectId);

    public async Task<IEnumerable<Project>> GetProjectsByMemberAsync(int employeeId)
        => await _context.Projects
            .Include(p => p.CreatedByEmployee)
            .Include(p => p.Members)
            .Where(p => p.Members.Any(m => m.EmployeeId == employeeId))
            .OrderByDescending(p => p.CreatedDate)
            .ToListAsync();

    public async Task<bool> IsMemberAsync(int projectId, int employeeId)
        => await _context.ProjectMembers
            .AnyAsync(m => m.ProjectId == projectId && m.EmployeeId == employeeId);
}
