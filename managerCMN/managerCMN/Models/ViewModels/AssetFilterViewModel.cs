using managerCMN.Models.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace managerCMN.Models.ViewModels;

public class AssetFilterViewModel
{
    public AssetStatus? Status { get; set; }
    public int? CategoryId { get; set; }
    public int? BrandId { get; set; }
    public int? EmployeeId { get; set; }
    public AssetAssignmentReason? AssignmentReason { get; set; }
    public DateTime? AssignedFromDate { get; set; }
    public DateTime? AssignedToDate { get; set; }
    public string? SearchTerm { get; set; }

    // For populating dropdowns
    public IEnumerable<SelectListItem> Categories { get; set; } = new List<SelectListItem>();
    public IEnumerable<SelectListItem> Brands { get; set; } = new List<SelectListItem>();
    public IEnumerable<SelectListItem> Employees { get; set; } = new List<SelectListItem>();
    public IEnumerable<SelectListItem> Statuses { get; set; } = new List<SelectListItem>();
    public IEnumerable<SelectListItem> AssignmentReasons { get; set; } = new List<SelectListItem>();

    public bool HasActiveFilters => Status.HasValue ||
                                   CategoryId.HasValue ||
                                   BrandId.HasValue ||
                                   EmployeeId.HasValue ||
                                   AssignmentReason.HasValue ||
                                   AssignedFromDate.HasValue ||
                                   AssignedToDate.HasValue ||
                                   !string.IsNullOrEmpty(SearchTerm);
}