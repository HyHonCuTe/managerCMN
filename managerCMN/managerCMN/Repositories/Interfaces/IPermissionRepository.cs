using managerCMN.Models.Entities;

namespace managerCMN.Repositories.Interfaces;

public interface IPermissionRepository
{
    Task<IEnumerable<Permission>> GetAllAsync();
    Task<IEnumerable<Permission>> GetByRoleIdAsync(int roleId);
    Task<Permission?> GetByIdAsync(int id);
    Task<Permission?> GetByKeyAsync(string permissionKey);
    Task<IEnumerable<Permission>> GetByCategoryAsync(string category);
    Task<bool> AddAsync(Permission permission);
    Task<bool> UpdateAsync(Permission permission);
    Task<bool> DeleteAsync(int id);
}
