using managerCMN.Models.Enums;
using managerCMN.Helpers;

namespace managerCMN.Models.Entities;

public class ProjectMember
{
    public int ProjectMemberId { get; set; }

    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public ProjectMemberRole Role { get; set; } = ProjectMemberRole.ProjectStaff;

    public int AddedByEmployeeId { get; set; }
    public Employee AddedByEmployee { get; set; } = null!;

    public DateTime JoinedDate { get; set; } = DateTimeHelper.VietnamNow;
}
