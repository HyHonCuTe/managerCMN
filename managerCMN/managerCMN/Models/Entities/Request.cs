using System.ComponentModel.DataAnnotations;
using managerCMN.Models.Enums;
using managerCMN.Helpers;

namespace managerCMN.Models.Entities;

public class Request
{
    [Key]
    public int RequestId { get; set; }

    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public RequestType RequestType { get; set; }

    /// <summary>Type of check in/out issue (only relevant when RequestType is CheckInOut)</summary>
    public CheckInOutType? CheckInOutType { get; set; }

    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    public LeaveReason? LeaveReason { get; set; }

    public bool IsHalfDayStart { get; set; }

    public bool IsHalfDayEnd { get; set; }

    /// <summary>true = morning half, false = afternoon half (only relevant when IsHalfDayStart is true)</summary>
    public bool IsHalfDayStartMorning { get; set; }

    /// <summary>true = morning half, false = afternoon half (only relevant when IsHalfDayEnd is true)</summary>
    public bool IsHalfDayEndMorning { get; set; }

    [MaxLength(1000)]
    public string? Reason { get; set; }

    public string? Description { get; set; }

    public bool CountsAsWork { get; set; } = true;

    public decimal TotalDays { get; set; }

    public RequestStatus Status { get; set; } = RequestStatus.Pending;

    // Deprecated - kept for backward compatibility
    public int? ApproverId { get; set; }
    public DateTime? ApprovedDate { get; set; }

    public DateTime CreatedDate { get; set; } = DateTimeHelper.VietnamNow;

    // Navigation
    public ICollection<RequestAttachment> Attachments { get; set; } = new List<RequestAttachment>();
    public ICollection<RequestApproval> Approvals { get; set; } = new List<RequestApproval>();
}
