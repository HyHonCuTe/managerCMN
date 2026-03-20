using managerCMN.Models.Entities;
using managerCMN.Models.ViewModels;
using managerCMN.Models.Enums;

namespace managerCMN.Services.Interfaces;

public interface IAssetService
{
    Task<IEnumerable<Asset>> GetAllAsync();
    Task<Asset?> GetByIdAsync(int id);
    Task<Asset?> GetWithConfigurationAsync(int id);
    Task CreateAsync(Asset asset);
    Task UpdateAsync(Asset asset);
    Task DeleteAsync(int id);

    // Enhanced assignment/return with reasons
    Task AssignToEmployeeAsync(AssetAssignment assignment);
    Task AssignToEmployeeAsync(AssetAssignment assignment, AssetAssignmentReason reason, string? condition = null);
    Task ReturnAssetAsync(int assignmentId, string? condition);
    Task ReturnAssetAsync(int assignmentId, DateTime returnDate, AssetReturnReason reason, string? condition = null);

    // Employee and filtering
    Task<IEnumerable<AssetAssignment>> GetAssignmentsByEmployeeAsync(int employeeId);
    Task<IEnumerable<Asset>> GetMyAssetsAsync(int employeeId);
    Task<IEnumerable<Asset>> GetFilteredAsync(AssetFilterViewModel filter);

    // Lifecycle tracking
    Task<AssetLifecycleHistory> LogLifecycleEventAsync(int assetId, AssetLifecycleEventType eventType,
        int? employeeId = null, int? performedById = null, string? description = null,
        string? previousValue = null, string? newValue = null, string? notes = null);

    Task<IEnumerable<AssetLifecycleHistory>> GetLifecycleHistoryAsync(int assetId);

    // Maintenance tracking
    Task UpdateAssetConditionAsync(int assetId, string condition, string? notes = null);
    Task MoveAssetLocationAsync(int assetId, string newLocation, string? notes = null);

    Task ImportFromExcelAsync(Stream excelStream);
}
