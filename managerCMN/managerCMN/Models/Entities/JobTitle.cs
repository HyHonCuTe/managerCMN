using System.ComponentModel.DataAnnotations;

namespace managerCMN.Models.Entities;

public class JobTitle
{
    [Key]
    public int JobTitleId { get; set; }

    [Required, MaxLength(100)]
    public string JobTitleName { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}
