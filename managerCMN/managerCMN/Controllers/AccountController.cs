using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using managerCMN.Models.Entities;
using managerCMN.Repositories.Interfaces;

namespace managerCMN.Controllers;

public class AccountController : Controller
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWebHostEnvironment _env;
    private readonly managerCMN.Data.ApplicationDbContext _db;

    public AccountController(IUnitOfWork unitOfWork, IWebHostEnvironment env, managerCMN.Data.ApplicationDbContext db)
    {
        _unitOfWork = unitOfWork;
        _env = env;
        _db = db;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        ViewData["IsDevelopment"] = _env.IsDevelopment();
        return View();
    }

    [HttpPost]
    public IActionResult ExternalLogin(string? returnUrl = null)
    {
        var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account", new { returnUrl });
        var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    [HttpGet]
    public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null)
    {
        var authenticateResult = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        if (!authenticateResult.Succeeded)
            return RedirectToAction(nameof(Login));

        var email = authenticateResult.Principal?.FindFirst(ClaimTypes.Email)?.Value;
        var name = authenticateResult.Principal?.FindFirst(ClaimTypes.Name)?.Value;
        var googleId = authenticateResult.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var picture = authenticateResult.Principal?.FindFirst("picture")?.Value;

        if (string.IsNullOrEmpty(email))
            return RedirectToAction(nameof(Login));

        var user = await _unitOfWork.Users.GetByEmailAsync(email);
        if (user == null)
        {
            user = new User
            {
                Email = email,
                FullName = name ?? email,
                GoogleId = googleId,
                AvatarUrl = picture,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            await _unitOfWork.Users.AddAsync(user);
            await _unitOfWork.SaveChangesAsync();
        }

        user.LastLogin = DateTime.UtcNow;
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();

        await SignInUserAsync(user);
        return LocalRedirect(returnUrl ?? "/");
    }

    /// <summary>
    /// Dev-only login: creates/gets admin user and signs in directly without Google OAuth
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> DevLogin(string? returnUrl = null)
    {
        if (!_env.IsDevelopment())
            return NotFound();

        const string devEmail = "admin@company.local";
        var user = await _unitOfWork.Users.GetByEmailAsync(devEmail);
        if (user == null)
        {
            user = new User
            {
                Email = devEmail,
                FullName = "Admin (Dev)",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            await _unitOfWork.Users.AddAsync(user);
            await _unitOfWork.SaveChangesAsync();
        }

        // Ensure admin role
        var userWithRoles = await _unitOfWork.Users.GetWithRolesAsync(user.UserId);
        if (userWithRoles?.UserRoles == null || !userWithRoles.UserRoles.Any(ur => ur.RoleId == 1))
        {
            _db.Set<UserRole>().Add(new UserRole { UserId = user.UserId, RoleId = 1 });
            await _db.SaveChangesAsync();
        }

        user.LastLogin = DateTime.UtcNow;
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();

        await SignInUserAsync(user);
        return LocalRedirect(returnUrl ?? "/");
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
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
            claims.Add(new Claim("EmployeeId", user.EmployeeId.Value.ToString()));

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
}
