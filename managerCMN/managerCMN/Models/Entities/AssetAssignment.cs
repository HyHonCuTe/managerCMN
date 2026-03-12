using System.ComponentModel.DataAnnotations;
using managerCMN.Models.Enums;

namespace managerCMN.Models.Entities;

public class AssetAssignment
{
    [Key]
    public int AssignmentId { get; set; }

    public int AssetId { get; set; }
    public Asset Asset { get; set; } = null!;

    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public DateTime AssignedDate { get; set; }

    public DateTime? ReturnDate { get; set; }

    public AssetAssignmentStatus Status { get; set; } = AssetAssignmentStatus.Assigned;

    [MaxLength(500)]
    public string? Condition { get; set; }

    [MaxLength(500)]
    public string? Note { get; set; }
}
