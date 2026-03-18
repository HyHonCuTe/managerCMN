using Microsoft.EntityFrameworkCore;
using managerCMN.Data;
using managerCMN.Models.Entities;
using managerCMN.Repositories.Interfaces;

namespace managerCMN.Repositories.Implementations;

public class PermissionRepository : Repository<Permission>, IPermissionRepository
{
    public PermissionRepository(ApplicationDbContext context) : base(context) { }

    public new async Task<IEnumerable<Permission>> GetAllAsync()
        => await _dbSet
            .OrderBy(p => p.Category)
            .ThenBy(p => p.SortOrder)
            .ToListAsync();

    public async Task<IEnumerable<Permission>> GetByRoleIdAsync(int roleId)
        => await _dbSet
            .Where(p => p.RolePermissions.Any(rp => rp.RoleId == roleId))
            .OrderBy(p => p.Category)
            .ThenBy(p => p.SortOrder)
            .ToListAsync();

    public new async Task<Permission?> GetByIdAsync(int id)
        => await _dbSet
            .Include(p => p.RolePermissions)
            .FirstOrDefaultAsync(p => p.PermissionId == id);

    public async Task<Permission?> GetByKeyAsync(string permissionKey)
        => await _dbSet
            .FirstOrDefaultAsync(p => p.PermissionKey == permissionKey);

    public async Task<IEnumerable<Permission>> GetByCategoryAsync(string category)
        => await _dbSet
            .Where(p => p.Category == category)
            .OrderBy(p => p.SortOrder)
            .ToListAsync();

    public new async Task<bool> AddAsync(Permission permission)
    {
        await _dbSet.AddAsync(permission);
        return true;
    }

    public async Task<bool> UpdateAsync(Permission permission)
    {
        _dbSet.Update(permission);
        return await Task.FromResult(true);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var permission = await GetByIdAsync(id);
        if (permission == null) return false;

        _dbSet.Remove(permission);
        return true;
    }
}
