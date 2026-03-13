namespace managerCMN.Models.ViewModels;

public class LeaveBalanceSummaryViewModel
{
    public int EmployeeId { get; set; }

    public int Year { get; set; }

    public DateTime AsOfDate { get; set; }

    public decimal AnnualEntitlement { get; set; } = 12m;

    public int GrantedQuarters { get; set; }

    public decimal CurrentYearAllocated { get; set; }

    public decimal CurrentYearUsed { get; set; }

    public decimal CurrentYearRemaining { get; set; }

    public decimal CarryForwardRemaining { get; set; }

    public decimal TotalRemaining { get; set; }

    public decimal PaidLeaveTaken { get; set; }

    public decimal UnpaidLeaveTaken { get; set; }

    public DateTime? CarryForwardExpiryDate { get; set; }
}