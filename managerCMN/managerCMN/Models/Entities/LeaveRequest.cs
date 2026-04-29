using System.ComponentModel.DataAnnotations;
using managerCMN.Models.Enums;
using managerCMN.Helpers;

namespace managerCMN.Models.Entities;

public class LeaveRequest
{
    [Key]
    public int RequestId { get; set; }

    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public LeavePayType PayType { get; set; } = LeavePayType.Paid;

    public decimal TotalDays { get; set; }

    [MaxLength(1000)]
    public string? Reason { get; set; }

    public RequestStatus Status { get; set; } = RequestStatus.Pending;

    public int? ApprovedBy { get; set; }

    public DateTime? ApprovedDate { get; set; }

    public bool IsLeaveDeducted { get; set; }

    public decimal DeductedFromCurrentYear { get; set; }

    public decimal DeductedFromCarryForward { get; set; }

    public DateTime CreatedAt { get; set; } = DateTimeHelper.VietnamNow;
}
