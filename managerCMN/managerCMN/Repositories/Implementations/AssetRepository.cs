using Microsoft.EntityFrameworkCore;
using managerCMN.Data;
using managerCMN.Models.Entities;
using managerCMN.Repositories.Interfaces;

namespace managerCMN.Repositories.Implementations;

public class AssetRepository : Repository<Asset>, IAssetRepository
{
    public AssetRepository(ApplicationDbContext context) : base(context) { }

    public new async Task<IEnumerable<Asset>> GetAllAsync()
        => await _dbSet
            .Include(a => a.AssetCategory)
            .Include(a => a.Brand)
            .Include(a => a.Supplier)
            .ToListAsync();

    public async Task<Asset?> GetByCodeAsync(string assetCode)
        => await _dbSet.FirstOrDefaultAsync(a => a.AssetCode == assetCode);

    public async Task<Asset?> GetWithConfigurationAsync(int assetId)
        => await _dbSet
            .Include(a => a.Configuration)
            .Include(a => a.AssetCategory)
            .Include(a => a.Brand)
            .Include(a => a.Supplier)
            .Include(a => a.Assignments).ThenInclude(aa => aa.Employee)
            .FirstOrDefaultAsync(a => a.AssetId == assetId);

    public async Task<IEnumerable<Asset>> GetByCategoryAsync(int categoryId)
        => await _dbSet.Where(a => a.AssetCategoryId == categoryId).ToListAsync();
}
