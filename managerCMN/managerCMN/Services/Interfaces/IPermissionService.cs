using managerCMN.Models.Entities;

namespace managerCMN.Services.Interfaces;

public interface IPermissionService
{
    Task<IEnumerable<Permission>> GetAllPermissionsAsync();
    Task<IEnumerable<Permission>> GetPermissionsByRoleIdAsync(int roleId);
    Task<Dictionary<string, List<Permission>>> GetAllPermissionsGroupedByCategoryAsync();
    Task<bool> UpdateRolePermissionsAsync(int roleId, int[] permissionIds);
    Task<bool> UserHasPermissionAsync(int userId, string permissionKey);
    Task<IEnumerable<string>> GetUserPermissionKeysAsync(int userId);
}
