using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using managerCMN.Models.ViewModels;
using managerCMN.Services.Interfaces;

namespace managerCMN.Controllers;

[Authorize(Roles = "Admin")]
public class ProjectTemplateController : Controller
{
    private readonly IProjectTemplateService _templateService;
    private readonly IEmployeeService _employeeService;

    public ProjectTemplateController(IProjectTemplateService templateService, IEmployeeService employeeService)
    {
        _templateService = templateService;
        _employeeService = employeeService;
    }

    public async Task<IActionResult> Index()
    {
        var templates = await _templateService.GetAllAsync();
        return View(templates);
    }

    public IActionResult Create()
    {
        return View(new ProjectTemplateCreateViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Create(ProjectTemplateCreateViewModel vm)
    {
        // Remove validation errors for nested task items that are empty placeholders
        CleanupTaskValidation(vm.Tasks);

        if (!ModelState.IsValid) return View(vm);

        var employeeId = GetCurrentEmployeeId();
        if (employeeId == 0) return Forbid();

        try
        {
            var id = await _templateService.CreateAsync(vm, employeeId);
            TempData["Success"] = "Tạo template thành công.";
            return RedirectToAction(nameof(Edit), new { id });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(vm);
        }
    }

    public async Task<IActionResult> Edit(int id)
    {
        var detail = await _templateService.GetByIdAsync(id);
        if (detail == null) return NotFound();

        var vm = new ProjectTemplateEditViewModel
        {
            ProjectTemplateId = detail.ProjectTemplateId,
            Name = detail.Name,
            Description = detail.Description,
            IsActive = detail.IsActive,
            Tasks = detail.Tasks
        };

        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(ProjectTemplateEditViewModel vm)
    {
        CleanupTaskValidation(vm.Tasks);

        if (!ModelState.IsValid) return View(vm);

        try
        {
            await _templateService.UpdateAsync(vm);
            TempData["Success"] = "Cập nhật template thành công.";
            return RedirectToAction(nameof(Edit), new { id = vm.ProjectTemplateId });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(vm);
        }
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _templateService.DeleteAsync(id);
            TempData["Success"] = "Đã xóa template.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var detail = await _templateService.GetByIdAsync(id);
        if (detail == null) return NotFound();

        var vm = new ProjectTemplateEditViewModel
        {
            ProjectTemplateId = detail.ProjectTemplateId,
            Name = detail.Name,
            Description = detail.Description,
            IsActive = !detail.IsActive,
            Tasks = detail.Tasks
        };

        await _templateService.UpdateAsync(vm);
        TempData["Success"] = vm.IsActive ? "Template đã được kích hoạt." : "Template đã tắt kích hoạt.";
        return RedirectToAction(nameof(Index));
    }

    private int GetCurrentEmployeeId()
    {
        var claim = User.FindFirst("EmployeeId");
        return claim != null && int.TryParse(claim.Value, out var id) ? id : 0;
    }

    private void CleanupTaskValidation(List<ProjectTemplateTaskFormViewModel> tasks)
    {
        // Remove ModelState errors for any task entries that have been removed on client
        var keysToRemove = ModelState.Keys
            .Where(k => k.StartsWith("Tasks["))
            .ToList();

        // Only keep errors for tasks that actually exist
        for (int i = tasks.Count; ; i++)
        {
            var prefix = $"Tasks[{i}]";
            var hasKey = ModelState.Keys.Any(k => k.StartsWith(prefix));
            if (!hasKey) break;
            foreach (var key in ModelState.Keys.Where(k => k.StartsWith(prefix)).ToList())
                ModelState.Remove(key);
        }
    }
}
