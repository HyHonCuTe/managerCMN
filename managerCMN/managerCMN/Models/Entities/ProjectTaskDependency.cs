namespace managerCMN.Models.Entities;

public class ProjectTaskDependency
{
    public int ProjectTaskDependencyId { get; set; }

    public int PredecessorTaskId { get; set; }
    public ProjectTask PredecessorTask { get; set; } = null!;

    public int SuccessorTaskId { get; set; }
    public ProjectTask SuccessorTask { get; set; } = null!;
}
