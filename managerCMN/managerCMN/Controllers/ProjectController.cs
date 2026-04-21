using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using managerCMN.Models.Enums;
using managerCMN.Models.ViewModels;
using managerCMN.Services.Interfaces;

namespace managerCMN.Controllers;

[Authorize]
public class ProjectController : Controller
{
    private readonly IProjectService _projectService;
    private readonly IProjectTaskService _taskService;
    private readonly IEmployeeService _employeeService;

    public ProjectController(IProjectService projectService, IProjectTaskService taskService,
        IEmployeeService employeeService)
    {
        _projectService = projectService;
        _taskService = taskService;
        _employeeService = employeeService;
    }

    public async Task<IActionResult> Index()
    {
        var employeeId = GetCurrentEmployeeId();
        var isSysAdmin = User.IsInRole("Admin");
        if (employeeId == 0 && !isSysAdmin) return Forbid();

        var projects = await _projectService.GetMyProjectsAsync(employeeId, includeAll: isSysAdmin);
        return View(projects);
    }

    public async Task<IActionResult> Details(int id)
    {
        var employeeId = GetCurrentEmployeeId();
        bool isSysAdmin = User.IsInRole("Admin");
        if (employeeId == 0 && !isSysAdmin) return Forbid();

        try
        {
            var details = await _projectService.GetDetailsAsync(id, employeeId, ignoreAccessCheck: isSysAdmin);
            if (details == null) return NotFound();

            bool isFullView = isSysAdmin
                || details.MyRole == ProjectMemberRole.ProjectOwner
                || details.MyRole == ProjectMemberRole.ProjectManager;

            details.IsFullView = isFullView;
            details.CurrentEmployeeId = employeeId;

            var taskTree = await _taskService.GetTaskTreeAsync(id, employeeId, ignoreAccessCheck: isSysAdmin);

            if (isFullView)
            {
                details.RootTasks = isSysAdmin || details.MyRole == ProjectMemberRole.ProjectOwner
                    ? taskTree.ToList()
                    : FilterTasksForEmployee(taskTree, employeeId, details.MyRole);

                NormalizeTaskDepths(details.RootTasks, 0);
                PopulateTimeline(details);

                if (isSysAdmin || details.MyRole == ProjectMemberRole.ProjectOwner)
                {
                    ViewBag.AllMembers = await GetMemberSelectListAsync(id, employeeId, isSysAdmin);
                    ViewBag.AllEmployees = await GetNonMemberCandidatesAsync(id, employeeId, isSysAdmin);
                }
            }
            else
            {
                details.RootTasks = FilterTasksForEmployee(taskTree, employeeId, details.MyRole);
                NormalizeTaskDepths(details.RootTasks, 0);
            }

            return View(details);
        }
        catch (UnauthorizedAccessException)
        {
            return NotFound();
        }
    }

