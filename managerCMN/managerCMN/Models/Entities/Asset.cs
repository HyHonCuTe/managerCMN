using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using managerCMN.Models.Enums;
using managerCMN.Helpers;

namespace managerCMN.Models.Entities;

public class Asset
{
    [Key]
    public int AssetId { get; set; }

    [Required, MaxLength(50)]
    public string AssetCode { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string AssetName { get; set; } = string.Empty;

    public int? AssetCategoryId { get; set; }
    [ForeignKey(nameof(AssetCategoryId))]
    public AssetCategory? AssetCategory { get; set; }

    public int? BrandId { get; set; }
    [ForeignKey(nameof(BrandId))]
    public Brand? Brand { get; set; }

    public int? SupplierId { get; set; }
    [ForeignKey(nameof(SupplierId))]
    public Supplier? Supplier { get; set; }

    public DateTime? PurchaseDate { get; set; }

    public decimal? PurchasePrice { get; set; }

    public AssetStatus Status { get; set; } = AssetStatus.Available;

    public DateTime CreatedAt { get; set; } = DateTimeHelper.VietnamNow;

    // Navigation
    public AssetConfiguration? Configuration { get; set; }
    public ICollection<AssetAssignment> Assignments { get; set; } = new List<AssetAssignment>();
}
