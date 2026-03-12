using System.ComponentModel.DataAnnotations;
using managerCMN.Models.Enums;

namespace managerCMN.Models.ViewModels;

public class AssetCreateViewModel
{
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

    // Configuration (optional)
    public string? CPU { get; set; }
    public string? Mainboard { get; set; }
    public string? RAM { get; set; }
    public string? SSD { get; set; }
    public string? HDD { get; set; }
    public string? VGA { get; set; }
    public string? OS { get; set; }
}
