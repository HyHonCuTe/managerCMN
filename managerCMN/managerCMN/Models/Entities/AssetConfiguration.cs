using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace managerCMN.Models.Entities;

public class AssetConfiguration
{
    [Key, ForeignKey(nameof(Asset))]
    public int AssetId { get; set; }
    public Asset Asset { get; set; } = null!;

    [MaxLength(100)]
    public string? CPU { get; set; }

    [MaxLength(100)]
    public string? Mainboard { get; set; }

    [MaxLength(100)]
    public string? RAM { get; set; }

    [MaxLength(100)]
    public string? SSD { get; set; }

    [MaxLength(100)]
    public string? HDD { get; set; }

    [MaxLength(100)]
    public string? VGA { get; set; }

    [MaxLength(100)]
    public string? OS { get; set; }
}
