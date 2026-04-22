using System.ComponentModel.DataAnnotations;
using managerCMN.Models.Enums;

namespace managerCMN.Models.Entities;

public class ProjectTemplateTask
{
    public int ProjectTemplateTaskId { get; set; }

    public int ProjectTemplateId { get; set; }
    public ProjectTemplate ProjectTemplate { get; set; } = null!;

    public int? ParentTemplateTaskId { get; set; }
    public ProjectTemplateTask? ParentTask { get; set; }

    [Required, MaxLength(300)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    public ProjectTaskPriority Priority { get; set; } = ProjectTaskPriority.Medium;

    public decimal? EstimatedHours { get; set; }

    public int SortOrder { get; set; } = 0;

    public ICollection<ProjectTemplateTask> SubTasks { get; set; } = new List<ProjectTemplateTask>();
}
