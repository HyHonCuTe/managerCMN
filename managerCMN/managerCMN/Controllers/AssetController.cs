using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using managerCMN.Models.Entities;
using managerCMN.Models.Enums;
using managerCMN.Models.ViewModels;
using managerCMN.Services.Interfaces;

namespace managerCMN.Controllers;

[Authorize(Policy = "AdminOnly")]
public class AssetController : Controller
{
    private readonly IAssetService _assetService;
    private readonly IEmployeeService _employeeService;

    public AssetController(IAssetService assetService, IEmployeeService employeeService)
    {
        _assetService = assetService;
        _employeeService = employeeService;
    }

    public async Task<IActionResult> Index()
    {
        var assets = await _assetService.GetAllAsync();
        return View(assets);
    }

    public async Task<IActionResult> Details(int id)
    {
        var asset = await _assetService.GetWithConfigurationAsync(id);
        if (asset == null) return NotFound();
        return View(asset);
    }

    public IActionResult Create() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AssetCreateViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var asset = new Asset
        {
            AssetCode = model.AssetCode,
            AssetName = model.AssetName,
            Category = model.Category,
            Brand = model.Brand,
            Supplier = model.Supplier,
            PurchaseDate = model.PurchaseDate,
            PurchasePrice = model.PurchasePrice,
            Status = AssetStatus.Available
        };

        // Add configuration if any field is filled
        if (!string.IsNullOrEmpty(model.CPU) || !string.IsNullOrEmpty(model.RAM))
        {
            asset.Configuration = new AssetConfiguration
            {
                CPU = model.CPU,
                Mainboard = model.Mainboard,
                RAM = model.RAM,
                SSD = model.SSD,
                HDD = model.HDD,
                VGA = model.VGA,
                OS = model.OS
            };
        }

        await _assetService.CreateAsync(asset);
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var asset = await _assetService.GetWithConfigurationAsync(id);
        if (asset == null) return NotFound();
        return View(asset);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Asset asset)
    {
        if (!ModelState.IsValid) return View(asset);

        await _assetService.UpdateAsync(asset);
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Assign(int id)
    {
        var asset = await _assetService.GetByIdAsync(id);
        if (asset == null) return NotFound();

        var employees = await _employeeService.GetAllAsync();
        ViewBag.Employees = new SelectList(employees, "EmployeeId", "FullName");
        ViewBag.Asset = asset;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Assign(AssetAssignment assignment)
    {
        assignment.AssignedDate = DateTime.UtcNow;
        assignment.Status = AssetAssignmentStatus.Assigned;
        await _assetService.AssignToEmployeeAsync(assignment);
        return RedirectToAction(nameof(Details), new { id = assignment.AssetId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Return(int assignmentId, string? condition)
    {
        await _assetService.ReturnAssetAsync(assignmentId, condition);
        return RedirectToAction(nameof(Index));
    }

    public IActionResult Import() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Import(IFormFile excelFile)
    {
        if (excelFile == null || excelFile.Length == 0)
        {
            ModelState.AddModelError("", "Vui lòng chọn file Excel.");
            return View();
        }

        using var stream = excelFile.OpenReadStream();
        await _assetService.ImportFromExcelAsync(stream);

        TempData["Success"] = "Import tài sản thành công!";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        await _assetService.DeleteAsync(id);
        return RedirectToAction(nameof(Index));
    }
}
