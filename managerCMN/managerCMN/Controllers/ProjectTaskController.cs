using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using managerCMN.Helpers;
using managerCMN.Models.Enums;
using managerCMN.Models.ViewModels;
using managerCMN.Services.Interfaces;

namespace managerCMN.Controllers;

[Authorize]
public class ProjectTaskController : Controller
{
    private readonly IProjectTaskService _taskService;
    private readonly IProjectService _projectService;
    private readonly IEmployeeService _employeeService;
    private readonly IWebHostEnvironment _env;

    public ProjectTaskController(IProjectTaskService taskService, IProjectService projectService,
        IEmployeeService employeeService, IWebHostEnvironment env)
    {
        _taskService = taskService;
        _projectService = projectService;
        _employeeService = employeeService;
        _env = env;
    }

    public async Task<IActionResult> Details(int id)
    {
        var employeeId = GetCurrentEmployeeId();
        var isSysAdmin = User.IsInRole("Admin");
        if (employeeId == 0 && !isSysAdmin) return Forbid();

        try
        {
            var task = await _taskService.GetTaskDetailsAsync(id, employeeId);
            if (task == null) return NotFound();

            var project = await _projectService.GetDetailsAsync(task.ProjectId, employeeId, ignoreAccessCheck: isSysAdmin);
            if (project == null) return NotFound();

            task.CanManageProjectMembers = !project.IsArchived
                && (isSysAdmin || project.MyRole == ProjectMemberRole.ProjectOwner);
            task.AvailableMembers = project.Members
                .Where(m => m.Role != ProjectMemberRole.ProjectViewer)
                .Select(m => new ProjectTaskMemberOptionViewModel
                {
                    EmployeeId = m.EmployeeId,
                    EmployeeName = m.EmployeeName,
                    EmployeeCode = m.EmployeeCode,
                    RoleLabel = RoleLabel(m.Role)
                })
                .ToList();

            return PartialView("_TaskDetail", task);
        }
        catch (UnauthorizedAccessException)
        {
            return NotFound();
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create(ProjectTaskCreateViewModel vm)
    {
        var employeeId = GetCurrentEmployeeId();
        if (employeeId == 0) return Forbid();

        if (!ModelState.IsValid)
        {
            if (IsAjaxRequest())
                return Json(new { success = false, message = "Dữ liệu không hợp lệ." });

            TempData["Error"] = "Dữ liệu không hợp lệ.";
            return RedirectToAction("Details", "Project", new { id = vm.ProjectId });
        }

        try
        {
            var taskId = await _taskService.CreateTaskAsync(vm, employeeId);
            if (IsAjaxRequest())
            {
                var task = await _taskService.GetTaskDetailsAsync(taskId, employeeId);
                return Json(new
                {
                    success = true,
                    message = "Tạo subtask thành công.",
                    task = task == null ? null : new
                    {
                        task.ProjectTaskId,
                        task.Title,
                        status = (int)task.Status,
                        statusLabel = TaskStatusLabel(task.Status),
                        statusCss = TaskStatusCss(task.Status),
                        progress = task.Progress,
                        dueDate = task.DueDate?.ToString("dd/MM")
                    }
                });
            }

            TempData["Success"] = "Tạo công việc thành công.";
            return RedirectToAction("Details", "Project", new { id = vm.ProjectId, openTaskId = taskId });
        }
        catch (Exception ex)
        {
            if (IsAjaxRequest())
                return Json(new { success = false, message = ex.Message });

            TempData["Error"] = ex.Message;
        }
        return RedirectToAction("Details", "Project", new { id = vm.ProjectId });
    }

    public async Task<IActionResult> Edit(int id)
    {
        var employeeId = GetCurrentEmployeeId();
        var isSysAdmin = User.IsInRole("Admin");
        if (employeeId == 0 && !isSysAdmin) return Forbid();

        try
        {
            var task = await _taskService.GetTaskDetailsAsync(id, employeeId);
            if (task == null) return NotFound();

            var project = await _projectService.GetDetailsAsync(task.ProjectId, employeeId, ignoreAccessCheck: isSysAdmin);
            if (project == null) return NotFound();
            if (!task.CanManageTask)
                return Forbid();

            var vm = new ProjectTaskEditViewModel
            {
                ProjectTaskId = task.ProjectTaskId,
                ProjectId = task.ProjectId,
                ParentTaskId = task.ParentTaskId,
                Title = task.Title,
                Description = task.Description,
                Priority = task.Priority,
                Status = task.Status,
                StartDate = task.StartDate,
                DueDate = task.DueDate,
                EstimatedHours = task.EstimatedHours,
                ActualHours = task.ActualHours,
                ProgressMode = task.ProgressMode,
                Progress = task.Progress,
                AssigneeIds = task.AssigneeIds
            };

            var members = await _projectService.GetMembersAsync(task.ProjectId, employeeId, isSysAdmin);
            ViewBag.Members = members;
            ViewBag.CanCompleteTask = task.CanCompleteTask;
            ViewBag.IsTaskDone = task.Status == ProjectTaskStatus.Done;
            return View(vm);
        }
        catch (UnauthorizedAccessException)
        {
            return NotFound();
        }
    }

    [HttpPost]
    public async Task<IActionResult> Edit(ProjectTaskEditViewModel vm)
    {
        var employeeId = GetCurrentEmployeeId();
        var isSysAdmin = User.IsInRole("Admin");
        if (employeeId == 0 && !isSysAdmin) return Forbid();

        if (!ModelState.IsValid)
        {
            var members = await _projectService.GetMembersAsync(vm.ProjectId, employeeId, isSysAdmin);
            ViewBag.Members = members;
            var task = await _taskService.GetTaskDetailsAsync(vm.ProjectTaskId, employeeId);
            ViewBag.CanCompleteTask = task?.CanCompleteTask ?? false;
            ViewBag.IsTaskDone = vm.Status == ProjectTaskStatus.Done;
            return View(vm);
        }

        try
        {
            await _taskService.UpdateTaskAsync(vm, employeeId);
            TempData["Success"] = "Cập nhật công việc thành công.";
            return RedirectToAction("Details", "Project", new { id = vm.ProjectId, openTaskId = vm.ProjectTaskId });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", ex.Message);
            var members = await _projectService.GetMembersAsync(vm.ProjectId, employeeId, isSysAdmin);
            ViewBag.Members = members;
            try
            {
                var task = await _taskService.GetTaskDetailsAsync(vm.ProjectTaskId, employeeId);
                ViewBag.CanCompleteTask = task?.CanCompleteTask ?? false;
                ViewBag.IsTaskDone = task?.Status == ProjectTaskStatus.Done;
            }
            catch
            {
                ViewBag.CanCompleteTask = false;
                ViewBag.IsTaskDone = vm.Status == ProjectTaskStatus.Done;
            }
            return View(vm);
        }
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id, int projectId)
    {
        var employeeId = GetCurrentEmployeeId();
        var isSysAdmin = User.IsInRole("Admin");
        if (employeeId == 0 && !isSysAdmin) return Forbid();

        try
        {
            await _taskService.DeleteTaskAsync(id, employeeId);
            TempData["Success"] = "Đã xoá công việc.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction("Details", "Project", new { id = projectId });
    }

    [HttpPost]
    public async Task<IActionResult> UpdateStatus(UpdateTaskStatusViewModel vm)
    {
        var employeeId = GetCurrentEmployeeId();
        var isSysAdmin = User.IsInRole("Admin");
        if (employeeId == 0 && !isSysAdmin) return Json(new { success = false, message = "Không xác thực được người dùng." });

        try
        {
            await _taskService.UpdateStatusAsync(vm, employeeId);
            var task = await _taskService.GetTaskDetailsAsync(vm.ProjectTaskId, employeeId);
            return Json(new
            {
                success = true,
                message = "Đã cập nhật trạng thái.",
                task = ToTaskPanelState(task),
                update = ToTaskUpdateDto(task?.Updates.OrderByDescending(u => u.CreatedDate).FirstOrDefault())
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> UpdateProgress(UpdateTaskProgressViewModel vm)
    {
        var employeeId = GetCurrentEmployeeId();
        var isSysAdmin = User.IsInRole("Admin");
        if (employeeId == 0 && !isSysAdmin) return Json(new { success = false, message = "Không xác thực được người dùng." });

        try
        {
            await _taskService.UpdateProgressAsync(vm, employeeId);
            var task = await _taskService.GetTaskDetailsAsync(vm.ProjectTaskId, employeeId);
            return Json(new
            {
                success = true,
                message = "Đã cập nhật tiến độ.",
                task = ToTaskPanelState(task),
                update = ToTaskUpdateDto(task?.Updates.OrderByDescending(u => u.CreatedDate).FirstOrDefault())
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> AssignMembers(int taskId, int projectId, List<int> employeeIds)
    {
        var employeeId = GetCurrentEmployeeId();
        var isSysAdmin = User.IsInRole("Admin");
        if (employeeId == 0 && !isSysAdmin) return Forbid();

        try
        {
            await _taskService.AssignMembersAsync(taskId, employeeIds, employeeId);
            if (IsAjaxRequest())
            {
                var task = await _taskService.GetTaskDetailsAsync(taskId, employeeId);
                return Json(new
                {
                    success = true,
                    message = "Đã lưu phân công.",
                    assignees = task?.AssigneeNames ?? new List<string>(),
                    updateCount = task?.Updates.Count ?? 0,
                    update = ToTaskUpdateDto(task?.Updates.OrderByDescending(u => u.CreatedDate).FirstOrDefault()),
                    canCompleteTask = task?.CanCompleteTask ?? false
                });
            }

            TempData["Success"] = "Cập nhật phân công thành công.";
        }
        catch (Exception ex)
        {
            if (IsAjaxRequest())
                return Json(new { success = false, message = ex.Message });

            TempData["Error"] = ex.Message;
        }
        return RedirectToAction("Details", "Project", new { id = projectId, openTaskId = taskId });
    }

    [HttpPost]
    public async Task<IActionResult> PostUpdate(PostTaskUpdateViewModel vm)
    {
        var employeeId = GetCurrentEmployeeId();
        var isSysAdmin = User.IsInRole("Admin");
        if (employeeId == 0 && !isSysAdmin) return Forbid();

        try
        {
            await _taskService.AddTaskUpdateAsync(vm, employeeId);
            if (IsAjaxRequest())
            {
                var task = await _taskService.GetTaskDetailsAsync(vm.ProjectTaskId, employeeId);
                return Json(new
                {
                    success = true,
                    message = "Đã gửi cập nhật công việc.",
                    updateCount = task?.Updates.Count ?? 0,
                    update = ToTaskUpdateDto(task?.Updates.OrderByDescending(u => u.CreatedDate).FirstOrDefault())
                });
            }

            TempData["Success"] = "Đã gửi cập nhật công việc.";
        }
        catch (Exception ex)
        {
            if (IsAjaxRequest())
                return Json(new { success = false, message = ex.Message });

            TempData["Error"] = ex.Message;
        }

        return RedirectToAction("Details", "Project", new { id = vm.ProjectId, openTaskId = vm.ProjectTaskId });
    }

    [HttpPost]
    public async Task<IActionResult> AddChecklistItem(AddChecklistItemViewModel vm)
    {
        var employeeId = GetCurrentEmployeeId();
        var isSysAdmin = User.IsInRole("Admin");
        if (employeeId == 0 && !isSysAdmin) return Json(new { success = false });

        try
        {
            var item = await _taskService.AddChecklistItemAsync(vm, employeeId);
            var task = await _taskService.GetTaskDetailsAsync(vm.ProjectTaskId, employeeId);
            return Json(new
            {
                success = true,
                message = "Đã thêm checklist.",
                item,
                task = ToTaskPanelState(task)
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> ToggleChecklistItem(int id)
    {
        var employeeId = GetCurrentEmployeeId();
        var isSysAdmin = User.IsInRole("Admin");
        if (employeeId == 0 && !isSysAdmin) return Json(new { success = false });

        try
        {
            var taskId = await _taskService.ToggleChecklistItemAsync(id, employeeId);
            var task = await _taskService.GetTaskDetailsAsync(taskId, employeeId);
            return Json(new
            {
                success = true,
                message = "Đã cập nhật checklist.",
                task = ToTaskPanelState(task)
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> DeleteChecklistItem(int id, int taskId)
    {
        var employeeId = GetCurrentEmployeeId();
        var isSysAdmin = User.IsInRole("Admin");
        if (employeeId == 0 && !isSysAdmin) return Json(new { success = false });

        try
        {
            var updatedTaskId = await _taskService.DeleteChecklistItemAsync(id, employeeId);
            var task = await _taskService.GetTaskDetailsAsync(updatedTaskId, employeeId);
            return Json(new
            {
                success = true,
                message = "Đã xoá checklist.",
                task = ToTaskPanelState(task)
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    public async Task<IActionResult> DownloadAttachment(int attachmentId)
    {
        var employeeId = GetCurrentEmployeeId();
        var isSysAdmin = User.IsInRole("Admin");
        if (employeeId == 0 && !isSysAdmin) return Forbid();

        try
        {
            var attachment = await _taskService.GetAttachmentAsync(attachmentId, employeeId);
            if (attachment == null) return NotFound();

            var webRootPath = Path.GetFullPath(_env.WebRootPath);
            var relativePath = attachment.FilePath
                .TrimStart('/', '\\')
                .Replace('/', Path.DirectorySeparatorChar);
            var filePath = Path.GetFullPath(Path.Combine(webRootPath, relativePath));

            if (!filePath.StartsWith(webRootPath, StringComparison.OrdinalIgnoreCase))
                return Forbid();
            if (!System.IO.File.Exists(filePath))
                return NotFound();

            var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
            return File(fileBytes, attachment.ContentType ?? "application/octet-stream", attachment.FileName);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    private int GetCurrentEmployeeId()
    {
        var claim = User.FindFirst("EmployeeId")?.Value;
        return int.TryParse(claim, out var id) ? id : 0;
    }

    private bool IsAjaxRequest()
        => string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);

    private object? ToTaskPanelState(ProjectTaskTreeViewModel? task, ProjectMemberRole? myRole = null,
        bool isArchived = false, int currentEmployeeId = 0, bool isSystemAdmin = false)
    {
        if (task == null)
            return null;

        var canCompleteTask = task.CanCompleteTask;

        return new
        {
            status = (int)task.Status,
            statusLabel = TaskStatusLabel(task.Status),
            statusCss = TaskStatusCss(task.Status),
            progress = task.Progress,
            progressText = $"{task.Progress:0}%",
            progressCss = ProgressColor(task.Progress),
            checklistDone = task.ChecklistDone,
            checklistTotal = task.ChecklistTotal,
            isDone = task.Status == ProjectTaskStatus.Done,
            canCompleteTask
        };
    }

    private static bool CanCurrentUserCompleteTask(ProjectTaskTreeViewModel task, ProjectMemberRole myRole,
        bool isArchived, int currentEmployeeId, bool isSystemAdmin = false)
    {
        if (isArchived)
            return false;

        if (isSystemAdmin)
            return true;

        var hasAssignees = task.AssigneeTotalCount > 0;
        var isAssignee = task.AssigneeIds.Contains(currentEmployeeId);

        if (hasAssignees)
            return isSystemAdmin || myRole == ProjectMemberRole.ProjectOwner || (isAssignee && !task.IsCurrentAssigneeCompleted);

        return isSystemAdmin || myRole == ProjectMemberRole.ProjectOwner;
    }

    private object? ToTaskUpdateDto(ProjectTaskUpdateViewModel? update)
    {
        if (update == null)
            return null;

        return new
        {
            update.ProjectTaskUpdateId,
            senderEmployeeId = update.SenderEmployeeId,
            senderName = string.IsNullOrWhiteSpace(update.SenderName) ? "Hệ thống" : update.SenderName,
            avatar = string.IsNullOrWhiteSpace(update.SenderName) ? "?" : update.SenderName[0].ToString().ToUpper(),
            content = update.Content,
            createdDate = update.CreatedDate.ToString("dd/MM/yyyy HH:mm"),
            statusLabel = update.StatusSnapshot.HasValue ? TaskStatusLabel(update.StatusSnapshot.Value) : null,
            statusCss = update.StatusSnapshot.HasValue ? TaskStatusCss(update.StatusSnapshot.Value) : null,
            progressText = update.ProgressSnapshot.HasValue ? $"{update.ProgressSnapshot.Value:0}%" : null,
            attachments = update.Attachments.Select(a => new
            {
                a.FileName,
                size = FileUploadHelper.FormatFileSize(a.FileSize),
                url = Url.Action(nameof(DownloadAttachment), new { attachmentId = a.ProjectTaskAttachmentId })
            }).ToList()
        };
    }

    private static string RoleLabel(ProjectMemberRole role) => role switch
    {
        ProjectMemberRole.ProjectOwner => "Owner",
        ProjectMemberRole.ProjectManager => "Manager",
        ProjectMemberRole.ProjectStaff => "Staff",
        ProjectMemberRole.ProjectViewer => "Viewer",
        _ => role.ToString()
    };

    private static string TaskStatusLabel(ProjectTaskStatus status) => status switch
    {
        ProjectTaskStatus.Todo => "Chưa bắt đầu",
        ProjectTaskStatus.InProgress => "Đang làm",
        ProjectTaskStatus.Review => "Review",
        ProjectTaskStatus.Done => "Hoàn thành",
        ProjectTaskStatus.Cancelled => "Đã huỷ",
        _ => status.ToString()
    };

    private static string TaskStatusCss(ProjectTaskStatus status) => status switch
    {
        ProjectTaskStatus.Todo => "task-status-todo",
        ProjectTaskStatus.InProgress => "task-status-inprogress",
        ProjectTaskStatus.Review => "task-status-review",
        ProjectTaskStatus.Done => "task-status-done",
        ProjectTaskStatus.Cancelled => "task-status-cancelled",
        _ => string.Empty
    };

    private static string ProgressColor(decimal progress)
        => progress >= 100 ? "bg-success" : progress >= 60 ? "bg-info" : progress >= 30 ? "bg-warning" : "bg-danger";
}
