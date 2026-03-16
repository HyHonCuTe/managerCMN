using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using managerCMN.Data;
using managerCMN.Models.Entities;
using managerCMN.Services.Interfaces;

namespace managerCMN.Controllers;

[Authorize(Policy = "AdminOnly")]
public class SettingsController : Controller
{
    private readonly IDepartmentService _departmentService;
    private readonly IEmployeeService _employeeService;
    private readonly ApplicationDbContext _db;

    public SettingsController(IDepartmentService departmentService, IEmployeeService employeeService, ApplicationDbContext db)
    {
        _departmentService = departmentService;
        _employeeService = employeeService;
        _db = db;
    }

    public async Task<IActionResult> Index(string tab = "departments")
    {
        ViewBag.ActiveTab = tab;
        ViewBag.Departments = await _departmentService.GetAllAsync();
        ViewBag.Positions = await _db.Positions.OrderBy(p => p.SortOrder).ToListAsync();
        ViewBag.JobTitles = await _db.JobTitles.OrderBy(j => j.SortOrder).ToListAsync();
        ViewBag.AssetCategories = await _db.AssetCategories.OrderBy(c => c.CategoryName).ToListAsync();
        ViewBag.Brands = await _db.Brands.OrderBy(b => b.BrandName).ToListAsync();
        ViewBag.Suppliers = await _db.Suppliers.OrderBy(s => s.SupplierName).ToListAsync();

        var employees = await _employeeService.GetAllAsync();
        ViewBag.EmployeeList = new SelectList(employees.OrderBy(e => e.FullName), "EmployeeId", "FullName");

        // Approvers tab
        ViewBag.Approvers = employees.Where(e => e.IsApprover).OrderBy(e => e.FullName).ToList();
        ViewBag.NonApprovers = employees.Where(e => !e.IsApprover && e.Status == Models.Enums.EmployeeStatus.Active)
            .OrderBy(e => e.FullName).ToList();

        return View();
    }

