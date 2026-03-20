using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using managerCMN.Data;
using managerCMN.Models.Entities;
using managerCMN.Models.Enums;
using managerCMN.Models.ViewModels;
using managerCMN.Services.Interfaces;
using managerCMN.Helpers;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace managerCMN.Controllers;

[Authorize]
public class AssetController : Controller
{
    private readonly IAssetService _assetService;
    private readonly IEmployeeService _employeeService;
    private readonly ApplicationDbContext _db;

    public AssetController(IAssetService assetService, IEmployeeService employeeService, ApplicationDbContext db)
    {
        _assetService = assetService;
        _employeeService = employeeService;
        _db = db;
    }

    private async Task PopulateDropdownsAsync(int? categoryId = null, int? brandId = null, int? supplierId = null)
    {
        ViewBag.Categories = new SelectList(
            await _db.AssetCategories.Where(c => c.IsActive).OrderBy(c => c.CategoryName).ToListAsync(),
            "AssetCategoryId", "CategoryName", categoryId);
        ViewBag.Brands = new SelectList(
            await _db.Brands.Where(b => b.IsActive).OrderBy(b => b.BrandName).ToListAsync(),
            "BrandId", "BrandName", brandId);
        ViewBag.Suppliers = new SelectList(
            await _db.Suppliers.Where(s => s.IsActive).OrderBy(s => s.SupplierName).ToListAsync(),
            "SupplierId", "SupplierName", supplierId);
    }

    private async Task PopulateFilterDropdownsAsync(AssetFilterViewModel filter)
    {
        // Categories
        var categories = await _db.AssetCategories.Where(c => c.IsActive)
            .OrderBy(c => c.CategoryName).ToListAsync();
        filter.Categories = categories.Select(c => new SelectListItem
        {
            Value = c.AssetCategoryId.ToString(),
            Text = c.CategoryName,
            Selected = filter.CategoryId == c.AssetCategoryId
        });

        // Brands
        var brands = await _db.Brands.Where(b => b.IsActive)
            .OrderBy(b => b.BrandName).ToListAsync();
        filter.Brands = brands.Select(b => new SelectListItem
        {
            Value = b.BrandId.ToString(),
            Text = b.BrandName,
            Selected = filter.BrandId == b.BrandId
        });

        // Employees
        var employees = await _employeeService.GetAllAsync();
        filter.Employees = employees.Select(e => new SelectListItem
        {
            Value = e.EmployeeId.ToString(),
            Text = e.FullName,
            Selected = filter.EmployeeId == e.EmployeeId
        });

        // Asset Statuses
        filter.Statuses = Enum.GetValues<AssetStatus>().Select(status => new SelectListItem
        {
            Value = ((int)status).ToString(),
            Text = status.ToString(),
            Selected = filter.Status == status
        });

        // Assignment Reasons
        filter.AssignmentReasons = Enum.GetValues<AssetAssignmentReason>().Select(reason => new SelectListItem
        {
            Value = ((int)reason).ToString(),
            Text = GetAssignmentReasonDisplayName(reason),
            Selected = filter.AssignmentReason == reason
        });
    }

    private static string GetAssignmentReasonDisplayName(AssetAssignmentReason reason) => reason switch
    {
        AssetAssignmentReason.NewEmployee => "Nhân viên mới",
        AssetAssignmentReason.ProjectNeeds => "Nhu cầu dự án",
        AssetAssignmentReason.Replacement => "Thay thế",
        AssetAssignmentReason.Upgrade => "Nâng cấp",
        AssetAssignmentReason.Temporary => "Tạm thời",
        _ => "Khác"
    };

    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Index(AssetFilterViewModel? filter)
    {
        filter ??= new AssetFilterViewModel();

        var assets = await _assetService.GetFilteredAsync(filter);

        // Populate filter dropdowns
        await PopulateFilterDropdownsAsync(filter);

        var viewModel = new AssetIndexViewModel
        {
            Assets = assets,
            Filter = filter
        };

        return View(viewModel);
    }

