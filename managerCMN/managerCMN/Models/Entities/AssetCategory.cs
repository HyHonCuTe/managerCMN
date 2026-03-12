using System.ComponentModel.DataAnnotations;

namespace managerCMN.Models.Entities;

public class AssetCategory
{
    [Key]
    public int AssetCategoryId { get; set; }

    [Required, MaxLength(100)]
    public string CategoryName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public ICollection<Asset> Assets { get; set; } = new List<Asset>();
}
