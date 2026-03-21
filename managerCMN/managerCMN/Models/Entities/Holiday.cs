using System.ComponentModel.DataAnnotations;

namespace managerCMN.Models.Entities;

public class Holiday
{
    [Key]
    public int HolidayId { get; set; }

    [Required]
    public DateOnly Date { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// true = recurring annually (e.g., Tet, Independence Day)
    /// false = one-time (e.g., sudden company off-day)
    /// </summary>
    public bool IsRecurring { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}