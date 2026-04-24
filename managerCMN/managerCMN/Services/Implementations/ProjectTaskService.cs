using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using managerCMN.Data;
using managerCMN.Helpers;
using managerCMN.Models.Entities;
using managerCMN.Models.Enums;
using managerCMN.Models.ViewModels;
using managerCMN.Repositories.Interfaces;
using managerCMN.Services.Interfaces;

namespace managerCMN.Services.Implementations;

public class ProjectTaskService : IProjectTaskService
{
    private const string TaskChangeLogPrefix = "[TASK_CHANGE_LOG]";
    private const string TaskChangeKindRejected = "rejected";
    private const string TaskChangeKindUndo = "undo";
    private const string TaskChangeKindOther = "other";

    private readonly IUnitOfWork _unitOfWork;
    private readonly IProjectAccessService _accessService;
    private readonly IProjectProgressService _progressService;
    private readonly ISystemLogService _logService;
    private readonly INotificationService _notificationService;
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _env;

    public ProjectTaskService(IUnitOfWork unitOfWork, IProjectAccessService accessService,
        IProjectProgressService progressService, ISystemLogService logService,
        INotificationService notificationService,
        ApplicationDbContext context, IWebHostEnvironment env)
    {
        _unitOfWork = unitOfWork;
        _accessService = accessService;
        _progressService = progressService;
        _logService = logService;
        _notificationService = notificationService;
        _context = context;
        _env = env;
    }

    public async Task<IEnumerable<ProjectTaskTreeViewModel>> GetTaskTreeAsync(int projectId, int employeeId, bool ignoreAccessCheck = false)
    {
        if (!ignoreAccessCheck)
            await _accessService.EnsureIsMemberAsync(projectId, employeeId);

        IQueryable<ProjectTask> baseQuery = _context.ProjectTasks
            .AsNoTracking()
            .Where(t => t.ProjectId == projectId)
            .Include(t => t.CreatedByEmployee)
            .Include(t => t.Assignments)
                .ThenInclude(a => a.Employee)
            .Include(t => t.ChecklistItems);

        List<ProjectTask> tasks;
        try
        {
            tasks = await baseQuery
                .Include(t => t.Updates)
                .ToListAsync();
        }
        catch (SqlException ex) when (IsMissingWorklogSchema(ex))
        {
            tasks = await baseQuery.ToListAsync();
        }

        var isSystemAdmin = ignoreAccessCheck || _accessService.IsSystemAdmin();
        var role = isSystemAdmin ? ProjectMemberRole.ProjectOwner : await _accessService.GetRoleAsync(projectId, employeeId);
        var isArchived = await IsProjectArchivedAsync(projectId);

        return BuildTaskTree(tasks, null, 0, employeeId, role, isSystemAdmin, isArchived, false);
    }

    public async Task<ProjectTaskTreeViewModel?> GetTaskDetailsAsync(int taskId, int employeeId)
    {
        ProjectTask? task;
        var worklogAvailable = true;

        try
        {
            task = await _unitOfWork.ProjectTasks.GetWithDetailsAsync(taskId);
        }
        catch (SqlException ex) when (IsMissingWorklogSchema(ex))
        {
            task = await GetTaskDetailsWithoutWorklogAsync(taskId);
            worklogAvailable = false;
        }

        if (task == null) return null;
        await _accessService.EnsureIsMemberAsync(task.ProjectId, employeeId);

        await EnsureCanViewTaskAsync(task, employeeId);

        var vm = MapToTreeViewModel(task, 0, employeeId);
        var role = await _accessService.GetRoleAsync(task.ProjectId, employeeId);
        if (!_accessService.IsSystemAdmin()
            && role != ProjectMemberRole.ProjectOwner
            && role != ProjectMemberRole.ProjectManager
            && role != ProjectMemberRole.ProjectViewer)
        {
            vm.SubTasks = FilterVisibleSubTasks(vm.SubTasks, employeeId);
            NormalizeTaskDepths(vm.SubTasks, 1);
        }

        var isArchived = await IsProjectArchivedAsync(task.ProjectId);
        vm.IsArchived = isArchived;
        var isOverdue = IsTaskOverdue(task);
        var canManageTaskNode = !isArchived && await CanManageTaskNodeAsync(task, employeeId);
        var canAdjustOverdueTimeline = !isArchived
            && isOverdue
            && await CanAdjustOverdueTaskTimelineAsync(task, employeeId);

        vm.CanManageTask = isOverdue ? canAdjustOverdueTimeline : canManageTaskNode;
        vm.CanManageMembers = !isOverdue && vm.CanManageTask;
        vm.CanCompleteTask = !isOverdue && !isArchived && await CanCompleteTaskAsync(task, employeeId);
        vm.CanUndoDoneTask = !isOverdue && !isArchived && await CanUndoDoneTaskAsync(task, employeeId);
        vm.CanRejectDoneTask = !isOverdue && !isArchived && await CanRejectDoneTaskAsync(task, employeeId);
        vm.CanPostUpdate = !isOverdue && !isArchived && await CanPostTaskUpdateAsync(task, employeeId);
        vm.WorklogAvailable = worklogAvailable;
        return vm;
    }

    public async Task<int> CreateTaskAsync(ProjectTaskCreateViewModel vm, int creatorEmployeeId)
    {
        await EnsureCanCreateTaskAsync(vm, creatorEmployeeId);
        await ValidateTaskScheduleAsync(vm.ProjectId, vm.ParentTaskId, vm.StartDate, vm.DueDate);

        var task = new ProjectTask
        {
            ProjectId = vm.ProjectId,
            ParentTaskId = vm.ParentTaskId,
            Title = vm.Title,
            Description = vm.Description,
            Priority = vm.Priority,
            StartDate = vm.StartDate,
            DueDate = vm.DueDate,
            EstimatedHours = vm.EstimatedHours,
            ProgressMode = vm.ProgressMode,
            CreatedByEmployeeId = creatorEmployeeId,
            CreatedDate = DateTime.Now,
            Status = ProjectTaskStatus.Todo
        };

        await _unitOfWork.ProjectTasks.AddAsync(task);
        await _unitOfWork.SaveChangesAsync();

        var newAssignees = await SyncAssignmentsAsync(task, vm.AssigneeIds, creatorEmployeeId);
        await CreateSystemUpdateAsync(task.ProjectTaskId, creatorEmployeeId,
            task.ParentTaskId.HasValue ? "Đã tạo subtask." : "Đã tạo task.");

        if (newAssignees.Count > 0)
        {
            var creatorName = await GetSingleEmployeeNameAsync(creatorEmployeeId);
            foreach (var empId in newAssignees.Where(id => id != creatorEmployeeId))
                await NotifyTaskAssignedAsync(empId, creatorName, task);
        }

        return task.ProjectTaskId;
    }

    public async Task UpdateTaskAsync(ProjectTaskEditViewModel vm, int employeeId)
    {
        var task = await _unitOfWork.ProjectTasks.GetByIdAsync(vm.ProjectTaskId)
            ?? throw new InvalidOperationException("Task không tồn tại.");

        if (IsTaskOverdue(task))
        {
            var canAdjustOverdueTimeline = await CanAdjustOverdueTaskTimelineAsync(task, employeeId);
            if (!canAdjustOverdueTimeline)
                throw new InvalidOperationException("Task đã quá hạn. Không thể thao tác thêm trên task này.");

            if (!await IsTimelineOnlyUpdateAsync(task, vm))
                throw new InvalidOperationException("Task đã quá hạn. Chỉ Admin/Owner/Manager được chỉnh sửa timeline (ngày bắt đầu và hạn hoàn thành).");
        }

        await EnsureCanManageTaskNodeAsync(task, employeeId);
        await ValidateTaskScheduleAsync(task.ProjectId, task.ParentTaskId, vm.StartDate, vm.DueDate, task.ProjectTaskId);

        var oldTitle = task.Title;
        var oldStartDate = task.StartDate;
        var oldDueDate = task.DueDate;
        var changeLogContent = BuildTaskChangeLogContent(oldTitle, vm.Title, oldStartDate, vm.StartDate, oldDueDate, vm.DueDate);

        var wasDone = task.Status == ProjectTaskStatus.Done;
        if (wasDone && vm.Status != ProjectTaskStatus.Done)
            throw new InvalidOperationException("Task đã hoàn thành nên không thể hoàn tác trạng thái.");

        if (!wasDone && vm.Status == ProjectTaskStatus.Done)
        {
            await EnsureCanCompleteTaskAsync(task, employeeId);
            await EnsureAllChildrenDoneAsync(task.ProjectTaskId);
        }

        task.Title = vm.Title;
        task.Description = vm.Description;
        task.Priority = vm.Priority;
        task.StartDate = vm.StartDate;
        task.DueDate = vm.DueDate;
        task.EstimatedHours = vm.EstimatedHours;
        task.ActualHours = vm.ActualHours;
        task.Status = vm.Status;
        task.ProgressMode = vm.ProgressMode;
        task.ModifiedDate = DateTime.Now;

        if (!wasDone && vm.Status != ProjectTaskStatus.Done && vm.ProgressMode == ProgressMode.Manual)
            task.Progress = vm.Progress;

        if (wasDone)
        {
            task.Status = ProjectTaskStatus.Done;
            task.Progress = 100;
            task.CompletedDate ??= DateTime.Now;
        }
        else if (vm.Status == ProjectTaskStatus.Done)
        {
            task.Progress = 100;
            task.CompletedDate = DateTime.Now;
        }
        else if (vm.Status == ProjectTaskStatus.Cancelled)
        {
            task.Progress = 0;
            task.CompletedDate = null;
        }
        else if (vm.Status != ProjectTaskStatus.Done)
        {
            task.CompletedDate = null;
        }

        _unitOfWork.ProjectTasks.Update(task);
        await _unitOfWork.SaveChangesAsync();

        await SyncAssignmentsAsync(task, vm.AssigneeIds, employeeId);

        await SyncTaskProgressFromAssignmentsOrEngineAsync(task.ProjectTaskId);
        await _progressService.BubbleUpParentProgressAsync(task.ProjectTaskId);
        await _progressService.RecalculateProjectProgressAsync(task.ProjectId);
        await CreateSystemUpdateAsync(task.ProjectTaskId, employeeId, "Đã cập nhật thông tin task.");

        if (!string.IsNullOrWhiteSpace(changeLogContent))
            await CreateTaskChangeLogAsync(task.ProjectTaskId, employeeId, changeLogContent);
    }

