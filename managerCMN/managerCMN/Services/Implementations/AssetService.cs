using ClosedXML.Excel;
using managerCMN.Models.Entities;
using managerCMN.Models.Enums;
using managerCMN.Repositories.Interfaces;
using managerCMN.Services.Interfaces;

namespace managerCMN.Services.Implementations;

public class AssetService : IAssetService
{
    private readonly IUnitOfWork _unitOfWork;

    public AssetService(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    public async Task<IEnumerable<Asset>> GetAllAsync()
        => await _unitOfWork.Assets.GetAllAsync();

    public async Task<Asset?> GetByIdAsync(int id)
        => await _unitOfWork.Assets.GetByIdAsync(id);

    public async Task<Asset?> GetWithConfigurationAsync(int id)
        => await _unitOfWork.Assets.GetWithConfigurationAsync(id);

    public async Task CreateAsync(Asset asset)
    {
        await _unitOfWork.Assets.AddAsync(asset);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task UpdateAsync(Asset asset)
    {
        _unitOfWork.Assets.Update(asset);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var asset = await _unitOfWork.Assets.GetByIdAsync(id);
        if (asset != null)
        {
            _unitOfWork.Assets.Remove(asset);
            await _unitOfWork.SaveChangesAsync();
        }
    }

    public async Task AssignToEmployeeAsync(AssetAssignment assignment)
    {
        var asset = await _unitOfWork.Assets.GetByIdAsync(assignment.AssetId);
        if (asset != null)
        {
            asset.Status = AssetStatus.Assigned;
            _unitOfWork.Assets.Update(asset);
        }

        await _unitOfWork.AssetAssignments.AddAsync(assignment);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task ReturnAssetAsync(int assignmentId, string? condition)
    {
        var assignment = await _unitOfWork.AssetAssignments.GetByIdAsync(assignmentId);
        if (assignment == null) return;

        assignment.Status = AssetAssignmentStatus.Returned;
        assignment.ReturnDate = DateTime.UtcNow;
        assignment.Condition = condition;
        _unitOfWork.AssetAssignments.Update(assignment);

        var asset = await _unitOfWork.Assets.GetByIdAsync(assignment.AssetId);
        if (asset != null)
        {
            asset.Status = AssetStatus.Available;
            _unitOfWork.Assets.Update(asset);
        }

        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<IEnumerable<AssetAssignment>> GetAssignmentsByEmployeeAsync(int employeeId)
        => await _unitOfWork.AssetAssignments.GetByEmployeeAsync(employeeId);

    public async Task ImportFromExcelAsync(Stream excelStream)
    {
        using var workbook = new XLWorkbook(excelStream);
        var worksheet = workbook.Worksheets.First();
        var rows = worksheet.RowsUsed().Skip(1);

        foreach (var row in rows)
        {
            var assetCode = row.Cell(1).GetString().Trim();
            var existing = await _unitOfWork.Assets.GetByCodeAsync(assetCode);
            if (existing != null) continue;

            var asset = new Asset
            {
                AssetCode = assetCode,
                AssetName = row.Cell(2).GetString(),
                Category = row.Cell(3).IsEmpty() ? null : row.Cell(3).GetString(),
                Brand = row.Cell(4).IsEmpty() ? null : row.Cell(4).GetString(),
                Supplier = row.Cell(5).IsEmpty() ? null : row.Cell(5).GetString(),
                PurchaseDate = row.Cell(6).IsEmpty() ? null : row.Cell(6).GetDateTime(),
                Status = AssetStatus.Available
            };

            await _unitOfWork.Assets.AddAsync(asset);
        }

        await _unitOfWork.SaveChangesAsync();
    }
}
