using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using managerCMN.Data;
using managerCMN.Models.Entities;

namespace managerCMN.Controllers;

/// <summary>
/// Controller for initial system setup - managing admin roles
/// This should be used only for initial setup or when no admin exists
/// </summary>
public class SetupController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<SetupController> _logger;

    public SetupController(ApplicationDbContext db, ILogger<SetupController> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        // Check if admin already exists
        var adminRole = await _db.Roles.FirstOrDefaultAsync(r => r.RoleName == "Admin");
        if (adminRole == null)
        {
            ViewBag.Message = "Lỗi: Không tìm thấy role Admin trong hệ thống.";
            return View();
        }

        var adminExists = await _db.UserRoles
            .AnyAsync(ur => ur.RoleId == adminRole.RoleId);

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
        try
        {
            // Check if admin already exists
            var adminRole = await _db.Roles.FirstOrDefaultAsync(r => r.RoleName == "Admin");
            if (adminRole == null)
            {
                TempData["Error"] = "Lỗi: Không tìm thấy role Admin trong hệ thống.";
                return RedirectToAction(nameof(Index));
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

            // Assign admin role
            var userRole = new UserRole
            {
                UserId = userId,
                RoleId = adminRole.RoleId
            };

            _db.UserRoles.Add(userRole);
            await _db.SaveChangesAsync();

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
}