using System.ComponentModel.DataAnnotations;
using managerCMN.Helpers;

namespace managerCMN.Models.Entities;

public class ProjectTemplate
{
    public int ProjectTemplateId { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public int CreatedByEmployeeId { get; set; }
    public Employee CreatedByEmployee { get; set; } = null!;

    public DateTime CreatedDate { get; set; } = DateTimeHelper.VietnamNow;
    public DateTime? ModifiedDate { get; set; }

    public ICollection<ProjectTemplateTask> Tasks { get; set; } = new List<ProjectTemplateTask>();
}
