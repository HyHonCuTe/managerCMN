using Microsoft.EntityFrameworkCore;
using managerCMN.Data;
using managerCMN.Models.Entities;
using managerCMN.Repositories.Interfaces;

namespace managerCMN.Repositories.Implementations;

public class ProjectTaskRepository : Repository<ProjectTask>, IProjectTaskRepository
{
    public ProjectTaskRepository(ApplicationDbContext context) : base(context) { }

    public async Task<ProjectTask?> GetWithDetailsAsync(int taskId)
        => await _context.ProjectTasks
            .Include(t => t.CreatedByEmployee)
            .Include(t => t.Assignments)
                .ThenInclude(a => a.Employee)
            .Include(t => t.ChecklistItems)
                .ThenInclude(c => c.CompletedByEmployee)
            .Include(t => t.SubTasks)
                .ThenInclude(s => s.Assignments)
                    .ThenInclude(a => a.Employee)
            .Include(t => t.SubTasks)
                .ThenInclude(s => s.ChecklistItems)
            .Include(t => t.SubTasks)
                .ThenInclude(s => s.CreatedByEmployee)
            .Include(t => t.Updates.OrderByDescending(u => u.CreatedDate))
                .ThenInclude(u => u.SenderEmployee)
            .Include(t => t.Updates.OrderByDescending(u => u.CreatedDate))
                .ThenInclude(u => u.Attachments)
            .Include(t => t.ParentTask)
            .FirstOrDefaultAsync(t => t.ProjectTaskId == taskId);

    public async Task<IEnumerable<ProjectTask>> GetRootTasksAsync(int projectId)
        => await _context.ProjectTasks
            .Include(t => t.Assignments).ThenInclude(a => a.Employee)
            .Include(t => t.ChecklistItems)
            .Include(t => t.SubTasks).ThenInclude(s => s.Assignments).ThenInclude(a => a.Employee)
            .Include(t => t.SubTasks).ThenInclude(s => s.ChecklistItems)
            .Include(t => t.SubTasks).ThenInclude(s => s.SubTasks)
            .Where(t => t.ProjectId == projectId && t.ParentTaskId == null)
            .OrderBy(t => t.CreatedDate)
            .ToListAsync();

    public async Task<IEnumerable<ProjectTask>> GetSubTasksAsync(int parentTaskId)
        => await _context.ProjectTasks
            .Include(t => t.Assignments).ThenInclude(a => a.Employee)
            .Include(t => t.ChecklistItems)
            .Where(t => t.ParentTaskId == parentTaskId)
            .OrderBy(t => t.CreatedDate)
            .ToListAsync();

    public async Task<IEnumerable<ProjectTask>> GetTasksByAssigneeAsync(int projectId, int employeeId)
        => await _context.ProjectTasks
            .Include(t => t.Assignments).ThenInclude(a => a.Employee)
            .Include(t => t.ChecklistItems)
            .Where(t => t.ProjectId == projectId
                && t.Assignments.Any(a => a.EmployeeId == employeeId))
            .OrderBy(t => t.DueDate)
            .ToListAsync();
}