    public async Task DeleteTaskAsync(int taskId, int employeeId)
    {
        var task = await _unitOfWork.ProjectTasks.GetByIdAsync(taskId)
            ?? throw new InvalidOperationException("Task không tồn tại.");

        EnsureTaskNotOverdueForOperation(task);

        await EnsureCanManageTaskNodeAsync(task, employeeId);
        if (task.Status == ProjectTaskStatus.Done)
            throw new InvalidOperationException("Task đã hoàn thành nên không thể xoá hoặc hoàn tác.");

        var role = await _accessService.GetRoleAsync(task.ProjectId, employeeId);
        if (role == ProjectMemberRole.ProjectStaff)
            throw new UnauthorizedAccessException("ProjectStaff không được xoá task.");

        var projectId = task.ProjectId;
        var parentTaskId = task.ParentTaskId;

        _unitOfWork.ProjectTasks.Remove(task);
        await _unitOfWork.SaveChangesAsync();

        if (parentTaskId.HasValue)
            await _progressService.BubbleUpParentProgressAsync(parentTaskId.Value);

        await _progressService.RecalculateProjectProgressAsync(projectId);
    }

    public async Task UpdateStatusAsync(UpdateTaskStatusViewModel vm, int employeeId)
    {
        var task = await _unitOfWork.ProjectTasks.GetByIdAsync(vm.ProjectTaskId)
            ?? throw new InvalidOperationException("Task không tồn tại.");

        EnsureTaskNotOverdueForOperation(task);

        await _accessService.EnsureIsMemberAsync(task.ProjectId, employeeId);
        await EnsureCanViewTaskAsync(task, employeeId);

        if (vm.Status != ProjectTaskStatus.Done)
            await EnsureCanManageTaskNodeAsync(task, employeeId);

        if (task.Status == ProjectTaskStatus.Done)
        {
            if (vm.Status != ProjectTaskStatus.Done)
            {
                var reason = vm.Reason?.Trim();
                if (string.IsNullOrWhiteSpace(reason))
                    throw new InvalidOperationException("Vui lòng nhập lý do hoàn tác/reject task.");

                if (vm.IsManagerReject)
                {
                    await EnsureCanRejectDoneTaskAsync(task, employeeId);
                    await RejectTaskWithCascadeAsync(task, employeeId, reason, vm.RejectSubTaskIds);
                }
                else
                {
                    await EnsureCanUndoDoneTaskAsync(task, employeeId);
                    await ReopenDoneTaskAsync(task, employeeId, reason, vm.Status);
                }

                return;
            }

            return;
        }

        if (vm.Status == ProjectTaskStatus.Done)
        {
            await EnsureCanCompleteTaskAsync(task, employeeId);
            await EnsureAllChildrenDoneAsync(task.ProjectTaskId);

            var completion = await TryCompleteCurrentAssigneeAsync(task, employeeId);
            if (completion.handled)
            {
                await _progressService.BubbleUpParentProgressAsync(task.ProjectTaskId);
                await _progressService.RecalculateProjectProgressAsync(task.ProjectId);
                if (completion.changed)
                {
                    await CreateSystemUpdateAsync(task.ProjectTaskId, employeeId,
                        "Đã xác nhận hoàn thành phần việc của bạn.");

                    if (task.Status == ProjectTaskStatus.Done)
                        await NotifyTaskDoneAsync(task, employeeId);
                }

                return;
            }
        }

        task.Status = vm.Status;
        task.ModifiedDate = DateTime.Now;

        if (vm.Status == ProjectTaskStatus.Done)
        {
            task.Progress = 100;
            task.CompletedDate = DateTime.Now;
        }
        else if (vm.Status == ProjectTaskStatus.Cancelled)
        {
            task.Progress = 0;
            task.CompletedDate = null;
        }
        else
        {
            task.CompletedDate = null;
        }

        _unitOfWork.ProjectTasks.Update(task);
        await _unitOfWork.SaveChangesAsync();

        await _progressService.BubbleUpParentProgressAsync(task.ProjectTaskId);
        await _progressService.RecalculateProjectProgressAsync(task.ProjectId);
        await CreateSystemUpdateAsync(task.ProjectTaskId, employeeId,
            $"Đã cập nhật trạng thái thành {TaskStatusLabel(task.Status)}.");

        if (vm.Status == ProjectTaskStatus.Done)
            await NotifyTaskDoneAsync(task, employeeId);
    }

    public async Task UpdateProgressAsync(UpdateTaskProgressViewModel vm, int employeeId)
    {
        var task = await _unitOfWork.ProjectTasks.GetByIdAsync(vm.ProjectTaskId)
            ?? throw new InvalidOperationException("Task không tồn tại.");

        EnsureTaskNotOverdueForOperation(task);

        await _accessService.EnsureIsMemberAsync(task.ProjectId, employeeId);
        await EnsureCanViewTaskAsync(task, employeeId);

        if (vm.Progress < 100)
            await EnsureCanManageTaskNodeAsync(task, employeeId);

        if (task.Status == ProjectTaskStatus.Done)
        {
            if (vm.Progress < 100)
                throw new InvalidOperationException("Task đã hoàn thành nên không thể giảm tiến độ.");

            return;
        }

        if (task.ProgressMode == ProgressMode.Auto)
            throw new InvalidOperationException("Task đang ở chế độ Auto. Chuyển sang Manual để nhập % thủ công.");

        var hasAssignees = await _context.ProjectTaskAssignments
            .AsNoTracking()
            .AnyAsync(a => a.ProjectTaskId == task.ProjectTaskId);

        if (hasAssignees && vm.Progress < 100)
            throw new InvalidOperationException("Task có người nhận việc sẽ tự tính % theo số người đã xác nhận hoàn thành.");

        if (vm.Progress >= 100)
        {
            await EnsureCanCompleteTaskAsync(task, employeeId);
            await EnsureAllChildrenDoneAsync(task.ProjectTaskId);

            var completion = await TryCompleteCurrentAssigneeAsync(task, employeeId);
            if (completion.handled)
            {
                await _progressService.BubbleUpParentProgressAsync(task.ProjectTaskId);
                await _progressService.RecalculateProjectProgressAsync(task.ProjectId);
                if (completion.changed)
                {
                    await CreateSystemUpdateAsync(task.ProjectTaskId, employeeId,
                        "Đã xác nhận hoàn thành phần việc của bạn.");
                }

                return;
            }

            task.Progress = 100;
            task.Status = ProjectTaskStatus.Done;
            task.CompletedDate = DateTime.Now;
        }
        else
        {
            task.Progress = vm.Progress;
        }

        task.ModifiedDate = DateTime.Now;
        _unitOfWork.ProjectTasks.Update(task);
        await _unitOfWork.SaveChangesAsync();

        await _progressService.BubbleUpParentProgressAsync(task.ProjectTaskId);
        await _progressService.RecalculateProjectProgressAsync(task.ProjectId);
        await CreateSystemUpdateAsync(task.ProjectTaskId, employeeId,
            $"Đã cập nhật tiến độ thành {task.Progress:0}%.");
    }

    public async Task AssignMembersAsync(int taskId, List<int> employeeIds, int actorEmployeeId)
    {
        var task = await _unitOfWork.ProjectTasks.GetByIdAsync(taskId)
            ?? throw new InvalidOperationException("Task không tồn tại.");

        EnsureTaskNotOverdueForOperation(task);

        await EnsureCanManageTaskNodeAsync(task, actorEmployeeId);

        var newAssignees = await SyncAssignmentsAsync(task, employeeIds, actorEmployeeId);
        await SyncTaskProgressFromAssignmentsOrEngineAsync(task.ProjectTaskId);
        await _progressService.BubbleUpParentProgressAsync(task.ProjectTaskId);
        await _progressService.RecalculateProjectProgressAsync(task.ProjectId);

        var assigneeNames = await GetEmployeeNamesAsync(employeeIds);
        var updateContent = assigneeNames.Count > 0
            ? $"Đã phân công thực hiện cho: {string.Join(", ", assigneeNames)}."
            : "Đã xoá toàn bộ người thực hiện khỏi task.";
        await CreateSystemUpdateAsync(task.ProjectTaskId, actorEmployeeId, updateContent);

        if (newAssignees.Count > 0)
        {
            var actorName = await GetSingleEmployeeNameAsync(actorEmployeeId);
            foreach (var empId in newAssignees.Where(id => id != actorEmployeeId))
                await NotifyTaskAssignedAsync(empId, actorName, task);
        }
    }

