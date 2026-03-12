using managerCMN.Models.Entities;

namespace managerCMN.Repositories.Interfaces;

public interface IAssetAssignmentRepository : IRepository<AssetAssignment>
{
    Task<IEnumerable<AssetAssignment>> GetByEmployeeAsync(int employeeId);
    Task<IEnumerable<AssetAssignment>> GetByAssetAsync(int assetId);
    Task<AssetAssignment?> GetCurrentAssignmentAsync(int assetId);
}
