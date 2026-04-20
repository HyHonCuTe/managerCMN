using managerCMN.Models.Entities;
using managerCMN.Models.ViewModels;

namespace managerCMN.Services.Interfaces;

public interface IProjectTaskService
{
    Task<IEnumerable<ProjectTaskTreeViewModel>> GetTaskTreeAsync(int projectId, int employeeId, bool ignoreAccessCheck = false);
    Task<ProjectTaskTreeViewModel?> GetTaskDetailsAsync(int taskId, int employeeId);
    Task<int> CreateTaskAsync(ProjectTaskCreateViewModel vm, int creatorEmployeeId);
    Task UpdateTaskAsync(ProjectTaskEditViewModel vm, int employeeId);
    Task DeleteTaskAsync(int taskId, int employeeId);
    Task UpdateStatusAsync(UpdateTaskStatusViewModel vm, int employeeId);
    Task UpdateProgressAsync(UpdateTaskProgressViewModel vm, int employeeId);
    Task AssignMembersAsync(int taskId, List<int> employeeIds, int actorEmployeeId);
    Task<ChecklistItemViewModel> AddChecklistItemAsync(AddChecklistItemViewModel vm, int employeeId);
    Task<int> ToggleChecklistItemAsync(int checklistItemId, int employeeId);
    Task<int> DeleteChecklistItemAsync(int checklistItemId, int employeeId);
    Task AddTaskUpdateAsync(PostTaskUpdateViewModel vm, int employeeId);
    Task<ProjectTaskAttachment?> GetAttachmentAsync(int attachmentId, int employeeId);
}
