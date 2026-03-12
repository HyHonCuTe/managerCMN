using Microsoft.EntityFrameworkCore;
using managerCMN.Data;
using managerCMN.Models.Entities;
using managerCMN.Repositories.Interfaces;

namespace managerCMN.Repositories.Implementations;

public class AssetRepository : Repository<Asset>, IAssetRepository
{
    public AssetRepository(ApplicationDbContext context) : base(context) { }

    public async Task<Asset?> GetByCodeAsync(string assetCode)
        => await _dbSet.FirstOrDefaultAsync(a => a.AssetCode == assetCode);

    public async Task<Asset?> GetWithConfigurationAsync(int assetId)
        => await _dbSet
            .Include(a => a.Configuration)
            .Include(a => a.Assignments).ThenInclude(aa => aa.Employee)
            .FirstOrDefaultAsync(a => a.AssetId == assetId);

    public async Task<IEnumerable<Asset>> GetByCategoryAsync(string category)
        => await _dbSet.Where(a => a.Category == category).ToListAsync();
}
