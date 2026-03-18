using managerCMN.Models.Entities;

namespace managerCMN.Models.ViewModels;

public class DashboardViewModel
{
    // ---- Admin / Manager stats ----
    public int TotalEmployees { get; set; }
    public int PendingRequests { get; set; }
    public int ActiveTickets { get; set; }
    public int TotalAssets { get; set; }
    public IEnumerable<Contract> ExpiringContracts { get; set; } = [];

    // ---- Personal user data (null when viewer is admin/manager) ----
    public PersonalDashboardData? Personal { get; set; }
}

public class PersonalDashboardData
{
    public string FullName { get; set; } = "";
    public string? PositionName { get; set; }
    public string? DepartmentName { get; set; }

    // Contract
    public Contract? CurrentContract { get; set; }

    // Leave
    public LeaveBalanceSummaryViewModel? LeaveSummary { get; set; }

    // Attendance (current month)
    public int AttendanceDaysWorked { get; set; }
    public int AttendanceLateCount { get; set; }
    public decimal AttendanceOvertimeHours { get; set; }

    // Pending requests
    public int MyPendingRequests { get; set; }

    // Assets
    public int AssignedAssetsCount { get; set; }
}
