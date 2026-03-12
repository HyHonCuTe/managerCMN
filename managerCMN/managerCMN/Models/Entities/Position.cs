using System.ComponentModel.DataAnnotations;

namespace managerCMN.Models.Entities;

public class Position
{
    [Key]
    public int PositionId { get; set; }

    [Required, MaxLength(200)]
    public string PositionName { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}
