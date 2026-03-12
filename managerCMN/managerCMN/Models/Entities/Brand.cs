using System.ComponentModel.DataAnnotations;

namespace managerCMN.Models.Entities;

public class Brand
{
    [Key]
    public int BrandId { get; set; }

    [Required, MaxLength(100)]
    public string BrandName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public ICollection<Asset> Assets { get; set; } = new List<Asset>();
}
