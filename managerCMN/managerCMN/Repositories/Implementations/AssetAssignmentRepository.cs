using Microsoft.EntityFrameworkCore;
using managerCMN.Data;
using managerCMN.Models.Entities;
using managerCMN.Models.Enums;
using managerCMN.Repositories.Interfaces;

namespace managerCMN.Repositories.Implementations;

public class AssetAssignmentRepository : Repository<AssetAssignment>, IAssetAssignmentRepository
{
    public AssetAssignmentRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IEnumerable<AssetAssignment>> GetByEmployeeAsync(int employeeId)
        => await _dbSet
            .Include(aa => aa.Asset)
            .Where(aa => aa.EmployeeId == employeeId)
            .ToListAsync();

    public async Task<IEnumerable<AssetAssignment>> GetByAssetAsync(int assetId)
        => await _dbSet
            .Include(aa => aa.Employee)
            .Where(aa => aa.AssetId == assetId)
            .OrderByDescending(aa => aa.AssignedDate)
            .ToListAsync();

    public async Task<AssetAssignment?> GetCurrentAssignmentAsync(int assetId)
        => await _dbSet
            .Include(aa => aa.Employee)
            .FirstOrDefaultAsync(aa => aa.AssetId == assetId
                && aa.Status == AssetAssignmentStatus.Assigned);
}
