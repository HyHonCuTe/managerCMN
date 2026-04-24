using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using managerCMN.Data;
using managerCMN.Helpers;
using managerCMN.Models.Entities;
using managerCMN.Models.Enums;
using managerCMN.Models.ViewModels;
using managerCMN.Services.Interfaces;

namespace managerCMN.Controllers;

[Authorize(Policy = "AdminOnly")]
public class SettingsController : Controller
{
    private const string AdminRoleName = "Admin";
    private const string MasterAdminEmployeeCode = "A00000";
    private const string FullAttendanceTableName = "FullAttendanceEmployees";

    private readonly IDepartmentService _departmentService;
    private readonly IEmployeeService _employeeService;
    private readonly IPermissionService _permissionService;
    private readonly ApplicationDbContext _db;
    private readonly ISystemLogService _systemLogService;

    public SettingsController(
        IDepartmentService departmentService,
        IEmployeeService employeeService,
        IPermissionService permissionService,
        ApplicationDbContext db,
        ISystemLogService systemLogService)
    {
        _departmentService = departmentService;
        _employeeService = employeeService;
        _permissionService = permissionService;
        _db = db;
        _systemLogService = systemLogService;
    }

    private bool IsMasterAdmin()
        => User.IsInRole(AdminRoleName) && User.HasClaim("EmployeeCode", MasterAdminEmployeeCode);

    private static bool IsMissingTableException(SqlException ex, string tableName)
        => ex.Message.Contains("Invalid object name", StringComparison.OrdinalIgnoreCase)
        && ex.Message.Contains(tableName, StringComparison.OrdinalIgnoreCase);

    private async Task<bool> IsFullAttendanceFeatureAvailableAsync()
    {
        try
        {
            await _db.FullAttendanceEmployees.AsNoTracking().AnyAsync();
            return true;
        }
        catch (SqlException ex) when (IsMissingTableException(ex, FullAttendanceTableName))
        {
            return false;
        }
    }

    private IActionResult RedirectFullAttendanceMigrationRequired()
    {
        TempData["Error"] = "Tính năng chấm công đầy đủ chưa sẵn sàng vì database chưa có bảng FullAttendanceEmployees. Vui lòng chạy migration mới nhất bằng 'dotnet ef database update'.";
        return RedirectToAction(nameof(Index), new { tab = "fullattendance" });
    }

    private async Task LoadFullAttendanceViewDataAsync(IEnumerable<Employee> employees)
    {
        try
        {
            var fullAttendanceEmployees = await _db.FullAttendanceEmployees
                .Include(f => f.Employee)
                .OrderBy(f => f.Employee.FullName)
                .ToListAsync();

            var fullAttendanceEmployeeIds = fullAttendanceEmployees
                .Select(f => f.EmployeeId)
                .ToHashSet();

            ViewBag.FullAttendanceEmployees = fullAttendanceEmployees;
            ViewBag.AvailableEmployeesForFullAttendance = employees
                .Where(e => e.Status == Models.Enums.EmployeeStatus.Active &&
                            !fullAttendanceEmployeeIds.Contains(e.EmployeeId))
                .OrderBy(e => e.FullName)
                .ToList();
            ViewBag.FullAttendanceFeatureAvailable = true;
            ViewBag.FullAttendanceFeatureMessage = null;
        }
        catch (SqlException ex) when (IsMissingTableException(ex, FullAttendanceTableName))
        {
            ViewBag.FullAttendanceEmployees = new List<FullAttendanceEmployee>();
            ViewBag.AvailableEmployeesForFullAttendance = new List<Employee>();
            ViewBag.FullAttendanceFeatureAvailable = false;
            ViewBag.FullAttendanceFeatureMessage = "Database hiện chưa có bảng FullAttendanceEmployees. Tab này chỉ hoạt động sau khi chạy migration mới nhất bằng 'dotnet ef database update'.";
        }
    }

    private static List<DepartmentApprover1SettingsViewModel> BuildApprover1Settings(
        IEnumerable<Department> departments,
        IEnumerable<Employee> employees)
    {
        var activeEmployees = employees
            .Where(e => e.Status == EmployeeStatus.Active && e.DepartmentId.HasValue)
            .OrderBy(e => e.FullName)
            .ToList();

        return departments
            .OrderBy(d => d.DepartmentName)
            .Select(d => new DepartmentApprover1SettingsViewModel
            {
                DepartmentId = d.DepartmentId,
                DepartmentName = d.DepartmentName,
                Employees = activeEmployees
                    .Where(e => e.DepartmentId == d.DepartmentId)
                    .Select(e => new Approver1EmployeeOptionViewModel
                    {
                        EmployeeId = e.EmployeeId,
                        EmployeeCode = e.EmployeeCode,
                        FullName = e.FullName,
                        JobTitleName = e.JobTitle?.JobTitleName,
                        Status = e.Status,
                        IsApprover1 = e.IsApprover1
                    })
                    .ToList()
            })
            .ToList();
    }

