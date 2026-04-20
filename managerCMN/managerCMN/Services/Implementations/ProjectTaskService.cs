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
        var tasks = await _context.ProjectTasks
            .AsNoTracking()
            .Where(t => t.ProjectId == projectId)
            .Include(t => t.CreatedByEmployee)
            .Include(t => t.Assignments)
                .ThenInclude(a => a.Employee)
            .Include(t => t.ChecklistItems)
            .ToListAsync();

        return BuildTaskTree(tasks, null, 0);
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

        var role = await _accessService.GetRoleAsync(task.ProjectId, employeeId);
        if (role != ProjectMemberRole.ProjectOwner && role != ProjectMemberRole.ProjectManager)
        {
            var isAssigned = await _context.ProjectTaskAssignments
                .AsNoTracking()
                .AnyAsync(a => a.ProjectTaskId == task.ProjectTaskId && a.EmployeeId == employeeId);

            if (!isAssigned)
                throw new UnauthorizedAccessException("Bạn không có quyền xem task này.");
        }

        var vm = MapToTreeViewModel(task, 0, employeeId);
        if (role != ProjectMemberRole.ProjectOwner && role != ProjectMemberRole.ProjectManager)
        {
            vm.SubTasks = FilterVisibleSubTasks(vm.SubTasks, employeeId);
            NormalizeTaskDepths(vm.SubTasks, 1);
        }

        vm.WorklogAvailable = worklogAvailable;
        return vm;
    }

    public async Task<int> CreateTaskAsync(ProjectTaskCreateViewModel vm, int creatorEmployeeId)
    {
        await _accessService.EnsureCanManageMembersAsync(vm.ProjectId, creatorEmployeeId);
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

        await SyncAssignmentsAsync(task, vm.AssigneeIds, creatorEmployeeId);
        await CreateSystemUpdateAsync(task.ProjectTaskId, creatorEmployeeId,
            task.ParentTaskId.HasValue ? "Đã tạo subtask." : "Đã tạo task.");

        return task.ProjectTaskId;
    }

    public async Task UpdateTaskAsync(ProjectTaskEditViewModel vm, int employeeId)
    {
        var task = await _unitOfWork.ProjectTasks.GetByIdAsync(vm.ProjectTaskId)
            ?? throw new InvalidOperationException("Task không tồn tại.");

        await _accessService.EnsureCanManageTaskAsync(task.ProjectId, employeeId);
        await ValidateTaskScheduleAsync(task.ProjectId, task.ParentTaskId, vm.StartDate, vm.DueDate, task.ProjectTaskId);

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
    }

    public async Task DeleteTaskAsync(int taskId, int employeeId)
    {
        var task = await _unitOfWork.ProjectTasks.GetByIdAsync(taskId)
            ?? throw new InvalidOperationException("Task không tồn tại.");

        await _accessService.EnsureCanManageTaskAsync(task.ProjectId, employeeId);
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

        await _accessService.EnsureIsMemberAsync(task.ProjectId, employeeId);
        await EnsureCanViewTaskAsync(task, employeeId);

        if (task.Status == ProjectTaskStatus.Done)
        {
            if (vm.Status != ProjectTaskStatus.Done)
                throw new InvalidOperationException("Task đã hoàn thành nên không thể hoàn tác trạng thái.");

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
    }

    public async Task UpdateProgressAsync(UpdateTaskProgressViewModel vm, int employeeId)
    {
        var task = await _unitOfWork.ProjectTasks.GetByIdAsync(vm.ProjectTaskId)
            ?? throw new InvalidOperationException("Task không tồn tại.");

        await _accessService.EnsureIsMemberAsync(task.ProjectId, employeeId);
        await EnsureCanViewTaskAsync(task, employeeId);

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

        await _accessService.EnsureCanManageMembersAsync(task.ProjectId, actorEmployeeId);

        await SyncAssignmentsAsync(task, employeeIds, actorEmployeeId);
        await SyncTaskProgressFromAssignmentsOrEngineAsync(task.ProjectTaskId);
        await _progressService.BubbleUpParentProgressAsync(task.ProjectTaskId);
        await _progressService.RecalculateProjectProgressAsync(task.ProjectId);

        var assigneeNames = await GetEmployeeNamesAsync(employeeIds);
        var updateContent = assigneeNames.Count > 0
            ? $"Đã phân công thực hiện cho: {string.Join(", ", assigneeNames)}."
            : "Đã xoá toàn bộ người thực hiện khỏi task.";
        await CreateSystemUpdateAsync(task.ProjectTaskId, actorEmployeeId, updateContent);
    }

    public async Task<ChecklistItemViewModel> AddChecklistItemAsync(AddChecklistItemViewModel vm, int employeeId)
    {
        var task = await _unitOfWork.ProjectTasks.GetByIdAsync(vm.ProjectTaskId)
            ?? throw new InvalidOperationException("Task không tồn tại.");

        await _accessService.EnsureCanManageTaskAsync(task.ProjectId, employeeId);
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

        await _accessService.EnsureCanManageTaskAsync(item.ProjectTask.ProjectId, employeeId);
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

        await _accessService.EnsureIsMemberAsync(task.ProjectId, employeeId);
        await EnsureCanViewTaskAsync(task, employeeId);

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
        var targetUserIds = await _context.ProjectTaskAssignments
            .AsNoTracking()
            .Where(a => a.ProjectTaskId == task.ProjectTaskId && a.EmployeeId != senderEmployeeId)
            .Join(_context.Users.AsNoTracking(), a => a.EmployeeId, u => u.EmployeeId, (a, u) => u.UserId)
            .Distinct()
            .ToListAsync();

        if (targetUserIds.Count == 0)
            return;

        var senderName = await _context.Employees
            .AsNoTracking()
            .Where(e => e.EmployeeId == senderEmployeeId)
            .Select(e => e.FullName)
            .FirstOrDefaultAsync() ?? "Một thành viên";

        var shortContent = string.IsNullOrWhiteSpace(updateContent)
            ? "Có cập nhật nhật ký công việc mới."
            : updateContent.Length > 140
                ? $"{updateContent[..140]}..."
                : updateContent;

        var title = $"Task cập nhật: {task.Title}";
        var message = $"{senderName} vừa cập nhật nhật ký công việc. {shortContent}";
        var targetUrl = $"/Project/Details/{task.ProjectId}?openTaskId={task.ProjectTaskId}";

        foreach (var userId in targetUserIds)
            await _notificationService.CreateAsync(userId, title, message, targetUrl);
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
        var role = await _accessService.GetRoleAsync(task.ProjectId, employeeId);
        if (role == ProjectMemberRole.ProjectOwner || role == ProjectMemberRole.ProjectManager)
            return;

        var isAssigned = await _context.ProjectTaskAssignments
            .AsNoTracking()
            .AnyAsync(a => a.ProjectTaskId == task.ProjectTaskId && a.EmployeeId == employeeId);

        if (!isAssigned)
            throw new UnauthorizedAccessException("Bạn không có quyền xem task này.");
    }

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
        var role = await _accessService.GetRoleAsync(task.ProjectId, employeeId);

        var hasAssignments = await _context.ProjectTaskAssignments
            .AsNoTracking()
            .AnyAsync(a => a.ProjectTaskId == task.ProjectTaskId);

        var isAssigned = await _context.ProjectTaskAssignments
            .AsNoTracking()
            .AnyAsync(a => a.ProjectTaskId == task.ProjectTaskId && a.EmployeeId == employeeId);

        if (isAssigned)
            return;

        if (!hasAssignments && (role == ProjectMemberRole.ProjectOwner || role == ProjectMemberRole.ProjectManager))
            return;

        throw new UnauthorizedAccessException("Chỉ người được giao task này mới được xác nhận hoàn thành.");
    }

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

    private async Task SyncAssignmentsAsync(ProjectTask task, IEnumerable<int>? employeeIds, int actorEmployeeId)
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
        }

        await _context.SaveChangesAsync();
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
            Updates = task.Updates?.OrderByDescending(u => u.CreatedDate).Select(u => new ProjectTaskUpdateViewModel
            {
                ProjectTaskUpdateId = u.ProjectTaskUpdateId,
                ProjectTaskId = u.ProjectTaskId,
                SenderName = u.SenderEmployee?.FullName ?? string.Empty,
                Content = u.Content,
                StatusSnapshot = u.StatusSnapshot,
                ProgressSnapshot = u.ProgressSnapshot,
                CreatedDate = u.CreatedDate,
                Attachments = u.Attachments.Select(a => new ProjectTaskAttachmentViewModel
                {
                    ProjectTaskAttachmentId = a.ProjectTaskAttachmentId,
                    FileName = a.FileName,
                    FileSize = a.FileSize,
                    ContentType = a.ContentType
                }).ToList()
            }).ToList() ?? new List<ProjectTaskUpdateViewModel>(),
            Depth = depth,
            SubTasks = OrderTasks(task.SubTasks).Select(s => MapToTreeViewModel(s, depth + 1, currentEmployeeId)).ToList()
                ?? new List<ProjectTaskTreeViewModel>()
        };
    }

    private static List<ProjectTaskTreeViewModel> BuildTaskTree(IReadOnlyCollection<ProjectTask> tasks, int? parentTaskId, int depth)
        => OrderTasks(tasks.Where(task => task.ParentTaskId == parentTaskId))
            .Select(task =>
            {
                var vm = MapTaskNode(task, depth);
                vm.SubTasks = BuildTaskTree(tasks, task.ProjectTaskId, depth + 1);
                return vm;
            })
            .ToList();

    private static ProjectTaskTreeViewModel MapTaskNode(ProjectTask task, int depth)
    {
        var vm = MapToTreeViewModel(task, depth);
        vm.SubTasks = new List<ProjectTaskTreeViewModel>();
        vm.Updates = new List<ProjectTaskUpdateViewModel>();
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
