using System.ComponentModel.DataAnnotations;
using managerCMN.Models.Enums;
using managerCMN.Helpers;

namespace managerCMN.Models.Entities;

public class ProjectTaskUpdate
{
    public int ProjectTaskUpdateId { get; set; }

    public int ProjectTaskId { get; set; }
    public ProjectTask ProjectTask { get; set; } = null!;

    public int SenderEmployeeId { get; set; }
    public Employee SenderEmployee { get; set; } = null!;

    [MaxLength(4000)]
    public string Content { get; set; } = string.Empty;

    public ProjectTaskStatus? StatusSnapshot { get; set; }
    public decimal? ProgressSnapshot { get; set; }
    public DateTime CreatedDate { get; set; } = DateTimeHelper.VietnamNow;

    public ICollection<ProjectTaskAttachment> Attachments { get; set; } = new List<ProjectTaskAttachment>();
}
