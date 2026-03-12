using managerCMN.Models.Entities;

namespace managerCMN.Repositories.Interfaces;

public interface IAssetRepository : IRepository<Asset>
{
    Task<Asset?> GetByCodeAsync(string assetCode);
    Task<Asset?> GetWithConfigurationAsync(int assetId);
    Task<IEnumerable<Asset>> GetByCategoryAsync(int categoryId);
}
