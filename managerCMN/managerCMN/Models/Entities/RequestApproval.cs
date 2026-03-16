using System.ComponentModel.DataAnnotations;
using managerCMN.Models.Enums;

namespace managerCMN.Models.Entities;

public class RequestApproval
{
    [Key]
    public int RequestApprovalId { get; set; }

    public int RequestId { get; set; }
    public Request Request { get; set; } = null!;

    public int ApproverId { get; set; }
    public Employee Approver { get; set; } = null!;

    public int ApproverOrder { get; set; } // 1 or 2

    public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;

    public DateTime? ApprovedDate { get; set; }

    [MaxLength(500)]
    public string? Comment { get; set; }
}