    // === DEPARTMENTS ===

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateDepartment(string departmentName, int? managerId, string? description)
    {
        if (string.IsNullOrWhiteSpace(departmentName))
        {
            TempData["Error"] = "Tên phòng ban không được để trống.";
            return RedirectToAction(nameof(Index), new { tab = "departments" });
        }

        var dept = new Department
        {
            DepartmentName = departmentName.Trim(),
            ManagerId = managerId,
            Description = description?.Trim()
        };
        await _departmentService.CreateAsync(dept);
        TempData["Success"] = "Thêm phòng ban thành công!";
        return RedirectToAction(nameof(Index), new { tab = "departments" });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditDepartment(int departmentId, string departmentName, int? managerId, string? description)
    {
        var dept = await _departmentService.GetByIdAsync(departmentId);
        if (dept == null) return NotFound();

        dept.DepartmentName = departmentName.Trim();
        dept.ManagerId = managerId;
        dept.Description = description?.Trim();
        await _departmentService.UpdateAsync(dept);
        TempData["Success"] = "Cập nhật phòng ban thành công!";
        return RedirectToAction(nameof(Index), new { tab = "departments" });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteDepartment(int id)
    {
        await _departmentService.DeleteAsync(id);
        TempData["Success"] = "Xóa phòng ban thành công!";
        return RedirectToAction(nameof(Index), new { tab = "departments" });
    }

    // === POSITIONS (Vị trí) ===

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreatePosition(string positionName, string? description, int sortOrder)
    {
        if (string.IsNullOrWhiteSpace(positionName))
        {
            TempData["Error"] = "Tên vị trí không được để trống.";
            return RedirectToAction(nameof(Index), new { tab = "positions" });
        }

        _db.Positions.Add(new Position
        {
            PositionName = positionName.Trim(),
            Description = description?.Trim(),
            SortOrder = sortOrder,
            IsActive = true
        });
        await _db.SaveChangesAsync();
        TempData["Success"] = "Thêm vị trí thành công!";
        return RedirectToAction(nameof(Index), new { tab = "positions" });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditPosition(int positionId, string positionName, string? description, int sortOrder, bool isActive)
    {
        var pos = await _db.Positions.FindAsync(positionId);
        if (pos == null) return NotFound();

        pos.PositionName = positionName.Trim();
        pos.Description = description?.Trim();
        pos.SortOrder = sortOrder;
        pos.IsActive = isActive;
        await _db.SaveChangesAsync();
        TempData["Success"] = "Cập nhật vị trí thành công!";
        return RedirectToAction(nameof(Index), new { tab = "positions" });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeletePosition(int id)
    {
        var pos = await _db.Positions.FindAsync(id);
        if (pos != null)
        {
            _db.Positions.Remove(pos);
            await _db.SaveChangesAsync();
        }
        TempData["Success"] = "Xóa vị trí thành công!";
        return RedirectToAction(nameof(Index), new { tab = "positions" });
    }

    // === JOB TITLES (Chức vụ) ===

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateJobTitle(string jobTitleName, string? description, int sortOrder)
    {
        if (string.IsNullOrWhiteSpace(jobTitleName))
        {
            TempData["Error"] = "Tên chức vụ không được để trống.";
            return RedirectToAction(nameof(Index), new { tab = "jobtitles" });
        }

        _db.JobTitles.Add(new JobTitle
        {
            JobTitleName = jobTitleName.Trim(),
            Description = description?.Trim(),
            SortOrder = sortOrder,
            IsActive = true
        });
        await _db.SaveChangesAsync();
        TempData["Success"] = "Thêm chức vụ thành công!";
        return RedirectToAction(nameof(Index), new { tab = "jobtitles" });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditJobTitle(int jobTitleId, string jobTitleName, string? description, int sortOrder, bool isActive)
    {
        var jt = await _db.JobTitles.FindAsync(jobTitleId);
        if (jt == null) return NotFound();

        jt.JobTitleName = jobTitleName.Trim();
        jt.Description = description?.Trim();
        jt.SortOrder = sortOrder;
        jt.IsActive = isActive;
        await _db.SaveChangesAsync();
        TempData["Success"] = "Cập nhật chức vụ thành công!";
        return RedirectToAction(nameof(Index), new { tab = "jobtitles" });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteJobTitle(int id)
    {
        var jt = await _db.JobTitles.FindAsync(id);
        if (jt != null)
        {
            _db.JobTitles.Remove(jt);
            await _db.SaveChangesAsync();
        }
        TempData["Success"] = "Xóa chức vụ thành công!";
        return RedirectToAction(nameof(Index), new { tab = "jobtitles" });
    }

    // === ASSET CATEGORIES ===

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateAssetCategory(string categoryName)
    {
        if (string.IsNullOrWhiteSpace(categoryName))
        {
            TempData["Error"] = "Tên danh mục không được để trống.";
            return RedirectToAction(nameof(Index), new { tab = "categories" });
        }

        _db.AssetCategories.Add(new AssetCategory { CategoryName = categoryName.Trim() });
        await _db.SaveChangesAsync();
        TempData["Success"] = "Thêm danh mục tài sản thành công!";
        return RedirectToAction(nameof(Index), new { tab = "categories" });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditAssetCategory(int assetCategoryId, string categoryName, bool isActive)
    {
        var cat = await _db.AssetCategories.FindAsync(assetCategoryId);
        if (cat == null) return NotFound();

        cat.CategoryName = categoryName.Trim();
        cat.IsActive = isActive;
        await _db.SaveChangesAsync();
        TempData["Success"] = "Cập nhật danh mục thành công!";
        return RedirectToAction(nameof(Index), new { tab = "categories" });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAssetCategory(int id)
    {
        var cat = await _db.AssetCategories.FindAsync(id);
        if (cat != null)
        {
            _db.AssetCategories.Remove(cat);
            await _db.SaveChangesAsync();
        }
        TempData["Success"] = "Xóa danh mục thành công!";
        return RedirectToAction(nameof(Index), new { tab = "categories" });
    }

    // === BRANDS ===

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateBrand(string brandName)
    {
        if (string.IsNullOrWhiteSpace(brandName))
        {
            TempData["Error"] = "Tên hãng sản xuất không được để trống.";
            return RedirectToAction(nameof(Index), new { tab = "brands" });
        }

        _db.Brands.Add(new Brand { BrandName = brandName.Trim() });
        await _db.SaveChangesAsync();
        TempData["Success"] = "Thêm hãng sản xuất thành công!";
        return RedirectToAction(nameof(Index), new { tab = "brands" });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditBrand(int brandId, string brandName, bool isActive)
    {
        var brand = await _db.Brands.FindAsync(brandId);
        if (brand == null) return NotFound();

        brand.BrandName = brandName.Trim();
        brand.IsActive = isActive;
        await _db.SaveChangesAsync();
        TempData["Success"] = "Cập nhật hãng sản xuất thành công!";
        return RedirectToAction(nameof(Index), new { tab = "brands" });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteBrand(int id)
    {
        var brand = await _db.Brands.FindAsync(id);
        if (brand != null)
        {
            _db.Brands.Remove(brand);
            await _db.SaveChangesAsync();
        }
        TempData["Success"] = "Xóa hãng sản xuất thành công!";
        return RedirectToAction(nameof(Index), new { tab = "brands" });
    }

    // === SUPPLIERS ===

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateSupplier(string supplierName, string? phone, string? address)
    {
        if (string.IsNullOrWhiteSpace(supplierName))
        {
            TempData["Error"] = "Tên nhà cung cấp không được để trống.";
            return RedirectToAction(nameof(Index), new { tab = "suppliers" });
        }

        _db.Suppliers.Add(new Supplier
        {
            SupplierName = supplierName.Trim(),
            Phone = phone?.Trim(),
            Address = address?.Trim()
        });
        await _db.SaveChangesAsync();
        TempData["Success"] = "Thêm nhà cung cấp thành công!";
        return RedirectToAction(nameof(Index), new { tab = "suppliers" });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditSupplier(int supplierId, string supplierName, string? phone, string? address, bool isActive)
    {
        var sup = await _db.Suppliers.FindAsync(supplierId);
        if (sup == null) return NotFound();

        sup.SupplierName = supplierName.Trim();
        sup.Phone = phone?.Trim();
        sup.Address = address?.Trim();
        sup.IsActive = isActive;
        await _db.SaveChangesAsync();
        TempData["Success"] = "Cập nhật nhà cung cấp thành công!";
        return RedirectToAction(nameof(Index), new { tab = "suppliers" });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteSupplier(int id)
    {
        var sup = await _db.Suppliers.FindAsync(id);
        if (sup != null)
        {
            _db.Suppliers.Remove(sup);
            await _db.SaveChangesAsync();
        }
        TempData["Success"] = "Xóa nhà cung cấp thành công!";
        return RedirectToAction(nameof(Index), new { tab = "suppliers" });
    }

    // === APPROVERS (Người duyệt đơn) ===

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddApprover(int employeeId)
    {
        var emp = await _db.Employees.FindAsync(employeeId);
        if (emp == null) return NotFound();

        emp.IsApprover = true;
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Đã thêm {emp.FullName} vào danh sách người duyệt!";
        return RedirectToAction(nameof(Index), new { tab = "approvers" });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveApprover(int employeeId)
    {
        var emp = await _db.Employees.FindAsync(employeeId);
        if (emp == null) return NotFound();

        emp.IsApprover = false;
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Đã xóa {emp.FullName} khỏi danh sách người duyệt!";
        return RedirectToAction(nameof(Index), new { tab = "approvers" });
    }
}
