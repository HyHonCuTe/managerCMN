using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using managerCMN.Helpers;
using managerCMN.Models.Entities;
using managerCMN.Repositories.Interfaces;
using managerCMN.Services.Interfaces;

namespace managerCMN.Controllers;

public class AccountController : Controller
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWebHostEnvironment _env;
    private readonly managerCMN.Data.ApplicationDbContext _db;
    private readonly ISystemLogService _systemLogService;
    private readonly IAuthenticationSchemeProvider _authenticationSchemeProvider;

    public AccountController(
        IUnitOfWork unitOfWork,
        IWebHostEnvironment env,
        managerCMN.Data.ApplicationDbContext db,
        ISystemLogService systemLogService,
        IAuthenticationSchemeProvider authenticationSchemeProvider)
    {
        _unitOfWork = unitOfWork;
        _env = env;
        _db = db;
        _systemLogService = systemLogService;
        _authenticationSchemeProvider = authenticationSchemeProvider;
    }

    [HttpGet]
    public async Task<IActionResult> Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        ViewData["IsDevelopment"] = _env.IsDevelopment();
        ViewData["IsGoogleLoginAvailable"] = await IsGoogleLoginAvailableAsync();

        if (_env.IsDevelopment())
        {
            ViewBag.DevEmployees = await _db.Employees
                .Include(e => e.Department)
                .OrderBy(e => e.EmployeeCode)
                .Select(e => new { e.EmployeeId, e.EmployeeCode, e.FullName, Department = e.Department != null ? e.Department.DepartmentName : "" })
                .ToListAsync();
        }

        return View();
    }

    [HttpPost]
    public async Task<IActionResult> ExternalLogin(string? returnUrl = null)
    {
        if (!await IsGoogleLoginAvailableAsync())
        {
            TempData["LoginError"] = "Đăng nhập Google chưa được cấu hình trên máy chủ. Hãy cấu hình biến môi trường trước khi sử dụng.";
            return RedirectToAction(nameof(Login), new { returnUrl });
        }

        var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account", new { returnUrl });
        var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    [HttpGet]
    public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null)
    {
        if (!await IsGoogleLoginAvailableAsync())
        {
            TempData["LoginError"] = "Đăng nhập Google hiện chưa sẵn sàng trên máy chủ.";
            return RedirectToAction(nameof(Login), new { returnUrl });
        }

        var authenticateResult = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        if (!authenticateResult.Succeeded)
            return RedirectToAction(nameof(Login));

        var email = authenticateResult.Principal?.FindFirst(ClaimTypes.Email)?.Value;
        var name = authenticateResult.Principal?.FindFirst(ClaimTypes.Name)?.Value;
        var googleId = authenticateResult.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var picture = authenticateResult.Principal?.FindFirst("picture")?.Value;

        if (string.IsNullOrEmpty(email))
            return RedirectToAction(nameof(Login));

        // Check if email exists in Employee list - RESTRICT LOGIN
        var employee = await _db.Employees.FirstOrDefaultAsync(e => e.Email == email);
        if (employee == null)
        {
            // Email not in employee list - DENY LOGIN
            await _systemLogService.LogAsync(
                null,
                "Tu choi dang nhap Google",
                "Account",
                null,
                new
                {
                    Email = email,
                    LoginType = "Google",
                    Reason = "Email khong ton tai trong danh sach nhan vien"
                },
                GetClientIP());
            TempData["LoginError"] = $"Email {email} không có trong danh sách nhân viên. Vui lòng liên hệ Admin để được cấp quyền truy cập.";
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }

        var user = await _unitOfWork.Users.GetByEmailAsync(email);
        bool createdUser = false;
        bool linkedEmployee = false;
        int? assignedRoleId = null;
        if (user == null)
        {
            // Try to find matching employee by email
            var matchedEmployee = await _db.Employees.FirstOrDefaultAsync(e => e.Email == email);

            user = new User
            {
                Email = email,
                FullName = name ?? email,
                GoogleId = googleId,
                AvatarUrl = picture,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                EmployeeId = matchedEmployee?.EmployeeId
            };
            await _unitOfWork.Users.AddAsync(user);
            await _unitOfWork.SaveChangesAsync();
            createdUser = true;

            // Assign role: Admin for the master account, User for everyone else
            int roleId = string.Equals(email, "tienthanhnguyen811@gmail.com", StringComparison.OrdinalIgnoreCase) ? 1 : 3;
            _db.Set<UserRole>().Add(new UserRole { UserId = user.UserId, RoleId = roleId });
            await _db.SaveChangesAsync();
            assignedRoleId = roleId;
        }
        else if (!user.EmployeeId.HasValue)
        {
            // Try to link existing user to employee by email
            var matchedEmployee = await _db.Employees.FirstOrDefaultAsync(e => e.Email == email);
            if (matchedEmployee != null)
            {
                user.EmployeeId = matchedEmployee.EmployeeId;
                _unitOfWork.Users.Update(user);
                await _unitOfWork.SaveChangesAsync();
                linkedEmployee = true;
            }
        }

        user.LastLogin = DateTime.UtcNow;
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();

        await _systemLogService.LogAsync(
            user.UserId,
            "Dang nhap Google thanh cong",
            "Account",
            null,
            new
            {
                user.UserId,
                user.Email,
                user.EmployeeId,
                LoginType = "Google",
                CreatedUser = createdUser,
                LinkedEmployee = linkedEmployee,
                AssignedRoleId = assignedRoleId
            },
            GetClientIP());

        await SignInUserAsync(user);
        await QueueBirthdayCelebrationAsync(user);
        return LocalRedirect(returnUrl ?? "/");
    }

    /// <summary>
    /// Dev-only login: creates/gets admin user and signs in directly without Google OAuth
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> DevLogin(string? returnUrl = null, int? employeeId = null)
    {
        if (!_env.IsDevelopment())
            return NotFound();

        User? user;
        bool createdUser = false;
        bool linkedEmployee = false;
        bool assignedAdminRole = false;
        int? effectiveEmployeeId = employeeId;

        if (employeeId.HasValue)
        {
            // Log in as a specific employee
            var emp = await _db.Employees.FindAsync(employeeId.Value);
            if (emp == null) return NotFound();

            user = await _db.Users.FirstOrDefaultAsync(u => u.EmployeeId == employeeId.Value);
            if (user == null)
            {
                user = new User
                {
                    Email = emp.Email ?? $"dev-{emp.EmployeeCode.ToLowerInvariant()}@company.local",
                    FullName = emp.FullName,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    EmployeeId = emp.EmployeeId
                };
                await _unitOfWork.Users.AddAsync(user);
                await _unitOfWork.SaveChangesAsync();
                createdUser = true;

                // Default role: User (3)
                _db.Set<UserRole>().Add(new UserRole { UserId = user.UserId, RoleId = 3 });
                await _db.SaveChangesAsync();
            }
        }
        else
        {
            // Original DevLogin: admin@company.local linked to NV001
            const string devEmail = "admin@company.local";
            user = await _unitOfWork.Users.GetByEmailAsync(devEmail);
            if (user == null)
            {
                var nv001 = await _db.Employees.FirstOrDefaultAsync(e => e.EmployeeCode == "NV001");
                user = new User
                {
                    Email = devEmail,
                    FullName = "Admin (Dev)",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    EmployeeId = nv001?.EmployeeId
                };
                await _unitOfWork.Users.AddAsync(user);
                await _unitOfWork.SaveChangesAsync();
                createdUser = true;
            }
            else if (!user.EmployeeId.HasValue)
            {
                var nv001 = await _db.Employees.FirstOrDefaultAsync(e => e.EmployeeCode == "NV001");
                if (nv001 != null)
                {
                    user.EmployeeId = nv001.EmployeeId;
                    _unitOfWork.Users.Update(user);
                    await _unitOfWork.SaveChangesAsync();
                    linkedEmployee = true;
                }
            }

            // Ensure admin role
            var userWithRoles = await _unitOfWork.Users.GetWithRolesAsync(user.UserId);
            if (userWithRoles?.UserRoles == null || !userWithRoles.UserRoles.Any(ur => ur.RoleId == 1))
            {
                _db.Set<UserRole>().Add(new UserRole { UserId = user.UserId, RoleId = 1 });
                await _db.SaveChangesAsync();
                assignedAdminRole = true;
            }

            effectiveEmployeeId = user.EmployeeId;
        }

        user.LastLogin = DateTime.UtcNow;
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();

        await _systemLogService.LogAsync(
            user.UserId,
            "Dang nhap dev thanh cong",
            "Account",
            null,
            new
            {
                user.UserId,
                user.Email,
                user.EmployeeId,
                LoginType = "Development",
                RequestedEmployeeId = employeeId,
                EffectiveEmployeeId = effectiveEmployeeId,
                CreatedUser = createdUser,
                LinkedEmployee = linkedEmployee,
                AssignedAdminRole = assignedAdminRole
            },
            GetClientIP());

        await SignInUserAsync(user);
        await QueueBirthdayCelebrationAsync(user);
        return LocalRedirect(returnUrl ?? "/");
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await _systemLogService.LogAsync(
            GetCurrentUserId(),
            "Dang xuat",
            "Account",
            null,
            new
            {
                UserId = GetCurrentUserId(),
                Email = User.FindFirstValue(ClaimTypes.Email),
                FullName = User.FindFirstValue(ClaimTypes.Name)
            },
            GetClientIP());

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    public IActionResult AccessDenied() => View();

    private async Task SignInUserAsync(User user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.FullName),
            new("AvatarUrl", user.AvatarUrl ?? "")
        };

        if (user.EmployeeId.HasValue)
        {
            claims.Add(new Claim("EmployeeId", user.EmployeeId.Value.ToString()));

            // Check if employee is an approver (IsApprover, JobTitleId=2 Trưởng phòng, or is a department manager)
            var employee = await _unitOfWork.Employees.GetByIdAsync(user.EmployeeId.Value);
            if (employee != null)
            {
                if (!string.IsNullOrWhiteSpace(employee.EmployeeCode))
                    claims.Add(new Claim("EmployeeCode", employee.EmployeeCode));

                bool isApprover = employee.IsApprover || employee.IsApprover1;
                // Check if employee is "Trưởng phòng" (JobTitleId = 2)
                if (!isApprover)
                    isApprover = employee.JobTitleId == 2;
                // Also check if they are set as ManagerId of their department
                if (!isApprover && employee.DepartmentId.HasValue)
                {
                    var dept = await _unitOfWork.Departments.GetByIdAsync(employee.DepartmentId.Value);
                    isApprover = dept?.ManagerId == employee.EmployeeId;
                }
                if (isApprover)
                    claims.Add(new Claim("IsApprover", "true"));
            }
        }

        var userWithRoles = await _unitOfWork.Users.GetWithRolesAsync(user.UserId);
        if (userWithRoles?.UserRoles != null)
        {
            foreach (var role in userWithRoles.UserRoles)
                claims.Add(new Claim(ClaimTypes.Role, role.Role.RoleName));
        }

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
    }

    private async Task QueueBirthdayCelebrationAsync(User user)
    {
        if (!user.EmployeeId.HasValue)
            return;

        var employee = await _db.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.EmployeeId == user.EmployeeId.Value);

        if (employee == null)
            return;

        var celebration = BirthdayCelebrationHelper.Build(employee, DateTimeHelper.VietnamToday);
        if (celebration == null)
            return;

        var celebrationJson = JsonSerializer.Serialize(celebration);
        if (celebrationJson.Length > 1024)
            return;

        // TempData uses cookies by default, so this payload must stay small enough for login redirect headers.
        TempData["BirthdayCelebration"] = celebrationJson;
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userIdClaim, out var id) ? id : null;
    }

    private string? GetClientIP()
        => HttpContext.Connection.RemoteIpAddress?.ToString();

    private async Task<bool> IsGoogleLoginAvailableAsync()
        => await _authenticationSchemeProvider.GetSchemeAsync(GoogleDefaults.AuthenticationScheme) != null;
}
