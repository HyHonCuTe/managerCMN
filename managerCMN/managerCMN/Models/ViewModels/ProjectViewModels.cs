using System.ComponentModel.DataAnnotations;
using managerCMN.Models.Enums;

namespace managerCMN.Models.ViewModels;

public class ProjectListViewModel
{
    public int ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ProjectStatus Status { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public decimal Progress { get; set; }
    public int MemberCount { get; set; }
    public int TaskCount { get; set; }
    public int OverdueTaskCount { get; set; }
    public ProjectMemberRole MyRole { get; set; }
    public bool IsSystemAdmin { get; set; }
    public string OwnerName { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public bool IsArchived { get; set; }
}

public class ProjectCreateViewModel
{
    [Required(ErrorMessage = "Tên dự án không được để trống")]
    [MaxLength(200, ErrorMessage = "Tên dự án không quá 200 ký tự")]
    [Display(Name = "Tên dự án")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(2000)]
    [Display(Name = "Mô tả")]
    public string? Description { get; set; }

    [Display(Name = "Ngày bắt đầu")]
    public DateTime? StartDate { get; set; }

    [Display(Name = "Ngày kết thúc")]
    public DateTime? EndDate { get; set; }
}

public class ProjectEditViewModel : ProjectCreateViewModel
{
    public int ProjectId { get; set; }

    [Display(Name = "Trạng thái")]
    public ProjectStatus Status { get; set; }
}

public class ProjectDetailsViewModel
{
    public int ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ProjectStatus Status { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public decimal Progress { get; set; }
    public string OwnerName { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public ProjectMemberRole MyRole { get; set; }
    public bool IsSystemAdmin { get; set; }

    public List<ProjectMemberViewModel> Members { get; set; } = new();
    public List<ProjectTaskTreeViewModel> RootTasks { get; set; } = new();
    public List<ProjectTimelineRowViewModel> TimelineRows { get; set; } = new();
    public DateTime? TimelineStart { get; set; }
    public DateTime? TimelineEnd { get; set; }

    public int TotalTasks { get; set; }
    public int DoneTasks { get; set; }
    public int OverdueTasks { get; set; }
    
    // Filtered KPI for Manager (only their managed branch tasks)
    public int FilteredTotalTasks { get; set; }
    public int FilteredDoneTasks { get; set; }
    public int FilteredOverdueTasks { get; set; }
    
    public bool IsArchived { get; set; }

    public bool IsFullView { get; set; }
    public int CurrentEmployeeId { get; set; }
}

public class ProjectTimelineRowViewModel
{
    public int ProjectTaskId { get; set; }
    public int? ParentTaskId { get; set; }
    public string? ParentTaskTitle { get; set; }
    public int Depth { get; set; }
    public string Title { get; set; } = string.Empty;
    public ProjectTaskStatus Status { get; set; }
    public ProjectTaskPriority Priority { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? DueDate { get; set; }
    public decimal Progress { get; set; }
    public List<int> AssigneeIds { get; set; } = new();
    public List<string> AssigneeNames { get; set; } = new();
    public bool CanManageTask { get; set; }
    public int ChangeLogCount { get; set; }
    public DateTime? LatestChangeLogDate { get; set; }
    public string? LatestChangeLogSummary { get; set; }
    public bool IsOverdue => DueDate.HasValue
        && DueDate.Value.Date < DateTime.Today
        && Status != ProjectTaskStatus.Done
        && Status != ProjectTaskStatus.Cancelled;
    public bool IsNearDeadline => DueDate.HasValue
        && DueDate.Value.Date >= DateTime.Today
        && DueDate.Value.Date <= DateTime.Today.AddDays(2)
        && Status != ProjectTaskStatus.Done
        && Status != ProjectTaskStatus.Cancelled;
}

public class ProjectTaskParentOptionViewModel
{
    public int ProjectTaskId { get; set; }
    public string Label { get; set; } = string.Empty;
    public DateTime? StartDate { get; set; }
    public DateTime? DueDate { get; set; }
}

public class ProjectMemberViewModel
{
    public int ProjectMemberId { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string EmployeeCode { get; set; } = string.Empty;
    public string? Department { get; set; }
    public ProjectMemberRole Role { get; set; }
    public DateTime JoinedDate { get; set; }
}

public class AddMemberViewModel
{
    public int ProjectId { get; set; }
    public int EmployeeId { get; set; }
    public List<int> EmployeeIds { get; set; } = new();
    public ProjectMemberRole Role { get; set; } = ProjectMemberRole.ProjectStaff;
}

public class ProjectMemberCandidateViewModel
{
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string EmployeeCode { get; set; } = string.Empty;
    public int? DepartmentId { get; set; }
    public string DepartmentName { get; set; } = "Chưa có phòng ban";

    public string Display => $"{EmployeeName} ({EmployeeCode})";
    public string SearchText => $"{EmployeeName} {EmployeeCode} {DepartmentName}".ToLowerInvariant();
}

public class ChangeMemberRoleViewModel
{
    public int ProjectId { get; set; }
    [Required]
    public int EmployeeId { get; set; }
    [Required]
    public ProjectMemberRole NewRole { get; set; }
}