    public async Task<IActionResult> Index(string tab = "departments")
    {
        tab = string.IsNullOrWhiteSpace(tab) ? "departments" : tab.Trim().ToLowerInvariant();
        ViewBag.ActiveTab = tab;
        var departments = await _departmentService.GetAllAsync();
        ViewBag.Departments = departments;
        ViewBag.Positions = await _db.Positions.OrderBy(p => p.SortOrder).ToListAsync();
        ViewBag.JobTitles = await _db.JobTitles.OrderBy(j => j.SortOrder).ToListAsync();
        ViewBag.AssetCategories = await _db.AssetCategories.OrderBy(c => c.CategoryName).ToListAsync();
        ViewBag.Brands = await _db.Brands.OrderBy(b => b.BrandName).ToListAsync();
        ViewBag.Suppliers = await _db.Suppliers.OrderBy(s => s.SupplierName).ToListAsync();
        ViewBag.Holidays = await _db.Holidays.OrderBy(h => h.Date).ToListAsync();

        var employees = await _employeeService.GetAllAsync();
        ViewBag.EmployeeList = new SelectList(employees.OrderBy(e => e.FullName), "EmployeeId", "FullName");

        // Approvers tab
        ViewBag.Approvers = employees.Where(e => e.IsApprover).OrderBy(e => e.FullName).ToList();
        ViewBag.NonApprovers = employees.Where(e => !e.IsApprover && e.Status == Models.Enums.EmployeeStatus.Active)
            .OrderBy(e => e.FullName).ToList();
        ViewBag.Approver1Departments = BuildApprover1Settings(departments, employees);

        // Full Attendance tab
        await LoadFullAttendanceViewDataAsync(employees);

        // Permissions tab
        if (tab == "permissions")
        {
            ViewBag.IsMasterAdmin = IsMasterAdmin();
            ViewBag.Users = await _db.Users
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .Include(u => u.Employee)
                .Where(u => u.IsActive)
                .OrderBy(u => u.Email)
                .ToListAsync();

            ViewBag.Roles = await _db.Roles.OrderBy(r => r.RoleName).ToListAsync();
            ViewBag.PermissionsByCategory = await _permissionService.GetAllPermissionsGroupedByCategoryAsync();
        }

        // Announcements tab
        ViewBag.Announcements = await _db.ScheduledAnnouncements
            .Include(a => a.FilterDepartment)
            .OrderByDescending(a => a.ScheduledAt)
            .Take(50)
            .ToListAsync();
        ViewBag.ActiveEmployees = employees
            .Where(e => e.Status == Models.Enums.EmployeeStatus.Active)
            .OrderBy(e => e.Department?.DepartmentName)
            .ThenBy(e => e.FullName)
            .ToList();

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

    // === HOLIDAYS (Ngày nghỉ lễ) ===

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateHoliday(DateOnly date, string name, string? description, bool isRecurring)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Error"] = "Tên ngày nghỉ không được để trống.";
            return RedirectToAction(nameof(Index), new { tab = "holidays" });
        }

        // Check for duplicate date
        if (await _db.Holidays.AnyAsync(h => h.Date == date))
        {
            TempData["Error"] = "Ngày này đã có trong danh sách nghỉ lễ.";
            return RedirectToAction(nameof(Index), new { tab = "holidays" });
        }

