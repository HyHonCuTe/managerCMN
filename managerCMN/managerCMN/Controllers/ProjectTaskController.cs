using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
        if (employeeId == 0) return Forbid();

        try
        {
            var task = await _taskService.GetTaskDetailsAsync(id, employeeId);
            if (task == null) return NotFound();

            var project = await _projectService.GetDetailsAsync(task.ProjectId, employeeId);
            if (project == null) return NotFound();

            task.CanManageTask = project.MyRole != ProjectMemberRole.ProjectViewer && !project.IsArchived;
            task.CanCompleteTask = !project.IsArchived
                && (project.MyRole == ProjectMemberRole.ProjectOwner || task.AssigneeIds.Contains(employeeId));
            task.CanManageMembers = (project.MyRole == ProjectMemberRole.ProjectOwner
                || project.MyRole == ProjectMemberRole.ProjectManager) && !project.IsArchived;
            task.IsArchived = project.IsArchived;
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
            TempData["Error"] = "Dữ liệu không hợp lệ.";
            return RedirectToAction("Details", "Project", new { id = vm.ProjectId });
        }

        try
        {
            var taskId = await _taskService.CreateTaskAsync(vm, employeeId);
            TempData["Success"] = "Tạo công việc thành công.";
            return RedirectToAction("Details", "Project", new { id = vm.ProjectId, openTaskId = taskId });
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction("Details", "Project", new { id = vm.ProjectId });
    }

    public async Task<IActionResult> Edit(int id)
    {
        var employeeId = GetCurrentEmployeeId();
        if (employeeId == 0) return Forbid();

        try
        {
            var task = await _taskService.GetTaskDetailsAsync(id, employeeId);
            if (task == null) return NotFound();

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

            var members = await _projectService.GetMembersAsync(task.ProjectId, employeeId);
            ViewBag.Members = members;
            var project = await _projectService.GetDetailsAsync(task.ProjectId, employeeId);
            ViewBag.CanCompleteTask = project != null
                && !project.IsArchived
                && (project.MyRole == ProjectMemberRole.ProjectOwner || task.AssigneeIds.Contains(employeeId));
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
        if (employeeId == 0) return Forbid();

        if (!ModelState.IsValid)
        {
            var members = await _projectService.GetMembersAsync(vm.ProjectId, employeeId);
            ViewBag.Members = members;
            var project = await _projectService.GetDetailsAsync(vm.ProjectId, employeeId);
            ViewBag.CanCompleteTask = project != null
                && !project.IsArchived
                && (project.MyRole == ProjectMemberRole.ProjectOwner || vm.AssigneeIds.Contains(employeeId));
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
            var members = await _projectService.GetMembersAsync(vm.ProjectId, employeeId);
            ViewBag.Members = members;
            try
            {
                var task = await _taskService.GetTaskDetailsAsync(vm.ProjectTaskId, employeeId);
                var project = task == null ? null : await _projectService.GetDetailsAsync(task.ProjectId, employeeId);
                ViewBag.CanCompleteTask = task != null
                    && project != null
                    && !project.IsArchived
                    && (project.MyRole == ProjectMemberRole.ProjectOwner || task.AssigneeIds.Contains(employeeId));
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
        if (employeeId == 0) return Forbid();

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
        if (employeeId == 0) return Json(new { success = false, message = "Không xác thực được người dùng." });

        try
        {
            await _taskService.UpdateStatusAsync(vm, employeeId);
            return Json(new { success = true });
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
        if (employeeId == 0) return Json(new { success = false, message = "Không xác thực được người dùng." });

        try
        {
            await _taskService.UpdateProgressAsync(vm, employeeId);
            return Json(new { success = true });
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
        if (employeeId == 0) return Forbid();

        try
        {
            await _taskService.AssignMembersAsync(taskId, employeeIds, employeeId);
            TempData["Success"] = "Cập nhật phân công thành công.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction("Details", "Project", new { id = projectId, openTaskId = taskId });
    }

    [HttpPost]
    public async Task<IActionResult> PostUpdate(PostTaskUpdateViewModel vm)
    {
        var employeeId = GetCurrentEmployeeId();
        if (employeeId == 0) return Forbid();

        try
        {
            await _taskService.AddTaskUpdateAsync(vm, employeeId);
            TempData["Success"] = "Đã gửi cập nhật công việc.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction("Details", "Project", new { id = vm.ProjectId, openTaskId = vm.ProjectTaskId });
    }

    [HttpPost]
    public async Task<IActionResult> AddChecklistItem(AddChecklistItemViewModel vm)
    {
        var employeeId = GetCurrentEmployeeId();
        if (employeeId == 0) return Json(new { success = false });

        try
        {
            await _taskService.AddChecklistItemAsync(vm, employeeId);
            return Json(new { success = true });
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
        if (employeeId == 0) return Json(new { success = false });

        try
        {
            await _taskService.ToggleChecklistItemAsync(id, employeeId);
            return Json(new { success = true });
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
        if (employeeId == 0) return Json(new { success = false });

        try
        {
            await _taskService.DeleteChecklistItemAsync(id, employeeId);
            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    public async Task<IActionResult> DownloadAttachment(int attachmentId)
    {
        var employeeId = GetCurrentEmployeeId();
        if (employeeId == 0) return Forbid();

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

    private static string RoleLabel(ProjectMemberRole role) => role switch
    {
        ProjectMemberRole.ProjectOwner => "Owner",
        ProjectMemberRole.ProjectManager => "Manager",
        ProjectMemberRole.ProjectStaff => "Staff",
        ProjectMemberRole.ProjectViewer => "Viewer",
        _ => role.ToString()
    };
}
