using managerCMN.Models.Entities;

namespace managerCMN.Services.Interfaces;

public interface IAssetService
{
    Task<IEnumerable<Asset>> GetAllAsync();
    Task<Asset?> GetByIdAsync(int id);
    Task<Asset?> GetWithConfigurationAsync(int id);
    Task CreateAsync(Asset asset);
    Task UpdateAsync(Asset asset);
    Task DeleteAsync(int id);
    Task AssignToEmployeeAsync(AssetAssignment assignment);
    Task ReturnAssetAsync(int assignmentId, string? condition);
    Task<IEnumerable<AssetAssignment>> GetAssignmentsByEmployeeAsync(int employeeId);
    Task ImportFromExcelAsync(Stream excelStream);
}
