using System.ComponentModel.DataAnnotations;
using managerCMN.Models.Enums;

namespace managerCMN.Models.Entities;

public class Asset
{
    [Key]
    public int AssetId { get; set; }

    [Required, MaxLength(50)]
    public string AssetCode { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string AssetName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Category { get; set; }

    [MaxLength(100)]
    public string? Brand { get; set; }

    [MaxLength(200)]
    public string? Supplier { get; set; }

    public DateTime? PurchaseDate { get; set; }

    public decimal? PurchasePrice { get; set; }

    public AssetStatus Status { get; set; } = AssetStatus.Available;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public AssetConfiguration? Configuration { get; set; }
    public ICollection<AssetAssignment> Assignments { get; set; } = new List<AssetAssignment>();
}
