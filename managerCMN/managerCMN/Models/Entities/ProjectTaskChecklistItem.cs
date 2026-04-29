using System.ComponentModel.DataAnnotations;
using managerCMN.Helpers;

namespace managerCMN.Models.Entities;

public class ProjectTaskChecklistItem
{
    public int ProjectTaskChecklistItemId { get; set; }

    public int ProjectTaskId { get; set; }
    public ProjectTask ProjectTask { get; set; } = null!;

    [Required, MaxLength(500)]
    public string Title { get; set; } = string.Empty;

    public bool IsDone { get; set; } = false;
    public int SortOrder { get; set; } = 0;

    public DateTime? CompletedDate { get; set; }
    public int? CompletedByEmployeeId { get; set; }
    public Employee? CompletedByEmployee { get; set; }

    public DateTime CreatedDate { get; set; } = DateTimeHelper.VietnamNow;
}
