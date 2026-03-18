using System.ComponentModel.DataAnnotations;

namespace managerCMN.Models.Entities;

public class Permission
{
    public int PermissionId { get; set; }

    [Required]
    [MaxLength(100)]
    public string PermissionKey { get; set; } = string.Empty; // e.g., "Employee.View", "System.ALL"

    [Required]
    [MaxLength(200)]
    public string PermissionName { get; set; } = string.Empty; // Display name

    [Required]
    [MaxLength(50)]
    public string Category { get; set; } = string.Empty; // e.g., "Employee", "Request", "System"

    [MaxLength(500)]
    public string? Description { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;

    // Navigation properties
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
