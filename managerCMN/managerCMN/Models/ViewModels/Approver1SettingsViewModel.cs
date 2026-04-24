using managerCMN.Models.Enums;

namespace managerCMN.Models.ViewModels;

public class DepartmentApprover1SettingsViewModel
{
    public int DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public List<Approver1EmployeeOptionViewModel> Employees { get; set; } = [];

    public int ApproverCount => Employees.Count(e => e.IsApprover1);
    public string CurrentApproverNames => ApproverCount == 0
        ? "Chưa có"
        : string.Join(", ", Employees.Where(e => e.IsApprover1).Select(e => e.FullName));
}

public class Approver1EmployeeOptionViewModel
{
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? JobTitleName { get; set; }
    public EmployeeStatus Status { get; set; }
    public bool IsApprover1 { get; set; }
}
