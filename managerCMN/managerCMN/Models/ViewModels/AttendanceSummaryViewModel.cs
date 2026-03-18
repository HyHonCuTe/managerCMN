namespace managerCMN.Models.ViewModels;

public class AttendanceSummaryViewModel
{
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public decimal TotalCong { get; set; }
    public decimal DonCoPhep { get; set; }
    public decimal DonKhongPhep { get; set; }
    public int LateDays { get; set; }
}
