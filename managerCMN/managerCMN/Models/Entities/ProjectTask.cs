using System.ComponentModel.DataAnnotations;
using managerCMN.Models.Enums;
using managerCMN.Helpers;

namespace managerCMN.Models.Entities;

public class ProjectTask
{
    public int ProjectTaskId { get; set; }

    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    public int? ParentTaskId { get; set; }
    public ProjectTask? ParentTask { get; set; }

    [Required, MaxLength(300)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    public ProjectTaskStatus Status { get; set; } = ProjectTaskStatus.Todo;
    public ProjectTaskPriority Priority { get; set; } = ProjectTaskPriority.Medium;
    public ProgressMode ProgressMode { get; set; } = ProgressMode.Auto;

    public decimal Progress { get; set; } = 0;
    public decimal? EstimatedHours { get; set; }
    public decimal? ActualHours { get; set; }

    public DateTime? StartDate { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? CompletedDate { get; set; }

    public int CreatedByEmployeeId { get; set; }
    public Employee CreatedByEmployee { get; set; } = null!;

    public DateTime CreatedDate { get; set; } = DateTimeHelper.VietnamNow;
    public DateTime? ModifiedDate { get; set; }

    public ICollection<ProjectTask> SubTasks { get; set; } = new List<ProjectTask>();
    public ICollection<ProjectTaskAssignment> Assignments { get; set; } = new List<ProjectTaskAssignment>();
    public ICollection<ProjectTaskChecklistItem> ChecklistItems { get; set; } = new List<ProjectTaskChecklistItem>();
    public ICollection<ProjectTaskUpdate> Updates { get; set; } = new List<ProjectTaskUpdate>();
    public ICollection<ProjectTaskDependency> Predecessors { get; set; } = new List<ProjectTaskDependency>();
    public ICollection<ProjectTaskDependency> Successors { get; set; } = new List<ProjectTaskDependency>();
}