    public IActionResult Create()
    {
        return View(new ProjectCreateViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Create(ProjectCreateViewModel vm)
    {
        var employeeId = GetCurrentEmployeeId();
        if (employeeId == 0) return Forbid();

        if (!ModelState.IsValid) return View(vm);

        if (vm.StartDate.HasValue && vm.EndDate.HasValue && vm.EndDate < vm.StartDate)
        {
            ModelState.AddModelError("EndDate", "Ngày kết thúc phải sau ngày bắt đầu.");
            return View(vm);
        }

        var projectId = await _projectService.CreateAsync(vm, employeeId);
        TempData["Success"] = "Tạo dự án thành công.";
        return RedirectToAction(nameof(Details), new { id = projectId });
    }

    public async Task<IActionResult> Edit(int id)
    {
        var employeeId = GetCurrentEmployeeId();
        var isSysAdmin = User.IsInRole("Admin");
        if (employeeId == 0 && !isSysAdmin) return Forbid();

        try
        {
            var details = await _projectService.GetDetailsAsync(id, employeeId, ignoreAccessCheck: isSysAdmin);
            if (details == null) return NotFound();

            if (details.IsArchived)
            {
                TempData["Error"] = "Dự án đã lưu trữ chỉ có thể xem, không thể chỉnh sửa.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (!isSysAdmin && details.MyRole != ProjectMemberRole.ProjectOwner)
                return Forbid();

            var vm = new ProjectEditViewModel
            {
                ProjectId = details.ProjectId,
                Name = details.Name,
                Description = details.Description,
                StartDate = details.StartDate,
                EndDate = details.EndDate,
                Status = details.Status
            };
            return View(vm);
        }
        catch (UnauthorizedAccessException)
        {
            return NotFound();
        }
    }

    [HttpPost]
    public async Task<IActionResult> Edit(ProjectEditViewModel vm)
    {
        var employeeId = GetCurrentEmployeeId();
        var isSysAdmin = User.IsInRole("Admin");
        if (employeeId == 0 && !isSysAdmin) return Forbid();

        if (!ModelState.IsValid) return View(vm);

        try
        {
            await _projectService.UpdateAsync(vm, employeeId);
            TempData["Success"] = "Cập nhật dự án thành công.";
            return RedirectToAction(nameof(Details), new { id = vm.ProjectId });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View(vm);
        }
    }

    [HttpPost]
    public async Task<IActionResult> AddMember(AddMemberViewModel vm)
    {
        var employeeId = GetCurrentEmployeeId();
        var isSysAdmin = User.IsInRole("Admin");
        if (employeeId == 0 && !isSysAdmin) return Forbid();

        try
        {
            await _projectService.AddMemberAsync(vm, employeeId);
            TempData["Success"] = "Thêm thành viên thành công.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Details), new { id = vm.ProjectId });
    }

    [HttpPost]
    public async Task<IActionResult> RemoveMember(int projectId, int targetEmployeeId)
    {
        var employeeId = GetCurrentEmployeeId();
        var isSysAdmin = User.IsInRole("Admin");
        if (employeeId == 0 && !isSysAdmin) return Forbid();

        try
        {
            await _projectService.RemoveMemberAsync(projectId, targetEmployeeId, employeeId);
            TempData["Success"] = "Đã xoá thành viên.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Details), new { id = projectId });
    }

    [HttpPost]
    public async Task<IActionResult> ChangeMemberRole(ChangeMemberRoleViewModel vm)
    {
        var employeeId = GetCurrentEmployeeId();
        var isSysAdmin = User.IsInRole("Admin");
        if (employeeId == 0 && !isSysAdmin) return Forbid();

        try
        {
            await _projectService.ChangeMemberRoleAsync(vm, employeeId);
            TempData["Success"] = "Đã cập nhật vai trò.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Details), new { id = vm.ProjectId });
    }

    [HttpPost]
    public async Task<IActionResult> Archive(int id)
    {
        var employeeId = GetCurrentEmployeeId();
        var isSysAdmin = User.IsInRole("Admin");
        if (employeeId == 0 && !isSysAdmin) return Forbid();

        try
        {
            await _projectService.ArchiveAsync(id, employeeId);
            TempData["Success"] = "Dự án đã được lưu trữ. Admin hệ thống có thể xoá vĩnh viễn trong khu lưu trữ.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> Restore(int id)
    {
        var employeeId = GetCurrentEmployeeId();
        if (!User.IsInRole("Admin")) return Forbid();

        try
        {
            await _projectService.RestoreAsync(id, employeeId);
            TempData["Success"] = "Dự án đã được phục hồi khỏi khu lưu trữ.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var employeeId = GetCurrentEmployeeId();
        if (!User.IsInRole("Admin")) return Forbid();

        try
        {
            await _projectService.DeleteAsync(id, employeeId);
            TempData["Success"] = "Đã xoá vĩnh viễn dự án khỏi khu lưu trữ.";
            return RedirectToAction(nameof(Index), new { status = (int)ProjectStatus.Archived });
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    private int GetCurrentEmployeeId()
    {
        var claim = User.FindFirst("EmployeeId")?.Value;
        return int.TryParse(claim, out var id) ? id : 0;
    }

    private async Task<SelectList> GetMemberSelectListAsync(int projectId, int employeeId, bool ignoreAccessCheck)
    {
        var members = await _projectService.GetMembersAsync(projectId, employeeId, ignoreAccessCheck);
        return new SelectList(members.Select(m => new { m.EmployeeId, Display = $"{m.EmployeeName} ({m.EmployeeCode})" }),
            "EmployeeId", "Display");
    }

    private async Task<IReadOnlyList<ProjectMemberCandidateViewModel>> GetNonMemberCandidatesAsync(int projectId,
        int employeeId, bool ignoreAccessCheck)
    {
        var allEmployees = await _employeeService.GetAllAsync();
        var memberIds = (await _projectService.GetMembersAsync(projectId, employeeId, ignoreAccessCheck))
            .Select(m => m.EmployeeId)
            .ToHashSet();

        return allEmployees
            .Where(e => !memberIds.Contains(e.EmployeeId))
            .OrderBy(e => e.Department?.DepartmentName ?? "zzz")
            .ThenBy(e => e.FullName)
            .Select(e => new ProjectMemberCandidateViewModel
            {
                EmployeeId = e.EmployeeId,
                EmployeeName = e.FullName,
                EmployeeCode = e.EmployeeCode,
                DepartmentId = e.DepartmentId,
                DepartmentName = e.Department?.DepartmentName ?? "Chưa có phòng ban"
            })
            .ToList();
    }

    private static void PopulateTimeline(ProjectDetailsViewModel details)
    {
        var timelineRows = FlattenTimelineRows(details.RootTasks).ToList();
        details.TimelineRows = timelineRows;

        var datedRows = timelineRows
            .Where(row => row.StartDate.HasValue || row.DueDate.HasValue)
            .ToList();

        if (datedRows.Count == 0)
            return;

        var firstTaskDate = datedRows.Min(row => row.StartDate ?? row.DueDate!.Value).Date;
        var lastTaskDate = datedRows.Max(row => row.DueDate ?? row.StartDate!.Value).Date;

        details.TimelineStart = (details.StartDate ?? firstTaskDate).Date;
        details.TimelineEnd = (details.EndDate ?? lastTaskDate).Date;

        if (details.TimelineEnd < details.TimelineStart)
            details.TimelineEnd = details.TimelineStart;
    }

    private static IEnumerable<ProjectTimelineRowViewModel> FlattenTimelineRows(IEnumerable<ProjectTaskTreeViewModel> tasks)
    {
        foreach (var task in OrderTasks(tasks))
        {
            yield return new ProjectTimelineRowViewModel
            {
                ProjectTaskId = task.ProjectTaskId,
                ParentTaskId = task.ParentTaskId,
                ParentTaskTitle = task.ParentTaskTitle,
                Depth = task.Depth,
                Title = task.Title,
                Status = task.Status,
                Priority = task.Priority,
                StartDate = task.StartDate,
                DueDate = task.DueDate,
                Progress = task.Progress,
                AssigneeIds = task.AssigneeIds,
                AssigneeNames = task.AssigneeNames
            };

            foreach (var subTask in FlattenTimelineRows(task.SubTasks))
                yield return subTask;
        }
    }

    private static IEnumerable<ProjectTaskTreeViewModel> OrderTasks(IEnumerable<ProjectTaskTreeViewModel> tasks)
        => tasks
            .OrderBy(task => task.StartDate ?? task.DueDate ?? DateTime.MaxValue)
            .ThenBy(task => task.DueDate ?? DateTime.MaxValue)
            .ThenByDescending(task => task.Priority)
            .ThenBy(task => task.Title);

    private static List<ProjectTaskTreeViewModel> FilterTasksForEmployee(
        IEnumerable<ProjectTaskTreeViewModel> tasks, int employeeId, ProjectMemberRole role)
    {
        var result = new List<ProjectTaskTreeViewModel>();
        foreach (var task in tasks)
        {
            if (role == ProjectMemberRole.ProjectManager && task.AssigneeIds.Contains(employeeId))
            {
                result.Add(task);
                continue;
            }

            var childMatches = FilterTasksForEmployee(task.SubTasks, employeeId, role);

            if (role != ProjectMemberRole.ProjectManager && task.AssigneeIds.Contains(employeeId))
            {
                task.SubTasks = childMatches;
                result.Add(task);
            }

            else if (childMatches.Count > 0)
            {
                result.AddRange(childMatches);
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
}
