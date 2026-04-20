namespace managerCMN.Services.Interfaces;

public interface IProjectProgressService
{
    Task RecalculateTaskProgressAsync(int taskId);
    Task BubbleUpParentProgressAsync(int taskId);
    Task RecalculateProjectProgressAsync(int projectId);
}
