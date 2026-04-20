using Microsoft.EntityFrameworkCore;
using managerCMN.Data;
using managerCMN.Models.Enums;
using managerCMN.Repositories.Interfaces;
using managerCMN.Services.Interfaces;

namespace managerCMN.Services.Implementations;

public class ProjectProgressService : IProjectProgressService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ApplicationDbContext _context;

    public ProjectProgressService(IUnitOfWork unitOfWork, ApplicationDbContext context)
    {
        _unitOfWork = unitOfWork;
        _context = context;
    }

    public async Task RecalculateTaskProgressAsync(int taskId)
    {
        var task = await _context.ProjectTasks
            .Include(t => t.ChecklistItems)
            .Include(t => t.SubTasks).ThenInclude(s => s.ChecklistItems)
            .FirstOrDefaultAsync(t => t.ProjectTaskId == taskId);

        if (task == null) return;

        if (task.Status == ProjectTaskStatus.Done)
        {
            task.Progress = 100;
            task.CompletedDate ??= DateTime.Now;
            task.ModifiedDate = DateTime.Now;
            _context.ProjectTasks.Update(task);
            await _context.SaveChangesAsync();
            return;
        }

        if (task.Status == ProjectTaskStatus.Cancelled)
        {
            task.Progress = 0;
            task.CompletedDate = null;
            task.ModifiedDate = DateTime.Now;
            _context.ProjectTasks.Update(task);
            await _context.SaveChangesAsync();
            return;
        }

        if (task.ProgressMode == ProgressMode.Manual) return;

        var subTasks = task.SubTasks.Where(s => s.Status != ProjectTaskStatus.Cancelled).ToList();

        if (subTasks.Any())
        {
            // Weighted average of subtasks by estimated hours (fallback: equal weight)
            decimal totalWeight = subTasks.Sum(s => s.EstimatedHours ?? 1m);
            decimal weightedSum = subTasks.Sum(s => s.Progress * (s.EstimatedHours ?? 1m));
            task.Progress = totalWeight > 0 ? Math.Round(weightedSum / totalWeight, 2) : 0;
        }
        else if (task.ChecklistItems.Any())
        {
            var total = task.ChecklistItems.Count;
            var done = task.ChecklistItems.Count(c => c.IsDone);
            task.Progress = total > 0 ? Math.Round((decimal)done / total * 100, 2) : 0;
        }
        // else: keep manual/current value

        task.ModifiedDate = DateTime.Now;
        _context.ProjectTasks.Update(task);
        await _context.SaveChangesAsync();
    }

    public async Task BubbleUpParentProgressAsync(int taskId)
    {
        var task = await _context.ProjectTasks.FirstOrDefaultAsync(t => t.ProjectTaskId == taskId);
        if (task?.ParentTaskId == null) return;

        await RecalculateTaskProgressAsync(task.ParentTaskId.Value);
        await BubbleUpParentProgressAsync(task.ParentTaskId.Value);
    }

    public async Task RecalculateProjectProgressAsync(int projectId)
    {
        var rootTasks = await _context.ProjectTasks
            .Where(t => t.ProjectId == projectId && t.ParentTaskId == null && t.Status != ProjectTaskStatus.Cancelled)
            .ToListAsync();

        var project = await _context.Projects.FirstOrDefaultAsync(p => p.ProjectId == projectId);
        if (project == null) return;

        if (!rootTasks.Any())
        {
            project.Progress = 0;
        }
        else
        {
            decimal totalWeight = rootTasks.Sum(t => t.EstimatedHours ?? 1m);
            decimal weightedSum = rootTasks.Sum(t => t.Progress * (t.EstimatedHours ?? 1m));
            project.Progress = totalWeight > 0 ? Math.Round(weightedSum / totalWeight, 2) : 0;
        }

        project.ModifiedDate = DateTime.Now;
        _context.Projects.Update(project);
        await _context.SaveChangesAsync();
    }
}
