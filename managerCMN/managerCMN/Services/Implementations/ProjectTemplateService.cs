using managerCMN.Models.Entities;
using managerCMN.Models.ViewModels;
using managerCMN.Repositories.Interfaces;
using managerCMN.Services.Interfaces;

namespace managerCMN.Services.Implementations;

public class ProjectTemplateService : IProjectTemplateService
{
    private readonly IUnitOfWork _unitOfWork;

    public ProjectTemplateService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<ProjectTemplateListViewModel>> GetAllAsync()
    {
        var templates = await _unitOfWork.ProjectTemplates.GetAllAsync();
        return templates
            .OrderByDescending(t => t.CreatedDate)
            .Select(t => new ProjectTemplateListViewModel
            {
                ProjectTemplateId = t.ProjectTemplateId,
                Name = t.Name,
                Description = t.Description,
                IsActive = t.IsActive,
                TaskCount = t.Tasks.Count,
                CreatedByName = t.CreatedByEmployee?.FullName ?? string.Empty,
                CreatedDate = t.CreatedDate
            });
    }

    public async Task<IEnumerable<ProjectTemplateListViewModel>> GetAllActiveAsync()
    {
        var templates = await _unitOfWork.ProjectTemplates.GetAllActiveAsync();
        return templates.Select(t => new ProjectTemplateListViewModel
        {
            ProjectTemplateId = t.ProjectTemplateId,
            Name = t.Name,
            Description = t.Description,
            IsActive = t.IsActive,
            TaskCount = t.Tasks.Count,
            CreatedByName = t.CreatedByEmployee?.FullName ?? string.Empty,
            CreatedDate = t.CreatedDate
        });
    }

    public async Task<ProjectTemplateDetailViewModel?> GetByIdAsync(int templateId)
    {
        var template = await _unitOfWork.ProjectTemplates.GetWithTasksAsync(templateId);
        if (template == null) return null;

        return new ProjectTemplateDetailViewModel
        {
            ProjectTemplateId = template.ProjectTemplateId,
            Name = template.Name,
            Description = template.Description,
            IsActive = template.IsActive,
            CreatedByName = template.CreatedByEmployee?.FullName ?? string.Empty,
            CreatedDate = template.CreatedDate,
            Tasks = BuildOrderedTaskViewModels(template.Tasks.ToList())
        };
    }

    public async Task<int> CreateAsync(ProjectTemplateCreateViewModel vm, int creatorEmployeeId)
    {
        var template = new ProjectTemplate
        {
            Name = vm.Name,
            Description = vm.Description,
            IsActive = vm.IsActive,
            CreatedByEmployeeId = creatorEmployeeId,
            CreatedDate = DateTime.Now
        };

        await _unitOfWork.ProjectTemplates.AddAsync(template);
        await _unitOfWork.SaveChangesAsync();

        await SaveTasksAsync(template.ProjectTemplateId, vm.Tasks);
        return template.ProjectTemplateId;
    }

    public async Task UpdateAsync(ProjectTemplateEditViewModel vm)
    {
        var template = await _unitOfWork.ProjectTemplates.GetWithTasksAsync(vm.ProjectTemplateId)
            ?? throw new InvalidOperationException("Template không tồn tại.");

        template.Name = vm.Name;
        template.Description = vm.Description;
        template.IsActive = vm.IsActive;
        template.ModifiedDate = DateTime.Now;

        await DeleteAllTemplateTasksAsync(template.Tasks.ToList());

        _unitOfWork.ProjectTemplates.Update(template);
        await _unitOfWork.SaveChangesAsync();

        await SaveTasksAsync(template.ProjectTemplateId, vm.Tasks);
    }

    public async Task DeleteAsync(int templateId)
    {
        var template = await _unitOfWork.ProjectTemplates.GetWithTasksAsync(templateId)
            ?? throw new InvalidOperationException("Template không tồn tại.");

        await DeleteAllTemplateTasksAsync(template.Tasks.ToList());

        _unitOfWork.ProjectTemplates.Remove(template);
        await _unitOfWork.SaveChangesAsync();
    }

    // Saves a flat task list (DFS order: parent always before its children) into the DB.
    private async Task SaveTasksAsync(int templateId, List<ProjectTemplateTaskFormViewModel> taskVms)
    {
        if (!taskVms.Any()) return;

        var indexToDbId = new Dictionary<int, int>();
        for (int i = 0; i < taskVms.Count; i++)
        {
            var vm = taskVms[i];
            int? parentDbId = vm.ParentIndex >= 0 && indexToDbId.TryGetValue(vm.ParentIndex, out var pid)
                ? pid
                : null;

            var task = BuildTask(templateId, vm, parentDbId, i);
            await _unitOfWork.ProjectTemplateTasks.AddAsync(task);
            await _unitOfWork.SaveChangesAsync();
            indexToDbId[i] = task.ProjectTemplateTaskId;
        }
    }

    // Deletes all tasks of a template, deepest-level first to satisfy the Restrict FK.
    private async Task DeleteAllTemplateTasksAsync(List<ProjectTemplateTask> tasks)
    {
        if (!tasks.Any()) return;

        var idMap = tasks.ToDictionary(t => t.ProjectTemplateTaskId);

        int GetDepth(ProjectTemplateTask t)
        {
            var d = 0;
            var cur = t;
            var visited = new HashSet<int>();
            while (cur.ParentTemplateTaskId.HasValue
                   && idMap.TryGetValue(cur.ParentTemplateTaskId.Value, out var parent)
                   && visited.Add(cur.ProjectTemplateTaskId))
            {
                d++;
                cur = parent;
            }
            return d;
        }

        var byLevelDesc = tasks
            .GroupBy(GetDepth)
            .OrderByDescending(g => g.Key);

        foreach (var group in byLevelDesc)
        {
            foreach (var t in group)
                _unitOfWork.ProjectTemplateTasks.Remove(t);
            await _unitOfWork.SaveChangesAsync();
        }
    }

    private static ProjectTemplateTask BuildTask(int templateId, ProjectTemplateTaskFormViewModel vm,
        int? parentDbId, int sortOrder) => new()
    {
        ProjectTemplateId = templateId,
        ParentTemplateTaskId = parentDbId,
        Title = vm.Title,
        Description = vm.Description,
        Priority = vm.Priority,
        EstimatedHours = vm.EstimatedHours,
        SortOrder = sortOrder
    };

    // Builds a DFS-ordered flat list (parent always before its children).
    // ParentIndex is the flat-list index of the immediate parent (-1 for root).
    public static List<ProjectTemplateTaskFormViewModel> BuildOrderedTaskViewModels(
        List<ProjectTemplateTask> allTasks)
    {
        var result = new List<ProjectTemplateTaskFormViewModel>();
        var idToIndex = new Dictionary<int, int>();

        void Visit(int? parentTemplateTaskId, int parentFlatIndex)
        {
            foreach (var t in allTasks
                .Where(x => x.ParentTemplateTaskId == parentTemplateTaskId)
                .OrderBy(x => x.SortOrder))
            {
                var idx = result.Count;
                idToIndex[t.ProjectTemplateTaskId] = idx;
                result.Add(new ProjectTemplateTaskFormViewModel
                {
                    ProjectTemplateTaskId = t.ProjectTemplateTaskId,
                    Title = t.Title,
                    Description = t.Description,
                    Priority = t.Priority,
                    EstimatedHours = t.EstimatedHours,
                    SortOrder = idx,
                    ParentIndex = parentFlatIndex
                });
                Visit(t.ProjectTemplateTaskId, idx);
            }
        }

        Visit(null, -1);
        return result;
    }
}
