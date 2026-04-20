using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using managerCMN.Models.Enums;

namespace managerCMN.Models.ViewModels;

public class ProjectTaskTreeViewModel
{
    public int ProjectTaskId { get; set; }
    public int ProjectId { get; set; }
    public int? ParentTaskId { get; set; }
    public string? ParentTaskTitle { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ProjectTaskStatus Status { get; set; }
    public ProjectTaskPriority Priority { get; set; }
    public ProgressMode ProgressMode { get; set; }
    public decimal Progress { get; set; }
    public decimal? EstimatedHours { get; set; }
    public decimal? ActualHours { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public string? CreatedByName { get; set; }
    public bool IsOverdue => DueDate.HasValue && DueDate.Value < DateTime.Today && Status != ProjectTaskStatus.Done && Status != ProjectTaskStatus.Cancelled;
    public List<string> AssigneeNames { get; set; } = new();
    public List<int> AssigneeIds { get; set; } = new();
    public int AssigneeTotalCount { get; set; }
    public int AssigneeCompletedCount { get; set; }
    public bool IsCurrentAssigneeCompleted { get; set; }
    public int ChecklistTotal { get; set; }
    public int ChecklistDone { get; set; }
    public List<ProjectTaskTreeViewModel> SubTasks { get; set; } = new();
    public List<ChecklistItemViewModel> ChecklistItems { get; set; } = new();
    public List<ProjectTaskUpdateViewModel> Updates { get; set; } = new();
    public List<ProjectTaskMemberOptionViewModel> AvailableMembers { get; set; } = new();
    public bool CanManageTask { get; set; }
    public bool CanCompleteTask { get; set; }
    public bool CanManageMembers { get; set; }
    public bool IsArchived { get; set; }
    public bool WorklogAvailable { get; set; } = true;
    public int Depth { get; set; } = 0;
}

public class ProjectTaskCreateViewModel
{
    public int ProjectId { get; set; }
    public int? ParentTaskId { get; set; }

    [Required(ErrorMessage = "Tiêu đề không được để trống")]
    [MaxLength(300, ErrorMessage = "Tiêu đề không quá 300 ký tự")]
    [Display(Name = "Tiêu đề")]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    [Display(Name = "Mô tả")]
    public string? Description { get; set; }

    [Display(Name = "Độ ưu tiên")]
    public ProjectTaskPriority Priority { get; set; } = ProjectTaskPriority.Medium;

    [Display(Name = "Ngày bắt đầu")]
    public DateTime? StartDate { get; set; }

    [Display(Name = "Hạn hoàn thành")]
    public DateTime? DueDate { get; set; }

    [Display(Name = "Giờ ước tính")]
    public decimal? EstimatedHours { get; set; }

    [Display(Name = "Chế độ tiến độ")]
    public ProgressMode ProgressMode { get; set; } = ProgressMode.Auto;

    public List<int> AssigneeIds { get; set; } = new();
}

public class ProjectTaskEditViewModel : ProjectTaskCreateViewModel
{
    public int ProjectTaskId { get; set; }

    [Display(Name = "Trạng thái")]
    public ProjectTaskStatus Status { get; set; }

    [Display(Name = "Giờ thực tế")]
    public decimal? ActualHours { get; set; }

    [Display(Name = "% hoàn thành")]
    [Range(0, 100)]
    public decimal Progress { get; set; }
}

public class ChecklistItemViewModel
{
    public int ProjectTaskChecklistItemId { get; set; }
    public int ProjectTaskId { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool IsDone { get; set; }
    public int SortOrder { get; set; }
    public string? CompletedByName { get; set; }
    public DateTime? CompletedDate { get; set; }
}

public class AddChecklistItemViewModel
{
    public int ProjectTaskId { get; set; }
    [Required, MaxLength(500)]
    public string Title { get; set; } = string.Empty;
}

public class UpdateTaskStatusViewModel
{
    public int ProjectTaskId { get; set; }
    public ProjectTaskStatus Status { get; set; }
}

public class UpdateTaskProgressViewModel
{
    public int ProjectTaskId { get; set; }
    [Range(0, 100)]
    public decimal Progress { get; set; }
}

public class ProjectTaskUpdateViewModel
{
    public int ProjectTaskUpdateId { get; set; }
    public int ProjectTaskId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public ProjectTaskStatus? StatusSnapshot { get; set; }
    public decimal? ProgressSnapshot { get; set; }
    public DateTime CreatedDate { get; set; }
    public List<ProjectTaskAttachmentViewModel> Attachments { get; set; } = new();
}

public class ProjectTaskAttachmentViewModel
{
    public int ProjectTaskAttachmentId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string? ContentType { get; set; }
}

public class ProjectTaskMemberOptionViewModel
{
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string EmployeeCode { get; set; } = string.Empty;
    public string RoleLabel { get; set; } = string.Empty;
}

public class PostTaskUpdateViewModel
{
    public int ProjectTaskId { get; set; }
    public int ProjectId { get; set; }

    [MaxLength(4000)]
    public string? Content { get; set; }

    public List<IFormFile>? Attachments { get; set; }
}