    public async Task<ChecklistItemViewModel> AddChecklistItemAsync(AddChecklistItemViewModel vm, int employeeId)
    {
        var task = await _unitOfWork.ProjectTasks.GetByIdAsync(vm.ProjectTaskId)
            ?? throw new InvalidOperationException("Task không tồn tại.");

        EnsureTaskNotOverdueForOperation(task);

        await EnsureCanManageTaskNodeAsync(task, employeeId);
        if (task.Status == ProjectTaskStatus.Done)
            throw new InvalidOperationException("Task đã hoàn thành nên không thể thêm checklist mới.");

        var maxOrder = await _context.ProjectTaskChecklistItems
            .Where(c => c.ProjectTaskId == vm.ProjectTaskId)
            .Select(c => (int?)c.SortOrder)
            .MaxAsync() ?? -1;

        var item = new ProjectTaskChecklistItem
        {
            ProjectTaskId = vm.ProjectTaskId,
            Title = vm.Title,
            SortOrder = maxOrder + 1,
            CreatedDate = DateTime.Now
        };

        await _context.ProjectTaskChecklistItems.AddAsync(item);
        await _context.SaveChangesAsync();

        await SyncTaskProgressFromAssignmentsOrEngineAsync(task.ProjectTaskId);
        await _progressService.BubbleUpParentProgressAsync(task.ProjectTaskId);
        await _progressService.RecalculateProjectProgressAsync(task.ProjectId);

        return new ChecklistItemViewModel
        {
            ProjectTaskChecklistItemId = item.ProjectTaskChecklistItemId,
            ProjectTaskId = item.ProjectTaskId,
            Title = item.Title,
            IsDone = item.IsDone,
            SortOrder = item.SortOrder,
            CompletedDate = item.CompletedDate
        };
    }

    public async Task<int> ToggleChecklistItemAsync(int checklistItemId, int employeeId)
    {
        var item = await _context.ProjectTaskChecklistItems
            .Include(c => c.ProjectTask)
            .FirstOrDefaultAsync(c => c.ProjectTaskChecklistItemId == checklistItemId)
            ?? throw new InvalidOperationException("Checklist item không tồn tại.");

        EnsureTaskNotOverdueForOperation(item.ProjectTask);

        await _accessService.EnsureIsMemberAsync(item.ProjectTask.ProjectId, employeeId);
        await EnsureCanViewTaskAsync(item.ProjectTask, employeeId);

        if (item.ProjectTask.Status == ProjectTaskStatus.Done)
            throw new InvalidOperationException("Task đã hoàn thành nên không thể thay đổi checklist.");

        if (item.IsDone)
            throw new InvalidOperationException("Checklist đã hoàn thành nên không thể bỏ tick.");

        await EnsureCanCompleteTaskAsync(item.ProjectTask, employeeId);

        item.IsDone = true;
        item.CompletedDate = DateTime.Now;
        item.CompletedByEmployeeId = employeeId;

        _context.ProjectTaskChecklistItems.Update(item);
        await _context.SaveChangesAsync();

        await SyncTaskProgressFromAssignmentsOrEngineAsync(item.ProjectTaskId);
        await _progressService.BubbleUpParentProgressAsync(item.ProjectTaskId);
        await _progressService.RecalculateProjectProgressAsync(item.ProjectTask.ProjectId);

        return item.ProjectTaskId;
    }

    public async Task<int> DeleteChecklistItemAsync(int checklistItemId, int employeeId)
    {
        var item = await _context.ProjectTaskChecklistItems
            .Include(c => c.ProjectTask)
            .FirstOrDefaultAsync(c => c.ProjectTaskChecklistItemId == checklistItemId)
            ?? throw new InvalidOperationException("Checklist item không tồn tại.");

        EnsureTaskNotOverdueForOperation(item.ProjectTask);

        await EnsureCanManageTaskNodeAsync(item.ProjectTask, employeeId);
        if (item.ProjectTask.Status == ProjectTaskStatus.Done || item.IsDone)
            throw new InvalidOperationException("Không thể xoá checklist đã hoàn thành.");

        _context.ProjectTaskChecklistItems.Remove(item);
        await _context.SaveChangesAsync();

        await SyncTaskProgressFromAssignmentsOrEngineAsync(item.ProjectTaskId);
        await _progressService.BubbleUpParentProgressAsync(item.ProjectTaskId);
        await _progressService.RecalculateProjectProgressAsync(item.ProjectTask.ProjectId);

        return item.ProjectTaskId;
    }

    public async Task AddTaskUpdateAsync(PostTaskUpdateViewModel vm, int employeeId)
    {
        var task = await _unitOfWork.ProjectTasks.GetByIdAsync(vm.ProjectTaskId)
            ?? throw new InvalidOperationException("Task không tồn tại.");

        EnsureTaskNotOverdueForOperation(task);

        await _accessService.EnsureIsMemberAsync(task.ProjectId, employeeId);
        await EnsureCanViewTaskAsync(task, employeeId);
        await EnsureCanPostTaskUpdateAsync(task, employeeId);

        var attachments = vm.Attachments?.Where(file => file is { Length: > 0 }).ToList() ?? new List<IFormFile>();
        if (string.IsNullOrWhiteSpace(vm.Content) && attachments.Count == 0)
            throw new InvalidOperationException("Vui lòng nhập nội dung hoặc đính kèm tệp.");

        var validation = FileUploadHelper.ValidateFiles(attachments, FileUploadHelper.AllowedAllExtensions);
        if (validation != ValidationResult.Success)
            throw new InvalidOperationException(validation!.ErrorMessage ?? "Tệp đính kèm không hợp lệ.");

        var update = new ProjectTaskUpdate
        {
            ProjectTaskId = task.ProjectTaskId,
            SenderEmployeeId = employeeId,
            Content = string.IsNullOrWhiteSpace(vm.Content)
                ? "Đã gửi tệp đính kèm."
                : vm.Content.Trim(),
            StatusSnapshot = task.Status,
            ProgressSnapshot = task.Progress,
            CreatedDate = DateTime.Now
        };

        if (attachments.Count > 0)
            await SaveAttachmentsAsync(update.Attachments, attachments, employeeId);

        try
        {
            await _context.ProjectTaskUpdates.AddAsync(update);
            task.ModifiedDate = DateTime.Now;
            _unitOfWork.ProjectTasks.Update(task);
            await _context.SaveChangesAsync();
        }
        catch (SqlException ex) when (IsMissingWorklogSchema(ex))
        {
            throw new InvalidOperationException("Cơ sở dữ liệu chưa cập nhật phần nhật ký công việc. Cần chạy database update trước khi gửi cập nhật/file.");
        }

        await _logService.LogAsync(null, "TaskUpdate", "ProjectTask", null,
            new { task.ProjectTaskId, task.ProjectId, update.ProjectTaskUpdateId }, null);

        await NotifyAssigneesForTaskUpdateAsync(task, employeeId, update.Content);
    }

    private async Task NotifyAssigneesForTaskUpdateAsync(ProjectTask task, int senderEmployeeId, string updateContent)
    {
        var assigneeUserIds = await _context.ProjectTaskAssignments
            .AsNoTracking()
            .Where(a => a.ProjectTaskId == task.ProjectTaskId && a.EmployeeId != senderEmployeeId)
            .Join(_context.Users.AsNoTracking(), a => a.EmployeeId, u => u.EmployeeId, (a, u) => u.UserId)
            .Distinct()
            .ToListAsync();

        var ownerUserIds = await _context.ProjectMembers
            .AsNoTracking()
            .Where(pm => pm.ProjectId == task.ProjectId
                && pm.Role == ProjectMemberRole.ProjectOwner)
            .Where(pm => pm.EmployeeId != senderEmployeeId)
            .Join(_context.Users.AsNoTracking(), pm => pm.EmployeeId, u => u.EmployeeId, (pm, u) => u.UserId)
            .Distinct()
            .ToListAsync();

        var targetUserIds = assigneeUserIds
            .Concat(ownerUserIds)
            .Distinct()
            .ToList();

        if (targetUserIds.Count == 0)
            return;

        var senderName = await _context.Employees
            .AsNoTracking()
            .Where(e => e.EmployeeId == senderEmployeeId)
            .Select(e => e.FullName)
            .FirstOrDefaultAsync() ?? "Một thành viên";

        var projectName = await GetProjectNameAsync(task.ProjectId);

        var shortContent = string.IsNullOrWhiteSpace(updateContent)
            ? "Có cập nhật nhật ký công việc mới."
            : updateContent.Length > 140
                ? $"{updateContent[..140]}..."
                : updateContent;

        var title = $"Task cập nhật: {task.Title}";
        var message = $"{senderName} vừa cập nhật nhật ký công việc. {shortContent}";
        var targetUrl = $"/Project/Details/{task.ProjectId}?openTaskId={task.ProjectTaskId}";
        var telegramText =
            $"📝 <b>Nhật ký task cập nhật</b>\n" +
            $"👤 Người cập nhật: {H(senderName)}\n" +
            $"📁 Dự án: {H(projectName)}\n" +
            $"🔖 Task: {H(task.Title)}\n" +
            $"💬 {H(shortContent)}";

        foreach (var userId in targetUserIds)
            await _notificationService.CreateAsync(userId, title, message, targetUrl, telegramText: telegramText, telegramCategory: TelegramNotificationCategory.Task);
    }