    // MyAssets action for employees - accessible to all authenticated users
    public async Task<IActionResult> MyAssets()
    {
        var empIdClaim = User.FindFirst("EmployeeId");
        if (empIdClaim == null || !int.TryParse(empIdClaim.Value, out var empId))
        {
            TempData["Error"] = "Không tìm thấy thông tin nhân viên.";
            return RedirectToAction("Index", "Dashboard");
        }

        var myAssets = await _assetService.GetMyAssetsAsync(empId);
        ViewBag.EmployeeName = User.FindFirst(ClaimTypes.Name)?.Value;

        return View(myAssets);
    }

    // Lifecycle History
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> LifecycleHistory(int id)
    {
        var asset = await _assetService.GetByIdAsync(id);
        if (asset == null) return NotFound();

        var history = await _assetService.GetLifecycleHistoryAsync(id);
        ViewBag.Asset = asset;

        return View(history);
    }

    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Details(int id)
    {
        var asset = await _assetService.GetWithConfigurationAsync(id);
        if (asset == null) return NotFound();
        return View(asset);
    }

    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Create()
    {
        await PopulateDropdownsAsync();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Create(AssetCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync(model.AssetCategoryId, model.BrandId, model.SupplierId);
            return View(model);
        }

        var asset = new Asset
        {
            AssetCode = model.AssetCode,
            AssetName = model.AssetName,
            AssetCategoryId = model.AssetCategoryId,
            BrandId = model.BrandId,
            SupplierId = model.SupplierId,
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

    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Edit(int id)
    {
        var asset = await _assetService.GetWithConfigurationAsync(id);
        if (asset == null) return NotFound();
        await PopulateDropdownsAsync(asset.AssetCategoryId, asset.BrandId, asset.SupplierId);
        return View(asset);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Edit(Asset asset)
    {
        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync(asset.AssetCategoryId, asset.BrandId, asset.SupplierId);
            return View(asset);
        }

        await _assetService.UpdateAsync(asset);
        TempData["Success"] = "Đã cập nhật thông tin tài sản thành công!";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Assign(int id)
    {
        var asset = await _assetService.GetByIdAsync(id);
        if (asset == null) return NotFound();

        var employees = await _employeeService.GetAllAsync();
        ViewBag.Employees = new SelectList(employees, "EmployeeId", "FullName");
        ViewBag.Asset = asset;

        // Add assignment reasons dropdown
        ViewBag.AssignmentReasons = Enum.GetValues<AssetAssignmentReason>().Select(reason => new SelectListItem
        {
            Value = ((int)reason).ToString(),
            Text = GetAssignmentReasonDisplayName(reason)
        });

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Assign(AssetAssignment assignment, AssetAssignmentReason assignmentReason, string? assignmentCondition)
    {
        assignment.AssignedDate = DateTime.UtcNow;
        assignment.Status = AssetAssignmentStatus.Assigned;

        // Use enhanced assignment method with reason
        await _assetService.AssignToEmployeeAsync(assignment, assignmentReason, assignmentCondition);

        TempData["Success"] = "Tài sản đã được cấp phát thành công!";
        return RedirectToAction(nameof(Details), new { id = assignment.AssetId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Return(int assignmentId, AssetReturnReason returnReason, string? returnCondition)
    {
        // Use enhanced return method with reason
        await _assetService.ReturnAssetAsync(assignmentId, returnReason, returnCondition);

        TempData["Success"] = "Tài sản đã được thu hồi thành công!";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Policy = "AdminOnly")]
    public IActionResult Import() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Import(IFormFile excelFile)
    {
        // Validate using FileUploadHelper
        var validationResult = FileUploadHelper.ValidateFile(
            excelFile,
            FileUploadHelper.AllowedExcelExtensions,
            true);

        if (validationResult != ValidationResult.Success)
        {
            ModelState.AddModelError("", validationResult.ErrorMessage ?? "File không hợp lệ.");
            return View();
        }

        using var stream = excelFile.OpenReadStream();
        await _assetService.ImportFromExcelAsync(stream);

        TempData["Success"] = "Import tài sản thành công!";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(int id)
    {
        await _assetService.DeleteAsync(id);
        return RedirectToAction(nameof(Index));
    }
}
