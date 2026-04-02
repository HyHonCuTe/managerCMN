using Microsoft.EntityFrameworkCore;
using managerCMN.Data;
using managerCMN.Models.Entities;
using managerCMN.Repositories.Interfaces;
using managerCMN.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace managerCMN.Services.Implementations;

public class PermissionService : IPermissionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ApplicationDbContext _context;
    private readonly ISystemLogService _systemLogService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public PermissionService(
        IUnitOfWork unitOfWork,
        ApplicationDbContext context,
        ISystemLogService systemLogService,
        IHttpContextAccessor httpContextAccessor)
    {
        _unitOfWork = unitOfWork;
        _context = context;
        _systemLogService = systemLogService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<IEnumerable<Permission>> GetAllPermissionsAsync()
    {
        return await _unitOfWork.Permissions.GetAllAsync();
    }

    public async Task<IEnumerable<Permission>> GetPermissionsByRoleIdAsync(int roleId)
    {
        return await _unitOfWork.Permissions.GetByRoleIdAsync(roleId);
    }

    public async Task<Dictionary<string, List<Permission>>> GetAllPermissionsGroupedByCategoryAsync()
    {
        var allPermissions = await _unitOfWork.Permissions.GetAllAsync();
        return allPermissions
            .GroupBy(p => p.Category)
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    public async Task<bool> UpdateRolePermissionsAsync(int roleId, int[] permissionIds)
    {
        try
        {
            permissionIds ??= [];
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.RoleId == roleId);

            // Remove existing role permissions
            var existingRolePermissions = await _context.RolePermissions
                .Where(rp => rp.RoleId == roleId)
                .ToListAsync();

            var dataBefore = new
            {
                RoleId = roleId,
                RoleName = role?.RoleName,
                PermissionIds = existingRolePermissions
                    .Select(rp => rp.PermissionId)
                    .OrderBy(id => id)
                    .ToArray()
            };

            _context.RolePermissions.RemoveRange(existingRolePermissions);

            // Add new role permissions
            var newRolePermissions = permissionIds.Select(permId => new RolePermission
            {
                RoleId = roleId,
                PermissionId = permId,
                AssignedDate = DateTime.UtcNow
            }).ToList();

            await _context.RolePermissions.AddRangeAsync(newRolePermissions);
            await _context.SaveChangesAsync();

            await _systemLogService.LogAsync(
                GetCurrentUserId(),
                "Cap nhat quyen cho vai tro",
                "RolePermission",
                dataBefore,
                new
                {
                    RoleId = roleId,
                    RoleName = role?.RoleName,
                    PermissionIds = permissionIds.OrderBy(id => id).ToArray()
                },
                GetClientIP());

            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> UserHasPermissionAsync(int userId, string permissionKey)
    {
        var userPermissions = await GetUserPermissionKeysAsync(userId);

        // Check for System.ALL super permission
        if (userPermissions.Contains("System.ALL"))
            return true;

        return userPermissions.Contains(permissionKey);
    }

    public async Task<IEnumerable<string>> GetUserPermissionKeysAsync(int userId)
    {
        var permissionKeys = await _context.UserRoles
            .Where(ur => ur.UserId == userId)
            .Include(ur => ur.Role)
            .ThenInclude(r => r.RolePermissions)
            .ThenInclude(rp => rp.Permission)
            .SelectMany(ur => ur.Role.RolePermissions.Select(rp => rp.Permission.PermissionKey))
            .Distinct()
            .ToListAsync();

        return permissionKeys;
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var id) ? id : null;
    }

    private string? GetClientIP()
        => _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
}
