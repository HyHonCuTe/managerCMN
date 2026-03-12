using managerCMN.Models.Entities;

namespace managerCMN.Models.ViewModels;

public class AttendanceCalendarViewModel
{
    public int Year { get; set; }
    public int Month { get; set; }
    public int? SelectedEmployeeId { get; set; }
    public string? EmployeeName { get; set; }
    public string? EmployeeCode { get; set; }

    public List<EmployeeSelectItem> Employees { get; set; } = [];
    public Dictionary<DateOnly, Attendance> AttendanceByDate { get; set; } = [];

    // Summary
    public decimal TotalWorkDays { get; set; }
    public decimal StandardWorkDays { get; set; }
    public int LateDays { get; set; }
    public int AbsentDays { get; set; }
    public decimal TotalOvertimeHours { get; set; }
    public decimal DeductionPoints { get; set; }
}

public class EmployeeSelectItem
{
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? DepartmentName { get; set; }
}
