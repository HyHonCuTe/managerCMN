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

    // Enhanced assignment tracking
    public AssetAssignmentReason AssignmentReason { get; set; }
    public AssetReturnReason? ReturnReason { get; set; }

    // Condition tracking - separate for assignment vs return
    [MaxLength(500)]
    public string? AssignmentCondition { get; set; } // Condition when assigned

    [MaxLength(500)]
    public string? ReturnCondition { get; set; } // Condition when returned

    [MaxLength(500)]
    public string? Condition { get; set; } // Keep for backward compatibility

    [MaxLength(500)]
    public string? Note { get; set; }

    // Approval workflow
    public int? ApprovedById { get; set; }
    public Employee? ApprovedBy { get; set; }
    public DateTime? ApprovedDate { get; set; }
}
