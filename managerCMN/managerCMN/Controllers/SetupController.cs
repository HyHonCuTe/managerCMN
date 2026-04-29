using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using managerCMN.Data;
using managerCMN.Models.Entities;
using managerCMN.Services.Interfaces;
using System.Security.Claims;

namespace managerCMN.Controllers;

/// <summary>
/// Controller for initial system setup - managing admin roles
/// This should be used only for initial setup or when no admin exists
/// </summary>
public class SetupController : Controller
{
    private const string MasterAdminEmployeeCode = "A00000";

    private readonly ApplicationDbContext _db;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<SetupController> _logger;
    private readonly ISystemLogService _systemLogService;

    public SetupController(
        ApplicationDbContext db,
        IWebHostEnvironment env,
        ILogger<SetupController> logger,
        ISystemLogService systemLogService)
    {
        _db = db;
        _env = env;
        _logger = logger;
        _systemLogService = systemLogService;
    }

    private bool IsMasterAdmin()
        => User.IsInRole("Admin") && User.HasClaim("EmployeeCode", MasterAdminEmployeeCode);

    public async Task<IActionResult> Index()
    {
        if (!_env.IsDevelopment())
            return NotFound();

        // Check if admin already exists
        var adminRole = await _db.Roles.FirstOrDefaultAsync(r => r.RoleName == "Admin");
        if (adminRole == null)
        {
            ViewBag.Message = "Lỗi: Không tìm thấy role Admin trong hệ thống.";
            return View();
        }

        var adminExists = await _db.UserRoles
            .AnyAsync(ur => ur.RoleId == adminRole.RoleId);

        if (adminExists && !IsMasterAdmin())
            return Forbid();

        if (adminExists)
        {
            ViewBag.Message = "Hệ thống đã có tài khoản Admin.";
            ViewBag.AdminExists = true;
        }
        else
        {
            ViewBag.Message = "Chưa có tài khoản Admin. Vui lòng đăng nhập bằng Google trước, sau đó gán role Admin.";
            ViewBag.AdminExists = false;
        }

        // Show current users for debugging
        var users = await _db.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .ToListAsync();
        ViewBag.Users = users;

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignAdmin(int userId)
    {
        if (!_env.IsDevelopment())
            return NotFound();

        try
        {
            // Check if admin already exists
            var adminRole = await _db.Roles.FirstOrDefaultAsync(r => r.RoleName == "Admin");
            if (adminRole == null)
            {
                TempData["Error"] = "Lỗi: Không tìm thấy role Admin trong hệ thống.";
                return RedirectToAction(nameof(Index));
            }

            var adminExists = await _db.UserRoles.AnyAsync(ur => ur.RoleId == adminRole.RoleId);
            if (adminExists && !IsMasterAdmin())
            {
                TempData["Error"] = "Chỉ admin có mã nhân viên A00000 mới được cấp thêm quyền Admin.";
                return Forbid();
            }

            // Check if user exists
            var user = await _db.Users.FindAsync(userId);
            if (user == null)
            {
                TempData["Error"] = "Không tìm thấy người dùng.";
                return RedirectToAction(nameof(Index));
            }

            // Check if user already has admin role
            if (await _db.UserRoles.AnyAsync(ur => ur.UserId == userId && ur.RoleId == adminRole.RoleId))
            {
                TempData["Error"] = "Người dùng này đã là Admin.";
                return RedirectToAction(nameof(Index));
            }

            var rolesBefore = await _db.UserRoles
                .Where(ur => ur.UserId == userId)
                .Join(_db.Roles,
                    ur => ur.RoleId,
                    role => role.RoleId,
                    (ur, role) => new { ur.RoleId, RoleName = role.RoleName })
                .OrderBy(role => role.RoleId)
                .ToArrayAsync();

            // Assign admin role
            var userRole = new UserRole
            {
                UserId = userId,
                RoleId = adminRole.RoleId
            };

            _db.UserRoles.Add(userRole);
            await _db.SaveChangesAsync();

            var rolesAfter = await _db.UserRoles
                .Where(ur => ur.UserId == userId)
                .Join(_db.Roles,
                    ur => ur.RoleId,
                    role => role.RoleId,
                    (ur, role) => new { ur.RoleId, RoleName = role.RoleName })
                .OrderBy(role => role.RoleId)
                .ToArrayAsync();

            await _systemLogService.LogAsync(
                GetCurrentUserId(),
                "Cấp quyền Admin",
                "Setup",
                new
                {
                    TargetUserId = user.UserId,
                    user.Email,
                    Roles = rolesBefore
                },
                new
                {
                    TargetUserId = user.UserId,
                    user.Email,
                    AddedRoleId = adminRole.RoleId,
                    AddedRoleName = adminRole.RoleName,
                    Roles = rolesAfter
                },
                GetClientIP());

            _logger.LogInformation("Admin role assigned to user: {Email}", user.Email);
            TempData["Success"] = $"Đã gán quyền Admin cho {user.Email} thành công!";

            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning admin role");
            TempData["Error"] = "Có lỗi xảy ra khi gán quyền Admin: " + ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var id) ? id : null;
    }

    private string? GetClientIP()
        => HttpContext.Connection.RemoteIpAddress?.ToString();
}