    public async Task<ProjectTaskAttachment?> GetAttachmentAsync(int attachmentId, int employeeId)
    {
        ProjectTaskAttachment? attachment;
        try
        {
            attachment = await _context.ProjectTaskAttachments
                .Include(a => a.ProjectTaskUpdate)
                    .ThenInclude(u => u.ProjectTask)
                .FirstOrDefaultAsync(a => a.ProjectTaskAttachmentId == attachmentId);
        }
        catch (SqlException ex) when (IsMissingWorklogSchema(ex))
        {
            throw new InvalidOperationException("Cơ sở dữ liệu chưa cập nhật phần file đính kèm của task.");
        }

        if (attachment == null)
            return null;

        await _accessService.EnsureIsMemberAsync(attachment.ProjectTaskUpdate.ProjectTask.ProjectId, employeeId);
        await EnsureCanViewTaskAsync(attachment.ProjectTaskUpdate.ProjectTask, employeeId);
        return attachment;
    }

    private async Task EnsureCanViewTaskAsync(ProjectTask task, int employeeId)
    {
        if (_accessService.IsSystemAdmin())
            return;

        var role = await _accessService.GetRoleAsync(task.ProjectId, employeeId);
        if (role == ProjectMemberRole.ProjectOwner)
            return;

        if (role == ProjectMemberRole.ProjectManager || role == ProjectMemberRole.ProjectViewer)
            return;

        var isAssigned = await IsTaskAssigneeAsync(task.ProjectTaskId, employeeId);

        if (!isAssigned)
            throw new UnauthorizedAccessException("Bạn không có quyền xem task này.");
    }

    private async Task EnsureCanPostTaskUpdateAsync(ProjectTask task, int employeeId)
    {
        if (await CanPostTaskUpdateAsync(task, employeeId))
            return;

        throw new UnauthorizedAccessException("Bạn chỉ có quyền xem task này, không thể gửi cập nhật.");
    }

    private async Task<bool> CanPostTaskUpdateAsync(ProjectTask task, int employeeId)
    {
        if (task.Status == ProjectTaskStatus.Done)
            return false;

        if (_accessService.IsSystemAdmin())
            return true;

        var role = await _accessService.GetRoleAsync(task.ProjectId, employeeId);
        return role switch
        {
            ProjectMemberRole.ProjectOwner => true,
            ProjectMemberRole.ProjectManager => await IsAssignedToTaskOrAncestorAsync(task, employeeId),
            ProjectMemberRole.ProjectStaff => await IsTaskAssigneeAsync(task.ProjectTaskId, employeeId),
            _ => false
        };
    }

    private async Task EnsureCanCreateTaskAsync(ProjectTaskCreateViewModel vm, int employeeId)
    {
        if (!vm.ParentTaskId.HasValue)
        {
            await _accessService.EnsureCanManageTaskAsync(vm.ProjectId, employeeId);
            return;
        }

        var parentTask = await _unitOfWork.ProjectTasks.GetByIdAsync(vm.ParentTaskId.Value)
            ?? throw new InvalidOperationException("Task cha không tồn tại.");

        if (parentTask.ProjectId != vm.ProjectId)
            throw new InvalidOperationException("Task cha không thuộc dự án này.");

        EnsureTaskNotOverdueForOperation(parentTask);

        await EnsureCanManageTaskNodeAsync(parentTask, employeeId);
    }

    private async Task EnsureCanManageTaskNodeAsync(ProjectTask task, int employeeId)
    {
        if (await CanManageTaskNodeAsync(task, employeeId))
            return;

        throw new UnauthorizedAccessException("Bạn không có quyền quản lý task này.");
    }

    private async Task<bool> CanManageTaskNodeAsync(ProjectTask task, int employeeId)
    {
        if (_accessService.IsSystemAdmin())
            return true;

        var role = await _accessService.GetRoleAsync(task.ProjectId, employeeId);
        if (role == ProjectMemberRole.ProjectOwner)
            return true;

        if (role != ProjectMemberRole.ProjectManager)
            return false;

        return await IsAssignedToTaskOrAncestorAsync(task, employeeId);
    }

    private static bool IsTaskOverdue(ProjectTask task)
        => task.DueDate.HasValue
            && task.DueDate.Value.Date < DateTime.Today
            && task.Status != ProjectTaskStatus.Done
            && task.Status != ProjectTaskStatus.Cancelled;

    private static void EnsureTaskNotOverdueForOperation(ProjectTask task)
    {
        if (IsTaskOverdue(task))
            throw new InvalidOperationException("Task đã quá hạn. Không thể thao tác thêm trên task này.");
    }

    private async Task<bool> CanAdjustOverdueTaskTimelineAsync(ProjectTask task, int employeeId)
    {
        if (_accessService.IsSystemAdmin())
            return true;

        var role = await _accessService.GetRoleAsync(task.ProjectId, employeeId);
        if (role == ProjectMemberRole.ProjectOwner)
            return true;

        return role == ProjectMemberRole.ProjectManager
            && await IsAssignedToTaskOrAncestorAsync(task, employeeId);
    }

    private async Task<bool> IsTimelineOnlyUpdateAsync(ProjectTask task, ProjectTaskEditViewModel vm)
    {
        var timelineChanged = task.StartDate?.Date != vm.StartDate?.Date
            || task.DueDate?.Date != vm.DueDate?.Date;
        if (!timelineChanged)
            return false;

        var currentAssigneeIds = await _context.ProjectTaskAssignments
            .AsNoTracking()
            .Where(a => a.ProjectTaskId == task.ProjectTaskId)
            .Select(a => a.EmployeeId)
            .OrderBy(id => id)
            .ToListAsync();

        var requestedAssigneeIds = (vm.AssigneeIds ?? new List<int>())
            .Distinct()
            .OrderBy(id => id)
            .ToList();

        return string.Equals(task.Title, vm.Title, StringComparison.Ordinal)
            && string.Equals(task.Description ?? string.Empty, vm.Description ?? string.Empty, StringComparison.Ordinal)
            && task.Priority == vm.Priority
            && task.Status == vm.Status
            && task.EstimatedHours == vm.EstimatedHours
            && task.ActualHours == vm.ActualHours
            && task.ProgressMode == vm.ProgressMode
            && task.Progress == vm.Progress
            && currentAssigneeIds.SequenceEqual(requestedAssigneeIds);
    }

    private async Task<bool> IsAssignedToTaskOrAncestorAsync(ProjectTask task, int employeeId)
    {
        var currentTaskId = task.ProjectTaskId;
        var parentTaskId = task.ParentTaskId;
        var visited = new HashSet<int>();

        while (visited.Add(currentTaskId))
        {
            if (await IsTaskAssigneeAsync(currentTaskId, employeeId))
                return true;

            if (!parentTaskId.HasValue)
                return false;

            var parent = await _context.ProjectTasks
                .AsNoTracking()
                .Where(t => t.ProjectTaskId == parentTaskId.Value)
                .Select(t => new { t.ProjectTaskId, t.ParentTaskId })
                .FirstOrDefaultAsync();

            if (parent == null)
                return false;

            currentTaskId = parent.ProjectTaskId;
            parentTaskId = parent.ParentTaskId;
        }

        return false;
    }

    private async Task<bool> IsTaskAssigneeAsync(int taskId, int employeeId)
        => await _context.ProjectTaskAssignments
            .AsNoTracking()
            .AnyAsync(a => a.ProjectTaskId == taskId && a.EmployeeId == employeeId);

    private static List<ProjectTaskTreeViewModel> FilterVisibleSubTasks(
        IEnumerable<ProjectTaskTreeViewModel> tasks, int employeeId)
    {
        var result = new List<ProjectTaskTreeViewModel>();

        foreach (var task in tasks)
        {
            var visibleChildren = FilterVisibleSubTasks(task.SubTasks, employeeId);

            if (task.AssigneeIds.Contains(employeeId))
            {
                task.SubTasks = visibleChildren;
                result.Add(task);
            }
            else
            {
                result.AddRange(visibleChildren);
            }
        }

        return result;
    }

    private static void NormalizeTaskDepths(IEnumerable<ProjectTaskTreeViewModel> tasks, int depth)
    {
        foreach (var task in tasks)
        {
            task.Depth = depth;
            NormalizeTaskDepths(task.SubTasks, depth + 1);
        }
    }

    private async Task EnsureAllChildrenDoneAsync(int taskId)
    {
        var hasActiveChildren = await _context.ProjectTasks
            .AnyAsync(t => t.ParentTaskId == taskId
                && t.Status != ProjectTaskStatus.Done
                && t.Status != ProjectTaskStatus.Cancelled);

        if (hasActiveChildren)
            throw new InvalidOperationException("Cần hoàn thành hoặc huỷ tất cả task con trước khi đánh dấu task cha là hoàn thành.");
    }

    private async Task EnsureCanCompleteTaskAsync(ProjectTask task, int employeeId)
    {
        if (await CanCompleteTaskAsync(task, employeeId))
            return;

        throw new UnauthorizedAccessException("Chỉ người được giao task này hoặc ProjectOwner/admin hệ thống mới được xác nhận hoàn thành.");
    }

    private async Task<bool> CanCompleteTaskAsync(ProjectTask task, int employeeId)
    {
        if (_accessService.IsSystemAdmin())
            return true;

        var role = await _accessService.GetRoleAsync(task.ProjectId, employeeId);
        if (role == ProjectMemberRole.ProjectOwner)
            return true;

        if (role == ProjectMemberRole.ProjectViewer)
            return false;

        return await _context.ProjectTaskAssignments
            .AsNoTracking()
            .AnyAsync(a => a.ProjectTaskId == task.ProjectTaskId
                && a.EmployeeId == employeeId
                && !a.IsCompleted);
    }

    private async Task EnsureCanUndoDoneTaskAsync(ProjectTask task, int employeeId)
    {
        if (await CanUndoDoneTaskAsync(task, employeeId))
            return;

        throw new UnauthorizedAccessException("Bạn không có quyền hoàn tác task đã hoàn thành.");
    }