        _db.Holidays.Add(new Holiday
        {
            Date = date,
            Name = name.Trim(),
            Description = description?.Trim(),
            IsRecurring = isRecurring,
            IsActive = true
        });
        await _db.SaveChangesAsync();
        TempData["Success"] = "Thêm ngày nghỉ lễ thành công!";
        return RedirectToAction(nameof(Index), new { tab = "holidays" });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditHoliday(int holidayId, DateOnly date, string name, string? description, bool isRecurring, bool isActive)
    {
        var holiday = await _db.Holidays.FindAsync(holidayId);
        if (holiday == null) return NotFound();

        // Check for duplicate date (excluding current holiday)
        if (await _db.Holidays.AnyAsync(h => h.Date == date && h.HolidayId != holidayId))
        {
            TempData["Error"] = "Ngày này đã có trong danh sách nghỉ lễ.";
            return RedirectToAction(nameof(Index), new { tab = "holidays" });
        }

        holiday.Date = date;
        holiday.Name = name.Trim();
        holiday.Description = description?.Trim();
        holiday.IsRecurring = isRecurring;
        holiday.IsActive = isActive;
        await _db.SaveChangesAsync();
        TempData["Success"] = "Cập nhật ngày nghỉ lễ thành công!";
        return RedirectToAction(nameof(Index), new { tab = "holidays" });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteHoliday(int id)
    {
        var holiday = await _db.Holidays.FindAsync(id);
        if (holiday != null)
        {
            _db.Holidays.Remove(holiday);
            await _db.SaveChangesAsync();
        }
        TempData["Success"] = "Xóa ngày nghỉ lễ thành công!";
        return RedirectToAction(nameof(Index), new { tab = "holidays" });
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

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateApprover1ByDepartment(int departmentId, int[]? approver1EmployeeIds)
    {
        var department = await _db.Departments.FindAsync(departmentId);
        if (department == null) return NotFound();

        var selectedIds = (approver1EmployeeIds ?? Array.Empty<int>()).ToHashSet();
        var departmentEmployees = await _db.Employees
            .Where(e => e.DepartmentId == departmentId && e.Status == EmployeeStatus.Active)
            .ToListAsync();

        var validIds = departmentEmployees.Select(e => e.EmployeeId).ToHashSet();
        selectedIds.IntersectWith(validIds);

        var before = departmentEmployees
            .Where(e => e.IsApprover1)
            .Select(e => new { e.EmployeeId, e.EmployeeCode, e.FullName })
            .OrderBy(e => e.FullName)
            .ToArray();

        foreach (var employee in departmentEmployees)
        {
            employee.IsApprover1 = selectedIds.Contains(employee.EmployeeId);
        }

        await _db.SaveChangesAsync();

        var after = departmentEmployees
            .Where(e => e.IsApprover1)
            .Select(e => new { e.EmployeeId, e.EmployeeCode, e.FullName })
            .OrderBy(e => e.FullName)
            .ToArray();

        await _systemLogService.LogAsync(
            GetCurrentUserId(),
            "Cap nhat nguoi duyet 1 theo phong ban",
            "Employee",
            new { department.DepartmentId, department.DepartmentName, Approver1 = before },
            new { department.DepartmentId, department.DepartmentName, Approver1 = after },
            GetClientIP());

        TempData["Success"] = $"Đã cập nhật người duyệt 1 cho phòng ban {department.DepartmentName}.";
        return RedirectToAction(nameof(Index), new { tab = "approvers" });
    }

    // === PERMISSIONS (Phân quyền) ===

    [HttpGet]
    public async Task<IActionResult> GetRolePermissions(int roleId)
    {
        var permissions = await _permissionService.GetPermissionsByRoleIdAsync(roleId);
        return Json(permissions.Select(p => p.PermissionId));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateRolePermissions(int roleId, int[] permissionIds)
    {
        try
        {
            permissionIds ??= Array.Empty<int>();
            await _permissionService.UpdateRolePermissionsAsync(roleId, permissionIds);
            return Json(new { success = true, message = "Đã cập nhật phân quyền thành công!" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateUserRoles(int userId, int[] roleIds)
    {
        try
        {
            roleIds ??= Array.Empty<int>();

            var adminRoleId = await _db.Roles
                .Where(r => r.RoleName == AdminRoleName)
                .Select(r => (int?)r.RoleId)
                .FirstOrDefaultAsync();

            if (!adminRoleId.HasValue)
                throw new InvalidOperationException("Không tìm thấy role Admin trong hệ thống.");

            var targetUser = await _db.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Include(u => u.Employee)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (targetUser == null)
            {
                TempData["Error"] = "Không tìm thấy người dùng cần cập nhật.";
                return RedirectToAction(nameof(Index), new { tab = "permissions" });
            }

            var isMasterAdmin = IsMasterAdmin();
            var targetIsAdmin = targetUser.UserRoles.Any(ur => ur.RoleId == adminRoleId.Value);
            var requestedAdminRole = roleIds.Contains(adminRoleId.Value);

            if (!isMasterAdmin)
            {
                if (targetIsAdmin)
                {
                    TempData["Error"] = "Chỉ admin có mã nhân viên A00000 mới được thay đổi vai trò của người dùng đang là Admin.";
                    return RedirectToAction(nameof(Index), new { tab = "permissions" });
                }

                if (requestedAdminRole)
                {
                    TempData["Error"] = "Chỉ admin có mã nhân viên A00000 mới được cấp vai trò Admin cho người dùng khác.";
                    return RedirectToAction(nameof(Index), new { tab = "permissions" });
                }
            }

            var previousRoles = targetUser.UserRoles
                .Select(ur => new
                {
                    ur.RoleId,
                    RoleName = ur.Role?.RoleName
                })
                .OrderBy(ur => ur.RoleId)
                .ToArray();

            // Remove existing user roles
            var existingUserRoles = await _db.UserRoles.Where(ur => ur.UserId == userId).ToListAsync();
            _db.UserRoles.RemoveRange(existingUserRoles);

            // Add new user roles
            var newUserRoles = roleIds.Select(roleId => new UserRole
            {
                UserId = userId,
                RoleId = roleId,
                AssignedDate = DateTime.UtcNow
            }).ToList();

            await _db.UserRoles.AddRangeAsync(newUserRoles);
            await _db.SaveChangesAsync();

            var updatedRoles = await _db.Roles
                .Where(role => roleIds.Contains(role.RoleId))
                .Select(role => new
                {
                    role.RoleId,
                    RoleName = role.RoleName
                })
                .OrderBy(role => role.RoleId)
                .ToArrayAsync();

            await _systemLogService.LogAsync(
                GetCurrentUserId(),
                "Cap nhat vai tro nguoi dung",
                "UserRole",
                new
                {
                    TargetUserId = targetUser.UserId,
                    targetUser.Email,
                    EmployeeCode = targetUser.Employee?.EmployeeCode,
                    Roles = previousRoles
                },
                new
                {
                    TargetUserId = targetUser.UserId,
                    targetUser.Email,
                    EmployeeCode = targetUser.Employee?.EmployeeCode,
                    Roles = updatedRoles
                },
                GetClientIP());

            TempData["Success"] = "Đã cập nhật vai trò người dùng thành công!";
            return RedirectToAction(nameof(Index), new { tab = "permissions" });
        }
        catch (Exception ex)
        {
            TempData["Error"] = "Có lỗi xảy ra: " + ex.Message;
            return RedirectToAction(nameof(Index), new { tab = "permissions" });
        }
    }

    // === FULL ATTENDANCE SETTINGS (Chấm công đầy đủ tự động) ===

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var id) ? id : null;
    }

    private string? GetClientIP()
        => HttpContext.Connection.RemoteIpAddress?.ToString();

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddFullAttendanceEmployee(int employeeId, string? reason)
    {
        try
        {
            if (!await IsFullAttendanceFeatureAvailableAsync())
                return RedirectFullAttendanceMigrationRequired();

            var existing = await _db.FullAttendanceEmployees.FirstOrDefaultAsync(f => f.EmployeeId == employeeId);
            if (existing != null)
            {
                TempData["Error"] = "Nhân viên này đã được thêm vào danh sách!";
                return RedirectToAction(nameof(Index), new { tab = "fullattendance" });
            }

            var emp = await _db.Employees.FindAsync(employeeId);
            if (emp == null) return NotFound();

            var fullAttendanceEmp = new FullAttendanceEmployee
            {
                EmployeeId = employeeId,
                Reason = reason?.Trim(),
                CreatedDate = DateTime.UtcNow
            };

            _db.FullAttendanceEmployees.Add(fullAttendanceEmp);
            await _db.SaveChangesAsync();

            TempData["Success"] = $"Đã thêm {emp.FullName} vào danh sách chấm công đầy đủ!";
            return RedirectToAction(nameof(Index), new { tab = "fullattendance" });
        }
        catch (Exception ex)
        {
            TempData["Error"] = "Có lỗi xảy ra: " + ex.Message;
            return RedirectToAction(nameof(Index), new { tab = "fullattendance" });
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditFullAttendanceEmployee(int id, string? reason)
    {
        try
        {
            if (!await IsFullAttendanceFeatureAvailableAsync())
                return RedirectFullAttendanceMigrationRequired();

            var fullAttendanceEmp = await _db.FullAttendanceEmployees.FindAsync(id);
            if (fullAttendanceEmp == null) return NotFound();

            fullAttendanceEmp.Reason = reason?.Trim();
            fullAttendanceEmp.UpdatedDate = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            TempData["Success"] = "Đã cập nhật thông tin thành công!";
            return RedirectToAction(nameof(Index), new { tab = "fullattendance" });
        }
        catch (Exception ex)
        {
            TempData["Error"] = "Có lỗi xảy ra: " + ex.Message;
            return RedirectToAction(nameof(Index), new { tab = "fullattendance" });
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteFullAttendanceEmployee(int id)
    {
        try
        {
            if (!await IsFullAttendanceFeatureAvailableAsync())
                return RedirectFullAttendanceMigrationRequired();

            var fullAttendanceEmp = await _db.FullAttendanceEmployees.Include(f => f.Employee).FirstOrDefaultAsync(f => f.Id == id);
            if (fullAttendanceEmp == null) return NotFound();

            var empName = fullAttendanceEmp.Employee.FullName;
            _db.FullAttendanceEmployees.Remove(fullAttendanceEmp);
            await _db.SaveChangesAsync();

            TempData["Success"] = $"Đã xóa {empName} khỏi danh sách chấm công đầy đủ!";
            return RedirectToAction(nameof(Index), new { tab = "fullattendance" });
        }
        catch (Exception ex)
        {
            TempData["Error"] = "Có lỗi xảy ra: " + ex.Message;
            return RedirectToAction(nameof(Index), new { tab = "fullattendance" });
        }
    }

    // === SCHEDULED ANNOUNCEMENTS ===

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateAnnouncement(
        string title, string content, DateTime scheduledAt,
        string targetMode, int? filterDepartmentId, int? filterGender, string? filterEmployeeIds)
    {
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(content))
        {
            TempData["Error"] = "Tiêu đề và nội dung không được để trống.";
            return RedirectToAction(nameof(Index), "Notification");
        }

        if (scheduledAt <= DateTimeHelper.VietnamNow)
        {
            TempData["Error"] = "Thời gian gửi phải là thời gian trong tương lai.";
            return RedirectToAction(nameof(Index), "Notification");
        }

        var employeeIdClaim = User.FindFirst("EmployeeId")?.Value;
        _ = int.TryParse(employeeIdClaim, out var creatorEmployeeId);

        string? resolvedFilterEmployeeIds = null;
        if (targetMode == "specific" && !string.IsNullOrWhiteSpace(filterEmployeeIds))
        {
            // Validate the JSON list
            try
            {
                var ids = JsonSerializer.Deserialize<List<int>>(filterEmployeeIds);
                if (ids == null || ids.Count == 0)
                {
                    TempData["Error"] = "Vui lòng chọn ít nhất một người nhận.";
                    return RedirectToAction(nameof(Index), "Notification");
                }
                resolvedFilterEmployeeIds = JsonSerializer.Serialize(ids);
            }
            catch
            {
                TempData["Error"] = "Dữ liệu người nhận không hợp lệ.";
                return RedirectToAction(nameof(Index), "Notification");
            }
        }

        var ann = new ScheduledAnnouncement
        {
            Title = title.Trim(),
            Content = content.Trim(),
            ScheduledAt = scheduledAt,
            CreatedByEmployeeId = creatorEmployeeId,
            FilterDepartmentId = targetMode == "filter" ? filterDepartmentId : null,
            FilterGender = targetMode == "filter" ? filterGender : null,
            FilterEmployeeIds = resolvedFilterEmployeeIds
        };

        _db.ScheduledAnnouncements.Add(ann);
        await _db.SaveChangesAsync();

        TempData["Success"] = $"Đã lên lịch thông báo \"{ann.Title}\" vào {scheduledAt:dd/MM/yyyy HH:mm}.";
        return RedirectToAction(nameof(Index), "Notification");
            
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelAnnouncement(int id)
    {
        var ann = await _db.ScheduledAnnouncements.FindAsync(id);
        if (ann == null) return NotFound();

        if (ann.Status != AnnouncementStatus.Pending)
        {
            TempData["Error"] = "Chỉ có thể huỷ thông báo đang chờ gửi.";
            return RedirectToAction(nameof(Index), "Notification");
        }

        ann.Status = AnnouncementStatus.Cancelled;
        await _db.SaveChangesAsync();

        TempData["Success"] = "Đã huỷ thông báo.";
        return RedirectToAction(nameof(Index), "Notification");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAnnouncement(int id)
    {
        var ann = await _db.ScheduledAnnouncements.FindAsync(id);
        if (ann == null) return NotFound();

        if (ann.Status == AnnouncementStatus.Pending)
        {
            TempData["Error"] = "Không thể xóa thông báo đang chờ gửi. Hãy huỷ trước.";
            return RedirectToAction(nameof(Index), "Notification");
        }

        _db.ScheduledAnnouncements.Remove(ann);
        await _db.SaveChangesAsync();

        TempData["Success"] = "Đã xóa thông báo.";
        return RedirectToAction(nameof(Index), "Notification");
    }
}
