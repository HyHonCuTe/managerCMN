using System.ComponentModel.DataAnnotations;
using managerCMN.Helpers;

namespace managerCMN.Models.Entities;

public class LeaveBalance
{
    [Key]
    public int LeaveId { get; set; }

    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public int Year { get; set; }

    public decimal TotalLeave { get; set; }

    public decimal UsedLeave { get; set; }

    public decimal RemainingLeave { get; set; }

    public decimal CarryForward { get; set; }

    public bool IsManuallyAdjusted { get; set; }

    public DateTime LastUpdated { get; set; } = DateTimeHelper.VietnamNow;
}