    private async Task<bool> CanUndoDoneTaskAsync(ProjectTask task, int employeeId)
    {
        if (task.Status != ProjectTaskStatus.Done)
            return false;

        if (_accessService.IsSystemAdmin())
            return true;

        var role = await _accessService.GetRoleAsync(task.ProjectId, employeeId);
        if (role == ProjectMemberRole.ProjectOwner)
            return true;

        if (role == ProjectMemberRole.ProjectManager && await CanManageTaskNodeAsync(task, employeeId))
            return true;

        return await _context.ProjectTaskAssignments
            .AsNoTracking()
            .AnyAsync(a => a.ProjectTaskId == task.ProjectTaskId
                && a.EmployeeId == employeeId
                && a.IsCompleted);
    }

    private async Task EnsureCanRejectDoneTaskAsync(ProjectTask task, int employeeId)
    {
        if (await CanRejectDoneTaskAsync(task, employeeId))
            return;

        throw new UnauthorizedAccessException("Chỉ ProjectOwner/ProjectManager/Admin mới có quyền reject task đã hoàn thành.");
    }

    private async Task<bool> CanRejectDoneTaskAsync(ProjectTask task, int employeeId)
    {
        if (_accessService.IsSystemAdmin())
            return true;

        var role = await _accessService.GetRoleAsync(task.ProjectId, employeeId);
        if (role == ProjectMemberRole.ProjectOwner)
            return true;

        return role == ProjectMemberRole.ProjectManager
            && await CanManageTaskNodeAsync(task, employeeId);
    }

    private async Task ReopenDoneTaskAsync(ProjectTask task, int employeeId, string reason, ProjectTaskStatus requestedStatus)
    {
        var assignments = await _context.ProjectTaskAssignments
            .Where(a => a.ProjectTaskId == task.ProjectTaskId)
            .ToListAsync();

        if (assignments.Count > 0)
        {
            var assignmentChanged = false;
            var actorAssignment = assignments.FirstOrDefault(a => a.EmployeeId == employeeId);
            if (actorAssignment != null && actorAssignment.IsCompleted)
            {
                actorAssignment.IsCompleted = false;
                actorAssignment.CompletedDate = null;
                _context.ProjectTaskAssignments.Update(actorAssignment);
                assignmentChanged = true;
            }
            else
            {
                var canManageUndo = _accessService.IsSystemAdmin() || await CanRejectDoneTaskAsync(task, employeeId);
                if (!canManageUndo)
                    throw new UnauthorizedAccessException("Bạn chỉ được hoàn tác phần việc do chính bạn hoàn thành.");

                var latestCompleted = assignments
                    .Where(a => a.IsCompleted)
                    .OrderByDescending(a => a.CompletedDate)
                    .FirstOrDefault();

                if (latestCompleted != null)
                {
                    latestCompleted.IsCompleted = false;
                    latestCompleted.CompletedDate = null;
                    _context.ProjectTaskAssignments.Update(latestCompleted);
                    assignmentChanged = true;
                }
            }

            if (!assignmentChanged)
                throw new InvalidOperationException("Task này chưa có phần hoàn thành nào để hoàn tác.");

            ApplyAssignmentCompletionState(task, assignments);
        }
        else
        {
            task.Status = requestedStatus == ProjectTaskStatus.Done
                ? ProjectTaskStatus.InProgress
                : requestedStatus;
            task.CompletedDate = null;
            task.Progress = 0;
            task.ModifiedDate = DateTime.Now;
        }

        _unitOfWork.ProjectTasks.Update(task);
        await _context.SaveChangesAsync();

        await _progressService.BubbleUpParentProgressAsync(task.ProjectTaskId);
        await _progressService.RecalculateProjectProgressAsync(task.ProjectId);

        await CreateTaskChangeLogAsync(task.ProjectTaskId, employeeId,
            $"Hoàn tác trạng thái hoàn thành.{Environment.NewLine}Lý do: {reason}");
        await CreateSystemUpdateAsync(task.ProjectTaskId, employeeId, "Đã hoàn tác trạng thái hoàn thành để tiếp tục xử lý task.");
    }

    private async Task RejectTaskWithCascadeAsync(ProjectTask task, int employeeId, string reason, IEnumerable<int>? selectedSubTaskIds)
    {
        var allNodes = await _context.ProjectTasks
            .AsNoTracking()
            .Where(t => t.ProjectId == task.ProjectId)
            .Select(t => new { t.ProjectTaskId, t.ParentTaskId })
            .ToListAsync();

        var childrenLookup = allNodes.ToLookup(t => t.ParentTaskId, t => t.ProjectTaskId);
        var descendantIds = CollectDescendantTaskIds(task.ProjectTaskId, childrenLookup).ToHashSet();

        var requestedSubTaskIds = (selectedSubTaskIds ?? Enumerable.Empty<int>())
            .Where(id => id > 0 && id != task.ProjectTaskId)
            .Distinct()
            .ToList();

        var invalidSubTaskIds = requestedSubTaskIds
            .Where(id => !descendantIds.Contains(id))
            .ToList();

        if (invalidSubTaskIds.Count > 0)
            throw new InvalidOperationException("Danh sách subtask cần reject không hợp lệ.");

        var taskIdSet = new HashSet<int> { task.ProjectTaskId };
        if (requestedSubTaskIds.Count > 0)
        {
            foreach (var subTaskId in requestedSubTaskIds)
            {
                taskIdSet.Add(subTaskId);
                foreach (var nestedId in CollectDescendantTaskIds(subTaskId, childrenLookup))
                    taskIdSet.Add(nestedId);
            }
        }

        var taskIds = taskIdSet.ToList();

        var taskEntities = await _context.ProjectTasks
            .Where(t => taskIds.Contains(t.ProjectTaskId))
            .ToListAsync();

        var changedTaskIds = new List<int>();
        foreach (var item in taskEntities)
        {
            if (item.Status == ProjectTaskStatus.Cancelled)
                continue;

            if (item.Status != ProjectTaskStatus.InProgress || item.CompletedDate.HasValue || item.Progress > 0)
            {
                item.Status = ProjectTaskStatus.InProgress;
                item.CompletedDate = null;
                item.Progress = 0;
                item.ModifiedDate = DateTime.Now;
                _unitOfWork.ProjectTasks.Update(item);
                changedTaskIds.Add(item.ProjectTaskId);
            }
        }

        var assignments = await _context.ProjectTaskAssignments
            .Where(a => taskIds.Contains(a.ProjectTaskId) && a.IsCompleted)
            .ToListAsync();

        foreach (var assignment in assignments)
        {
            assignment.IsCompleted = false;
            assignment.CompletedDate = null;
            _context.ProjectTaskAssignments.Update(assignment);
        }

        await _context.SaveChangesAsync();

        await _progressService.BubbleUpParentProgressAsync(task.ProjectTaskId);
        await _progressService.RecalculateProjectProgressAsync(task.ProjectId);

        await CreateTaskChangeLogAsync(task.ProjectTaskId, employeeId,
            $"Reject task cần làm lại.{Environment.NewLine}Lý do: {reason}");
        await CreateSystemUpdateAsync(task.ProjectTaskId, employeeId,
            "Task đã bị reject và chuyển về trạng thái cần làm lại.");

        foreach (var childTaskId in changedTaskIds.Where(id => id != task.ProjectTaskId))
        {
            await CreateTaskChangeLogAsync(childTaskId, employeeId,
                $"Task con bị reject theo task cha.{Environment.NewLine}Lý do: {reason}");
        }
    }

    private async Task<List<int>> CollectDescendantTaskIdsAsync(int rootTaskId)
    {
        var allNodes = await _context.ProjectTasks
            .AsNoTracking()
            .Select(t => new { t.ProjectTaskId, t.ParentTaskId })
            .ToListAsync();

        var lookup = allNodes.ToLookup(t => t.ParentTaskId, t => t.ProjectTaskId);
        return CollectDescendantTaskIds(rootTaskId, lookup);
    }

    private static List<int> CollectDescendantTaskIds(int rootTaskId, ILookup<int?, int> lookup)
    {
        var result = new List<int>();
        var queue = new Queue<int>();
        queue.Enqueue(rootTaskId);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            foreach (var childId in lookup[current])
            {
                result.Add(childId);
                queue.Enqueue(childId);
            }
        }

