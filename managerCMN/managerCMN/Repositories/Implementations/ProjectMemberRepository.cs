using Microsoft.EntityFrameworkCore;
using managerCMN.Data;
using managerCMN.Models.Entities;
using managerCMN.Models.Enums;
using managerCMN.Repositories.Interfaces;

namespace managerCMN.Repositories.Implementations;

public class ProjectMemberRepository : Repository<ProjectMember>, IProjectMemberRepository
{
    public ProjectMemberRepository(ApplicationDbContext context) : base(context) { }

    public async Task<ProjectMember?> GetMemberAsync(int projectId, int employeeId)
        => await _context.ProjectMembers
            .Include(m => m.Employee)
            .FirstOrDefaultAsync(m => m.ProjectId == projectId && m.EmployeeId == employeeId);

    public async Task<IEnumerable<ProjectMember>> GetProjectMembersAsync(int projectId)
        => await _context.ProjectMembers
            .Include(m => m.Employee)
            .Where(m => m.ProjectId == projectId)
            .OrderBy(m => m.Role)
            .ToListAsync();

    public async Task<ProjectMemberRole?> GetMemberRoleAsync(int projectId, int employeeId)
    {
        var member = await _context.ProjectMembers
            .FirstOrDefaultAsync(m => m.ProjectId == projectId && m.EmployeeId == employeeId);
        return member?.Role;
    }
}
