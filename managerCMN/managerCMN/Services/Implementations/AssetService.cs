using ClosedXML.Excel;
using managerCMN.Data;
using managerCMN.Models.Entities;
using managerCMN.Models.Enums;
using managerCMN.Repositories.Interfaces;
using managerCMN.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace managerCMN.Services.Implementations;

public class AssetService : IAssetService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ApplicationDbContext _db;

    public AssetService(IUnitOfWork unitOfWork, ApplicationDbContext db)
    {
        _unitOfWork = unitOfWork;
        _db = db;
    }

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
                PurchaseDate = row.Cell(6).IsEmpty() ? null : row.Cell(6).GetDateTime(),
                Status = AssetStatus.Available
            };

            if (!row.Cell(3).IsEmpty())
            {
                var catName = row.Cell(3).GetString().Trim();
                var cat = await _db.AssetCategories.FirstOrDefaultAsync(c => c.CategoryName == catName);
                if (cat != null) asset.AssetCategoryId = cat.AssetCategoryId;
            }
            if (!row.Cell(4).IsEmpty())
            {
                var brandName = row.Cell(4).GetString().Trim();
                var brand = await _db.Brands.FirstOrDefaultAsync(b => b.BrandName == brandName);
                if (brand != null) asset.BrandId = brand.BrandId;
            }
            if (!row.Cell(5).IsEmpty())
            {
                var supName = row.Cell(5).GetString().Trim();
                var sup = await _db.Suppliers.FirstOrDefaultAsync(s => s.SupplierName == supName);
                if (sup != null) asset.SupplierId = sup.SupplierId;
            }

            await _unitOfWork.Assets.AddAsync(asset);
        }

        await _unitOfWork.SaveChangesAsync();
    }
}
