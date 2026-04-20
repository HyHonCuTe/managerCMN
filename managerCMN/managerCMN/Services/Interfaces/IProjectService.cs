using managerCMN.Models.Entities;
using managerCMN.Models.Enums;
using managerCMN.Models.ViewModels;

namespace managerCMN.Services.Interfaces;

public interface IProjectService
{
    Task<IEnumerable<ProjectListViewModel>> GetMyProjectsAsync(int employeeId);
    Task<ProjectDetailsViewModel?> GetDetailsAsync(int projectId, int employeeId);
    Task<Project?> GetByIdAsync(int projectId);
    Task<int> CreateAsync(ProjectCreateViewModel vm, int creatorEmployeeId);
    Task UpdateAsync(ProjectEditViewModel vm, int employeeId);
    Task ArchiveAsync(int projectId, int employeeId);
    Task AddMemberAsync(AddMemberViewModel vm, int actorEmployeeId);
    Task RemoveMemberAsync(int projectId, int targetEmployeeId, int actorEmployeeId);
    Task ChangeMemberRoleAsync(ChangeMemberRoleViewModel vm, int actorEmployeeId);
    Task<IEnumerable<ProjectMemberViewModel>> GetMembersAsync(int projectId, int employeeId);
}
