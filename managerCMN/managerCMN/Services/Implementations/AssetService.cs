using ClosedXML.Excel;
using managerCMN.Data;
using managerCMN.Models.Entities;
using managerCMN.Models.Enums;
using managerCMN.Models.ViewModels;
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
            // Get and validate required fields
            var assetCode = row.Cell(1).GetString().Trim();
            var assetName = row.Cell(2).GetString().Trim();

            // Skip if required fields are empty
            if (string.IsNullOrWhiteSpace(assetCode) || string.IsNullOrWhiteSpace(assetName))
                continue;

            // Check if asset code already exists
            var existing = await _unitOfWork.Assets.GetByCodeAsync(assetCode);
            if (existing != null) continue;

            var asset = new Asset
            {
                AssetCode = assetCode,
                AssetName = assetName,
                Status = AssetStatus.Available
            };

            // Category (optional) - Column 3
            if (!row.Cell(3).IsEmpty())
            {
                var catName = row.Cell(3).GetString().Trim();
                if (!string.IsNullOrWhiteSpace(catName))
                {
                    var cat = await _db.AssetCategories.FirstOrDefaultAsync(c => c.CategoryName == catName);
                    if (cat != null) asset.AssetCategoryId = cat.AssetCategoryId;
                }
            }

            // Brand (optional) - Column 4
            if (!row.Cell(4).IsEmpty())
            {
                var brandName = row.Cell(4).GetString().Trim();
                if (!string.IsNullOrWhiteSpace(brandName))
                {
                    var brand = await _db.Brands.FirstOrDefaultAsync(b => b.BrandName == brandName);
                    if (brand != null) asset.BrandId = brand.BrandId;
                }
            }

            // Supplier (optional) - Column 5
            if (!row.Cell(5).IsEmpty())
            {
                var supName = row.Cell(5).GetString().Trim();
                if (!string.IsNullOrWhiteSpace(supName))
                {
                    var sup = await _db.Suppliers.FirstOrDefaultAsync(s => s.SupplierName == supName);
                    if (sup != null) asset.SupplierId = sup.SupplierId;
                }
            }

            // Purchase Date (optional) - Column 6
            if (!row.Cell(6).IsEmpty())
            {
                try
                {
                    // Try to get as DateTime first
                    asset.PurchaseDate = row.Cell(6).GetDateTime();
                }
                catch
                {
                    // If that fails, try to parse as string
                    var dateStr = row.Cell(6).GetString().Trim();
                    if (DateTime.TryParse(dateStr, out var parsedDate))
                    {
                        asset.PurchaseDate = parsedDate;
                    }
                }
            }

            // Purchase Price (optional) - Column 7
            if (!row.Cell(7).IsEmpty())
            {
                try
                {
                    // Try to get as double first
                    asset.PurchasePrice = Convert.ToDecimal(row.Cell(7).GetDouble());
                }
                catch
                {
                    // If that fails, try to parse as string
                    try
                    {
                        var priceStr = row.Cell(7).GetString().Trim();
                        if (decimal.TryParse(priceStr, out var parsedPrice))
                        {
                            asset.PurchasePrice = parsedPrice;
                        }
                    }
                    catch
                    {
                        // If parsing fails, leave as null
                    }
                }
            }

            await _unitOfWork.Assets.AddAsync(asset);
        }

        await _unitOfWork.SaveChangesAsync();
    }

    // Enhanced assignment with reason
    public async Task AssignToEmployeeAsync(AssetAssignment assignment, AssetAssignmentReason reason, string? condition = null)
    {
        assignment.AssignmentReason = reason;
        assignment.AssignmentCondition = condition;

        var asset = await _unitOfWork.Assets.GetByIdAsync(assignment.AssetId);
        if (asset != null)
        {
            asset.Status = AssetStatus.Assigned;
            _unitOfWork.Assets.Update(asset);
        }

        await _unitOfWork.AssetAssignments.AddAsync(assignment);

        // Log lifecycle event
        await LogLifecycleEventAsync(assignment.AssetId, AssetLifecycleEventType.Assigned,
            assignment.EmployeeId, null,
            $"Asset assigned to employee with reason: {reason}",
            "Available", "Assigned", assignment.Note);

        await _unitOfWork.SaveChangesAsync();
    }

    // Enhanced return with reason and manual return date
    public async Task ReturnAssetAsync(int assignmentId, DateTime returnDate, AssetReturnReason reason, string? condition = null)
    {
        var assignment = await _unitOfWork.AssetAssignments.GetByIdAsync(assignmentId);
        if (assignment == null) return;

        assignment.Status = AssetAssignmentStatus.Returned;
        assignment.ReturnDate = returnDate; // Use manual return date from form
        assignment.ReturnReason = reason;
        assignment.ReturnCondition = condition;
        _unitOfWork.AssetAssignments.Update(assignment);

        var asset = await _unitOfWork.Assets.GetByIdAsync(assignment.AssetId);
        if (asset != null)
        {
            asset.Status = AssetStatus.Available;
            _unitOfWork.Assets.Update(asset);
        }

        // Log lifecycle event
        await LogLifecycleEventAsync(assignment.AssetId, AssetLifecycleEventType.Returned,
            assignment.EmployeeId, null,
            $"Asset returned with reason: {reason}",
            "Assigned", "Available", condition);

        await _unitOfWork.SaveChangesAsync();
    }

    // Get assets assigned to employee
    public async Task<IEnumerable<Asset>> GetMyAssetsAsync(int employeeId)
    {
        return await _db.Assets
            .Include(a => a.AssetCategory)
            .Include(a => a.Brand)
            .Include(a => a.Assignments)
            .Where(a => a.Assignments.Any(aa =>
                aa.EmployeeId == employeeId &&
                aa.Status == AssetAssignmentStatus.Assigned))
            .ToListAsync();
    }

    // Filtered search
    public async Task<IEnumerable<Asset>> GetFilteredAsync(AssetFilterViewModel filter)
    {
        var query = _db.Assets
            .Include(a => a.AssetCategory)
            .Include(a => a.Brand)
            .Include(a => a.Supplier)
            .Include(a => a.Assignments)
                .ThenInclude(aa => aa.Employee)
            .AsQueryable();

        if (filter.Status.HasValue)
            query = query.Where(a => a.Status == filter.Status.Value);

        if (filter.CategoryId.HasValue)
            query = query.Where(a => a.AssetCategoryId == filter.CategoryId.Value);

        if (filter.BrandId.HasValue)
            query = query.Where(a => a.BrandId == filter.BrandId.Value);

        if (filter.EmployeeId.HasValue)
            query = query.Where(a => a.Assignments.Any(aa =>
                aa.EmployeeId == filter.EmployeeId.Value &&
                aa.Status == AssetAssignmentStatus.Assigned));

        if (filter.AssignmentReason.HasValue)
            query = query.Where(a => a.Assignments.Any(aa =>
                aa.AssignmentReason == filter.AssignmentReason.Value));

        if (filter.AssignedFromDate.HasValue)
            query = query.Where(a => a.Assignments.Any(aa =>
                aa.AssignedDate >= filter.AssignedFromDate.Value));

        if (filter.AssignedToDate.HasValue)
            query = query.Where(a => a.Assignments.Any(aa =>
                aa.AssignedDate <= filter.AssignedToDate.Value));

        if (!string.IsNullOrEmpty(filter.SearchTerm))
        {
            var searchTerm = filter.SearchTerm.ToLower();
            query = query.Where(a =>
                a.AssetName.ToLower().Contains(searchTerm) ||
                a.AssetCode.ToLower().Contains(searchTerm) ||
                (a.AssetCategory != null && a.AssetCategory.CategoryName.ToLower().Contains(searchTerm)) ||
                (a.Brand != null && a.Brand.BrandName.ToLower().Contains(searchTerm)));
        }

        return await query.ToListAsync();
    }

    // Lifecycle event logging
    public async Task<AssetLifecycleHistory> LogLifecycleEventAsync(int assetId, AssetLifecycleEventType eventType,
        int? employeeId = null, int? performedById = null, string? description = null,
        string? previousValue = null, string? newValue = null, string? notes = null)
    {
        var lifecycleEvent = new AssetLifecycleHistory
        {
            AssetId = assetId,
            EventType = eventType,
            EventDate = DateTime.UtcNow,
            EmployeeId = employeeId,
            PerformedById = performedById,
            EventDescription = description,
            PreviousValue = previousValue,
            NewValue = newValue,
            Notes = notes
        };

        await _db.AssetLifecycleHistories.AddAsync(lifecycleEvent);
        return lifecycleEvent;
    }

    // Get lifecycle history
    public async Task<IEnumerable<AssetLifecycleHistory>> GetLifecycleHistoryAsync(int assetId)
    {
        return await _db.AssetLifecycleHistories
            .Include(alh => alh.Employee)
            .Include(alh => alh.PerformedBy)
            .Where(alh => alh.AssetId == assetId)
            .OrderByDescending(alh => alh.EventDate)
            .ToListAsync();
    }

    // Update asset condition
    public async Task UpdateAssetConditionAsync(int assetId, string condition, string? notes = null)
    {
        var asset = await _unitOfWork.Assets.GetByIdAsync(assetId);
        if (asset == null) return;

        // Log condition update
        await LogLifecycleEventAsync(assetId, AssetLifecycleEventType.ConditionUpdated,
            null, null, "Asset condition updated", null, condition, notes);

        await _unitOfWork.SaveChangesAsync();
    }

    // Move asset location
    public async Task MoveAssetLocationAsync(int assetId, string newLocation, string? notes = null)
    {
        // Log location move
        await LogLifecycleEventAsync(assetId, AssetLifecycleEventType.Moved,
            null, null, "Asset location changed", null, newLocation, notes);

        await _unitOfWork.SaveChangesAsync();
    }
}