        return result;
    }

    private async Task<bool> CanCompleteTaskOnBehalfAsync(ProjectTask task, int employeeId)
    {
        if (_accessService.IsSystemAdmin())
            return true;

        var role = await _accessService.GetRoleAsync(task.ProjectId, employeeId);
        return role == ProjectMemberRole.ProjectOwner;
    }

    private async Task<bool> IsProjectArchivedAsync(int projectId)
        => await _context.Projects
            .AsNoTracking()
            .Where(p => p.ProjectId == projectId)
            .Select(p => p.IsArchived || p.Status == ProjectStatus.Archived)
            .FirstOrDefaultAsync();

    private async Task<int> GetTaskDepthAsync(int taskId)
    {
        int depth = 0;
        var current = await _unitOfWork.ProjectTasks.GetByIdAsync(taskId);
        while (current?.ParentTaskId != null && depth < 10)
        {
            depth++;
            current = await _unitOfWork.ProjectTasks.GetByIdAsync(current.ParentTaskId.Value);
        }
        return depth;
    }

    public async Task<(DateTime? EffectiveMin, DateTime? EffectiveMax, DateTime? ChildMinStart, DateTime? ChildMaxDue)> GetDateConstraintsForEditAsync(int projectId, int? parentTaskId, int taskId)
    {
        var project = await _unitOfWork.Projects.GetByIdAsync(projectId);
        DateTime? effectiveMin = project?.StartDate;
        DateTime? effectiveMax = project?.EndDate;

        if (parentTaskId.HasValue)
        {
            var parent = await _unitOfWork.ProjectTasks.GetByIdAsync(parentTaskId.Value);
            if (parent != null)
            {
                if (parent.StartDate.HasValue)
                    effectiveMin = effectiveMin.HasValue
                        ? (DateTime?)new[] { effectiveMin.Value.Date, parent.StartDate.Value.Date }.Max()
                        : parent.StartDate;
                if (parent.DueDate.HasValue)
                    effectiveMax = effectiveMax.HasValue
                        ? (DateTime?)new[] { effectiveMax.Value.Date, parent.DueDate.Value.Date }.Min()
                        : parent.DueDate;
            }
        }

        var childDates = await _context.ProjectTasks
            .AsNoTracking()
            .Where(t => t.ParentTaskId == taskId)
            .Select(t => new { t.StartDate, t.DueDate })
            .ToListAsync();

        var childStarts = childDates.Where(c => c.StartDate.HasValue).Select(c => c.StartDate!.Value).ToList();
        var childDues = childDates.Where(c => c.DueDate.HasValue).Select(c => c.DueDate!.Value).ToList();
        DateTime? childMinStart = childStarts.Any() ? childStarts.Min() : null;
        DateTime? childMaxDue = childDues.Any() ? childDues.Max() : null;

        return (effectiveMin, effectiveMax, childMinStart, childMaxDue);
    }

    private async Task ValidateTaskScheduleAsync(int projectId, int? parentTaskId, DateTime? startDate, DateTime? dueDate, int? currentTaskId = null)
    {
        if (startDate.HasValue && dueDate.HasValue && dueDate.Value.Date < startDate.Value.Date)
            throw new InvalidOperationException("Hạn hoàn thành không được nhỏ hơn ngày bắt đầu.");

        var project = await _unitOfWork.Projects.GetByIdAsync(projectId)
            ?? throw new InvalidOperationException("Dự án không tồn tại.");

        ValidateDateWithinRange(startDate, dueDate, project.StartDate, project.EndDate,
            "Khoảng thời gian của task phải nằm trong khoảng thời gian của dự án.");

        if (parentTaskId.HasValue)
        {
            if (!startDate.HasValue || !dueDate.HasValue)
                throw new InvalidOperationException("Subtask phải có ngày bắt đầu và ngày kết thúc.");

            var parent = await _unitOfWork.ProjectTasks.GetByIdAsync(parentTaskId.Value);
            if (parent == null || parent.ProjectId != projectId)
                throw new InvalidOperationException("Task cha không hợp lệ.");

            if (!parent.StartDate.HasValue || !parent.DueDate.HasValue)
                throw new InvalidOperationException("Task cha phải có ngày bắt đầu và ngày kết thúc trước khi tạo hoặc cập nhật subtask.");

            var depth = await GetTaskDepthAsync(parentTaskId.Value);
            if (depth >= 5)
                throw new InvalidOperationException("Không thể tạo task sâu hơn 5 cấp.");

            ValidateDateWithinRange(startDate, dueDate, parent.StartDate, parent.DueDate,
                "Thời gian của subtask phải nằm trong khoảng thời gian của task cha.");
        }

        if (currentTaskId.HasValue)
        {
            var tasks = await _context.ProjectTasks
                .AsNoTracking()
                .Where(t => t.ProjectId == projectId)
                .Select(t => new TaskScheduleNode
                {
                    ProjectTaskId = t.ProjectTaskId,
                    ParentTaskId = t.ParentTaskId,
                    Title = t.Title,
                    StartDate = t.StartDate,
                    DueDate = t.DueDate
                })
                .ToListAsync();

            var descendants = GetDescendants(tasks, currentTaskId.Value);
            if (descendants.Count > 0 && (!startDate.HasValue || !dueDate.HasValue))
                throw new InvalidOperationException("Task cha có subtask phải giữ đủ ngày bắt đầu và ngày kết thúc.");

            foreach (var descendant in descendants)
            {
                ValidateDateWithinRange(descendant.StartDate, descendant.DueDate, startDate, dueDate,
                    $"Task \"{descendant.Title}\" đang nằm ngoài khoảng thời gian mới của task cha.");
            }
        }
    }

    private static void ValidateDateWithinRange(DateTime? childStart, DateTime? childEnd,
        DateTime? rangeStart, DateTime? rangeEnd, string message)
    {
        if (!rangeStart.HasValue && !rangeEnd.HasValue)
            return;

        var providedDates = new[] { childStart, childEnd }
            .Where(date => date.HasValue)
            .Select(date => date!.Value.Date)
            .ToList();

        if (providedDates.Count == 0)
            return;

        if (rangeStart.HasValue && providedDates.Any(date => date < rangeStart.Value.Date))
            throw new InvalidOperationException(message);

        if (rangeEnd.HasValue && providedDates.Any(date => date > rangeEnd.Value.Date))
            throw new InvalidOperationException(message);
    }

    private static List<(int ProjectTaskId, string Title, DateTime? StartDate, DateTime? DueDate)> GetDescendants(
        IEnumerable<TaskScheduleNode> tasks, int currentTaskId)
    {
        var childrenLookup = tasks.ToLookup(task => task.ParentTaskId);

        var result = new List<(int ProjectTaskId, string Title, DateTime? StartDate, DateTime? DueDate)>();

        void Collect(int parentId)
        {
            foreach (var child in childrenLookup[parentId])
            {
                result.Add((child.ProjectTaskId, child.Title, child.StartDate, child.DueDate));
                Collect(child.ProjectTaskId);
            }
        }

        Collect(currentTaskId);
        return result;
    }

    private async Task<ProjectTask?> GetTaskDetailsWithoutWorklogAsync(int taskId)
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
            .Include(t => t.ParentTask)
            .FirstOrDefaultAsync(t => t.ProjectTaskId == taskId);

    private async Task<List<int>> SyncAssignmentsAsync(ProjectTask task, IEnumerable<int>? employeeIds, int actorEmployeeId)
    {
        var targetIds = (employeeIds ?? Enumerable.Empty<int>())
            .Distinct()
            .ToHashSet();

        var existingAssignments = await _context.ProjectTaskAssignments
            .Where(a => a.ProjectTaskId == task.ProjectTaskId)
            .ToListAsync();

        var toRemove = existingAssignments
            .Where(a => !targetIds.Contains(a.EmployeeId))
            .ToList();
        if (toRemove.Count > 0)
            _context.ProjectTaskAssignments.RemoveRange(toRemove);

        var keptEmployeeIds = existingAssignments
            .Where(a => !toRemove.Contains(a))
            .Select(a => a.EmployeeId)
            .ToHashSet();

        var newlyAddedIds = new List<int>();
        foreach (var empId in targetIds)
        {
            if (keptEmployeeIds.Contains(empId))
                continue;

            if (!await _unitOfWork.Projects.IsMemberAsync(task.ProjectId, empId))
                continue;

            await _context.ProjectTaskAssignments.AddAsync(new ProjectTaskAssignment
            {
                ProjectTaskId = task.ProjectTaskId,
                EmployeeId = empId,
                AssignedByEmployeeId = actorEmployeeId,
                AssignedDate = DateTime.Now,
                IsCompleted = false,
                CompletedDate = null
            });
            newlyAddedIds.Add(empId);
        }

        await _context.SaveChangesAsync();
        return newlyAddedIds;
    }

    private async Task SyncTaskProgressFromAssignmentsOrEngineAsync(int taskId)
    {
        var task = await _unitOfWork.ProjectTasks.GetByIdAsync(taskId)
            ?? throw new InvalidOperationException("Task không tồn tại.");

        var assignments = await _context.ProjectTaskAssignments
            .Where(a => a.ProjectTaskId == taskId)
            .ToListAsync();

        if (assignments.Count == 0)
        {
            await _progressService.RecalculateTaskProgressAsync(taskId);
            return;
        }

        ApplyAssignmentCompletionState(task, assignments);
        _unitOfWork.ProjectTasks.Update(task);
        await _context.SaveChangesAsync();
    }

    private async Task<(bool handled, bool changed)> TryCompleteCurrentAssigneeAsync(ProjectTask task, int employeeId)
    {
        var assignments = await _context.ProjectTaskAssignments
            .Where(a => a.ProjectTaskId == task.ProjectTaskId)
            .ToListAsync();

        if (assignments.Count == 0)
            return (false, false);

        if (await CanCompleteTaskOnBehalfAsync(task, employeeId))
        {
            var changed = false;
            foreach (var assignment in assignments.Where(a => !a.IsCompleted))
            {
                assignment.IsCompleted = true;
                assignment.CompletedDate = DateTime.Now;
                _context.ProjectTaskAssignments.Update(assignment);
                changed = true;
            }

            ApplyAssignmentCompletionState(task, assignments);
            _unitOfWork.ProjectTasks.Update(task);
            await _context.SaveChangesAsync();

            return (true, changed);
        }

        var currentAssignment = assignments.FirstOrDefault(a => a.EmployeeId == employeeId);
        if (currentAssignment == null)
            throw new UnauthorizedAccessException("Bạn không được giao task này nên không thể xác nhận hoàn thành.");

        if (currentAssignment.IsCompleted)
            return (true, false);

        currentAssignment.IsCompleted = true;
        currentAssignment.CompletedDate = DateTime.Now;
        _context.ProjectTaskAssignments.Update(currentAssignment);

        ApplyAssignmentCompletionState(task, assignments);
        _unitOfWork.ProjectTasks.Update(task);
        await _context.SaveChangesAsync();

        return (true, true);
    }

    private static void ApplyAssignmentCompletionState(ProjectTask task, IReadOnlyCollection<ProjectTaskAssignment> assignments)
    {
        if (assignments.Count == 0)
            return;

        if (task.Status == ProjectTaskStatus.Cancelled)
        {
            task.Progress = 0;
            task.CompletedDate = null;
            task.ModifiedDate = DateTime.Now;
            return;
        }

        var completedCount = assignments.Count(a => a.IsCompleted);
        var totalCount = assignments.Count;
        var allCompleted = completedCount == totalCount;

        task.Progress = Math.Round((decimal)completedCount * 100m / totalCount, 2);

        if (allCompleted)
        {
            task.Status = ProjectTaskStatus.Done;
            task.CompletedDate ??= DateTime.Now;
        }
        else
        {
            if (task.Status == ProjectTaskStatus.Todo || task.Status == ProjectTaskStatus.Done)
                task.Status = ProjectTaskStatus.InProgress;

            task.CompletedDate = null;
        }

        task.ModifiedDate = DateTime.Now;
    }

    private async Task SaveAttachmentsAsync(ICollection<ProjectTaskAttachment> attachmentCollection, List<IFormFile> files, int uploadedById)
    {
        var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "project-tasks");
        Directory.CreateDirectory(uploadsDir);

        foreach (var file in files)
        {
            if (file.Length == 0)
                continue;

            var secureFileName = FileUploadHelper.GenerateSecureFileName(file.FileName, "task");
            var filePath = Path.Combine(uploadsDir, secureFileName);

            await using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);

            attachmentCollection.Add(new ProjectTaskAttachment
            {
                FileName = file.FileName,
                FilePath = $"/uploads/project-tasks/{secureFileName}",
                FileSize = file.Length,
                ContentType = file.ContentType,
                UploadedByEmployeeId = uploadedById,
                UploadedDate = DateTime.Now
            });
        }
    }

    private async Task CreateSystemUpdateAsync(int taskId, int senderEmployeeId, string content)
    {
        var task = await _context.ProjectTasks
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.ProjectTaskId == taskId);

        if (task == null)
            return;

        try
        {
            await _context.ProjectTaskUpdates.AddAsync(new ProjectTaskUpdate
            {
                ProjectTaskId = taskId,
                SenderEmployeeId = senderEmployeeId,
                Content = content,
                StatusSnapshot = task.Status,
                ProgressSnapshot = task.Progress,
                CreatedDate = DateTime.Now
            });

            await _context.SaveChangesAsync();
        }
        catch (SqlException ex) when (IsMissingWorklogSchema(ex))
        {
            // Ignore worklog write failures until the new tables are migrated.
        }
    }

    private async Task CreateTaskChangeLogAsync(int taskId, int senderEmployeeId, string content)
        => await CreateSystemUpdateAsync(taskId, senderEmployeeId, $"{TaskChangeLogPrefix}\n{content}");

    private static string? BuildTaskChangeLogContent(string oldTitle, string newTitle,
        DateTime? oldStartDate, DateTime? newStartDate, DateTime? oldDueDate, DateTime? newDueDate)
    {
        var changes = new List<string>();

        if (!string.Equals(oldTitle.Trim(), newTitle.Trim(), StringComparison.Ordinal))
            changes.Add($"Đổi tên task: \"{oldTitle}\" → \"{newTitle}\".");

        var timelineChanges = new List<string>();

        // Only log date changes when there was a previous value (skip first-time date assignment)
        if (oldStartDate.HasValue && !SameDate(oldStartDate, newStartDate))
            timelineChanges.Add($"Bắt đầu {FormatLogDate(oldStartDate)} → {FormatLogDate(newStartDate)}");

        if (oldDueDate.HasValue && !SameDate(oldDueDate, newDueDate))
            timelineChanges.Add($"Hạn {FormatLogDate(oldDueDate)} → {FormatLogDate(newDueDate)}");

        if (timelineChanges.Count > 0)
            changes.Add($"Cập nhật timeline: {string.Join("; ", timelineChanges)}.");

        return changes.Count == 0 ? null : string.Join(Environment.NewLine, changes);
    }

    private static bool SameDate(DateTime? left, DateTime? right)
    {
        if (!left.HasValue && !right.HasValue)
            return true;

        if (!left.HasValue || !right.HasValue)
            return false;

        return left.Value.Date == right.Value.Date;
    }

    private static string FormatLogDate(DateTime? value)
        => value.HasValue ? value.Value.ToString("dd/MM/yyyy") : "Chưa đặt";

    private async Task<string> GetSingleEmployeeNameAsync(int employeeId)
        => await _context.Employees
            .Where(e => e.EmployeeId == employeeId)
            .Select(e => e.FullName)
            .FirstOrDefaultAsync() ?? "Một thành viên";

    private async Task<string> GetProjectNameAsync(int projectId)
        => await _context.Projects
            .AsNoTracking()
            .Where(p => p.ProjectId == projectId)
            .Select(p => p.Name)
            .FirstOrDefaultAsync() ?? "Dự án";

    private static string H(string s) => s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

    private async Task NotifyTaskAssignedAsync(int assigneeEmployeeId, string actorName, ProjectTask task)
    {
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.EmployeeId == assigneeEmployeeId);
        if (user == null) return;

        var projectName = await GetProjectNameAsync(task.ProjectId);
        var title = $"Bạn được giao task: {task.Title}";
        var message = $"{actorName} đã giao task này cho bạn.";
        var targetUrl = $"/Project/Details/{task.ProjectId}?openTaskId={task.ProjectTaskId}";
        var telegramText =
            $"📋 <b>Giao việc mới</b>\n" +
            $"👤 Người giao: {H(actorName)}\n" +
            $"📁 Dự án: {H(projectName)}\n" +
            $"🔖 Task: {H(task.Title)}";
        await _notificationService.CreateAsync(user.UserId, title, message, targetUrl, telegramText: telegramText, telegramCategory: TelegramNotificationCategory.Task);
    }

    private async Task NotifyTaskDoneAsync(ProjectTask task, int completingEmployeeId)
    {
        var completingName = await GetSingleEmployeeNameAsync(completingEmployeeId);
        var projectName = await GetProjectNameAsync(task.ProjectId);
        var title = $"Task hoàn thành: {task.Title}";
        var message = $"{completingName} đã hoàn thành task.";
        var targetUrl = $"/Project/Details/{task.ProjectId}?openTaskId={task.ProjectTaskId}";

        var telegramText =
            $"✅ <b>Task hoàn thành</b>\n" +
            $"👤 Người thực hiện: {H(completingName)}\n" +
            $"📁 Dự án: {H(projectName)}\n" +
            $"🔖 Task: {H(task.Title)}";

        var managerUserIds = await _context.ProjectMembers
            .AsNoTracking()
            .Where(pm => pm.ProjectId == task.ProjectId
                && pm.EmployeeId != completingEmployeeId
                && (pm.Role == ProjectMemberRole.ProjectOwner || pm.Role == ProjectMemberRole.ProjectManager))
            .Join(_context.Users.AsNoTracking(), pm => pm.EmployeeId, u => u.EmployeeId, (pm, u) => u.UserId)
            .Distinct()
            .ToListAsync();

        foreach (var userId in managerUserIds)
            await _notificationService.CreateAsync(userId, title, message, targetUrl, telegramText: telegramText, telegramCategory: TelegramNotificationCategory.Task);
    }

    private async Task<List<string>> GetEmployeeNamesAsync(IEnumerable<int> employeeIds)
    {
        var ids = employeeIds.Distinct().ToList();
        if (ids.Count == 0)
            return new List<string>();

        return await _context.Employees
            .Where(e => ids.Contains(e.EmployeeId))
            .OrderBy(e => e.FullName)
            .Select(e => e.FullName)
            .ToListAsync();
    }

    private static ProjectTaskTreeViewModel MapToTreeViewModel(ProjectTask task, int depth, int? currentEmployeeId = null)
    {
        var updateViewModels = task.Updates?
            .OrderBy(u => u.CreatedDate)
            .Select(MapProjectTaskUpdate)
            .ToList() ?? new List<ProjectTaskUpdateViewModel>();
        var changeLogs = updateViewModels.Where(u => u.IsChangeLog).ToList();
        var workUpdates = updateViewModels.Where(u => !u.IsChangeLog).ToList();
        var latestChangeLog = changeLogs.OrderByDescending(u => u.CreatedDate).FirstOrDefault();

        return new ProjectTaskTreeViewModel
        {
            ProjectTaskId = task.ProjectTaskId,
            ProjectId = task.ProjectId,
            ParentTaskId = task.ParentTaskId,
            ParentTaskTitle = task.ParentTask?.Title,
            Title = task.Title,
            Description = task.Description,
            Status = task.Status,
            Priority = task.Priority,
            ProgressMode = task.ProgressMode,
            Progress = task.Progress,
            EstimatedHours = task.EstimatedHours,
            ActualHours = task.ActualHours,
            StartDate = task.StartDate,
            DueDate = task.DueDate,
            CompletedDate = task.CompletedDate,
            CreatedDate = task.CreatedDate,
            ModifiedDate = task.ModifiedDate,
            CreatedByName = task.CreatedByEmployee?.FullName,
            AssigneeNames = task.Assignments?.Select(a => a.Employee?.FullName ?? string.Empty)
                .Where(n => !string.IsNullOrEmpty(n))
                .ToList() ?? new List<string>(),
            AssigneeIds = task.Assignments?.Select(a => a.EmployeeId).ToList() ?? new List<int>(),
            AssigneeStatuses = task.Assignments?
                .Select(a => new ProjectTaskAssigneeStatusViewModel
                {
                    EmployeeId = a.EmployeeId,
                    EmployeeName = a.Employee?.FullName ?? string.Empty,
                    IsCompleted = a.IsCompleted,
                    CompletedDate = a.CompletedDate
                })
                .Where(a => !string.IsNullOrWhiteSpace(a.EmployeeName))
                .OrderBy(a => a.EmployeeName)
                .ToList() ?? new List<ProjectTaskAssigneeStatusViewModel>(),
            AssigneeTotalCount = task.Assignments?.Count ?? 0,
            AssigneeCompletedCount = task.Assignments?.Count(a => a.IsCompleted) ?? 0,
            IsCurrentAssigneeCompleted = currentEmployeeId.HasValue
                && (task.Assignments?.Any(a => a.EmployeeId == currentEmployeeId.Value && a.IsCompleted) ?? false),
            ChecklistTotal = task.ChecklistItems?.Count ?? 0,
            ChecklistDone = task.ChecklistItems?.Count(c => c.IsDone) ?? 0,
            ChecklistItems = task.ChecklistItems?.OrderBy(c => c.SortOrder).Select(c => new ChecklistItemViewModel
            {
                ProjectTaskChecklistItemId = c.ProjectTaskChecklistItemId,
                ProjectTaskId = c.ProjectTaskId,
                Title = c.Title,
                IsDone = c.IsDone,
                SortOrder = c.SortOrder,
                CompletedDate = c.CompletedDate,
                CompletedByName = c.CompletedByEmployee?.FullName
            }).ToList() ?? new List<ChecklistItemViewModel>(),
            Updates = workUpdates,
            ChangeLogs = changeLogs,
            ChangeLogCount = changeLogs.Count,
            LatestChangeLogDate = latestChangeLog?.CreatedDate,
            LatestChangeLogSummary = latestChangeLog == null ? null : BuildChangeLogSummary(latestChangeLog.Content),
            LatestChangeLogKind = latestChangeLog?.ChangeKind,
            Depth = depth,
            SubTasks = OrderTasks(task.SubTasks).Select(s => MapToTreeViewModel(s, depth + 1, currentEmployeeId)).ToList()
                ?? new List<ProjectTaskTreeViewModel>()
        };
    }

    private static ProjectTaskUpdateViewModel MapProjectTaskUpdate(ProjectTaskUpdate update)
    {
        var isChangeLog = IsTaskChangeLogContent(update.Content);
        var normalizedContent = NormalizeTaskUpdateContent(update.Content);
        var changeKind = isChangeLog ? DetectTaskChangeKind(normalizedContent) : null;

        return new ProjectTaskUpdateViewModel
        {
            ProjectTaskUpdateId = update.ProjectTaskUpdateId,
            ProjectTaskId = update.ProjectTaskId,
            SenderEmployeeId = update.SenderEmployeeId,
            SenderName = update.SenderEmployee?.FullName ?? string.Empty,
            Content = normalizedContent,
            StatusSnapshot = update.StatusSnapshot,
            ProgressSnapshot = update.ProgressSnapshot,
            CreatedDate = update.CreatedDate,
            IsChangeLog = isChangeLog,
            ChangeKind = changeKind,
            Attachments = update.Attachments.Select(a => new ProjectTaskAttachmentViewModel
            {
                ProjectTaskAttachmentId = a.ProjectTaskAttachmentId,
                FileName = a.FileName,
                FileSize = a.FileSize,
                ContentType = a.ContentType
            }).ToList()
        };
    }

    private static bool IsTaskChangeLogContent(string content)
        => content.StartsWith(TaskChangeLogPrefix, StringComparison.Ordinal);

    private static string NormalizeTaskUpdateContent(string content)
        => IsTaskChangeLogContent(content)
            ? content[TaskChangeLogPrefix.Length..].TrimStart()
            : content;

    private static string DetectTaskChangeKind(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return TaskChangeKindOther;

        var normalized = content.Trim().ToLowerInvariant();

        if (normalized.Contains("reject", StringComparison.Ordinal)
            || normalized.Contains("bị reject", StringComparison.Ordinal))
            return TaskChangeKindRejected;

        if (normalized.Contains("hoàn tác", StringComparison.Ordinal)
            || normalized.Contains("hoan tac", StringComparison.Ordinal))
            return TaskChangeKindUndo;

        return TaskChangeKindOther;
    }

    private static string BuildChangeLogSummary(string content)
    {
        var summary = content
            .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault()?.Trim() ?? "Có thay đổi task.";

        return summary.Length <= 120 ? summary : $"{summary[..117]}...";
    }

    private static List<ProjectTaskTreeViewModel> BuildTaskTree(IReadOnlyCollection<ProjectTask> tasks, int? parentTaskId,
        int depth, int employeeId, ProjectMemberRole? role, bool isSystemAdmin, bool isArchived,
        bool hasManagedAncestor)
        => OrderTasks(tasks.Where(task => task.ParentTaskId == parentTaskId))
            .Select(task =>
            {
                var isDirectAssignee = task.Assignments?.Any(a => a.EmployeeId == employeeId) == true;
                var isManagedBranch = hasManagedAncestor || isDirectAssignee;
                var vm = MapTaskNode(task, depth, employeeId);

                vm.CanManageTask = !isArchived
                    && (isSystemAdmin
                        || role == ProjectMemberRole.ProjectOwner
                        || (role == ProjectMemberRole.ProjectManager && isManagedBranch));
                vm.CanManageMembers = vm.CanManageTask;
                vm.CanCompleteTask = !isArchived
                    && (isSystemAdmin
                        || role == ProjectMemberRole.ProjectOwner
                        || (role != ProjectMemberRole.ProjectViewer
                            && (task.Assignments?.Any(a => a.EmployeeId == employeeId && !a.IsCompleted) == true)));
                vm.CanUndoDoneTask = !isArchived
                    && task.Status == ProjectTaskStatus.Done
                    && (isSystemAdmin
                        || role == ProjectMemberRole.ProjectOwner
                        || (role == ProjectMemberRole.ProjectManager && isManagedBranch)
                        || (task.Assignments?.Any(a => a.EmployeeId == employeeId && a.IsCompleted) == true));
                vm.CanRejectDoneTask = !isArchived
                    && task.Status == ProjectTaskStatus.Done
                    && (isSystemAdmin
                        || role == ProjectMemberRole.ProjectOwner
                        || (role == ProjectMemberRole.ProjectManager && isManagedBranch));
                vm.CanPostUpdate = !isArchived
                    && task.Status != ProjectTaskStatus.Done
                    && (isSystemAdmin
                        || role == ProjectMemberRole.ProjectOwner
                        || (role == ProjectMemberRole.ProjectManager && isManagedBranch)
                        || (role == ProjectMemberRole.ProjectStaff && isDirectAssignee));

                if (vm.IsOverdue)
                {
                    vm.CanManageTask = !isArchived
                        && (isSystemAdmin
                            || role == ProjectMemberRole.ProjectOwner
                            || (role == ProjectMemberRole.ProjectManager && isManagedBranch));
                    vm.CanManageMembers = false;
                    vm.CanCompleteTask = false;
                    vm.CanUndoDoneTask = false;
                    vm.CanRejectDoneTask = false;
                    vm.CanPostUpdate = false;
                }

                vm.SubTasks = BuildTaskTree(tasks, task.ProjectTaskId, depth + 1, employeeId, role,
                    isSystemAdmin, isArchived, isManagedBranch);
                return vm;
            })
            .ToList();

    private static ProjectTaskTreeViewModel MapTaskNode(ProjectTask task, int depth, int employeeId)
    {
        var vm = MapToTreeViewModel(task, depth, employeeId);
        vm.SubTasks = new List<ProjectTaskTreeViewModel>();
        vm.Updates = new List<ProjectTaskUpdateViewModel>();
        vm.ChangeLogs = new List<ProjectTaskUpdateViewModel>();
        return vm;
    }

    private static IEnumerable<ProjectTask> OrderTasks(IEnumerable<ProjectTask>? tasks)
        => (tasks ?? Enumerable.Empty<ProjectTask>())
            .OrderBy(task => task.StartDate ?? task.DueDate ?? DateTime.MaxValue)
            .ThenBy(task => task.DueDate ?? DateTime.MaxValue)
            .ThenByDescending(task => task.Priority)
            .ThenBy(task => task.CreatedDate);

    private sealed class TaskScheduleNode
    {
        public int ProjectTaskId { get; set; }
        public int? ParentTaskId { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime? StartDate { get; set; }
        public DateTime? DueDate { get; set; }
    }

    private static string TaskStatusLabel(ProjectTaskStatus status) => status switch
    {
        ProjectTaskStatus.Todo => "Chưa bắt đầu",
        ProjectTaskStatus.InProgress => "Đang làm",
        ProjectTaskStatus.Review => "Review",
        ProjectTaskStatus.Done => "Hoàn thành",
        ProjectTaskStatus.Cancelled => "Đã huỷ",
        _ => status.ToString()
    };

    private static bool IsMissingWorklogSchema(SqlException ex)
        => ex.Message.Contains("ProjectTaskUpdates", StringComparison.OrdinalIgnoreCase)
            || ex.Message.Contains("ProjectTaskAttachments", StringComparison.OrdinalIgnoreCase);
}
