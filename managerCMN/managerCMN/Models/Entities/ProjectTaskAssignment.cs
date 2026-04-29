using managerCMN.Helpers;

namespace managerCMN.Models.Entities;

public class ProjectTaskAssignment
{
    public int ProjectTaskAssignmentId { get; set; }

    public int ProjectTaskId { get; set; }
    public ProjectTask ProjectTask { get; set; } = null!;

    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public int AssignedByEmployeeId { get; set; }
    public Employee AssignedByEmployee { get; set; } = null!;

    public DateTime AssignedDate { get; set; } = DateTimeHelper.VietnamNow;

    public bool IsCompleted { get; set; } = false;
    public DateTime? CompletedDate { get; set; }
}
