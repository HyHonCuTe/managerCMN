using System.ComponentModel.DataAnnotations;
using managerCMN.Models.Enums;
using managerCMN.Helpers;

namespace managerCMN.Models.Entities;

public class Project
{
    public int ProjectId { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    public ProjectStatus Status { get; set; } = ProjectStatus.Planning;

    // Status before archiving (for restore)
    public ProjectStatus? PriorStatus { get; set; }

    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    public decimal Progress { get; set; } = 0;

    public int CreatedByEmployeeId { get; set; }
    public Employee CreatedByEmployee { get; set; } = null!;

    public DateTime CreatedDate { get; set; } = DateTimeHelper.VietnamNow;
    public DateTime? ModifiedDate { get; set; }

    public bool IsArchived { get; set; } = false;

    public ICollection<ProjectMember> Members { get; set; } = new List<ProjectMember>();
    public ICollection<ProjectTask> Tasks { get; set; } = new List<ProjectTask>();
}
