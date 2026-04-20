using managerCMN.Models.Enums;
using managerCMN.Repositories.Interfaces;
using managerCMN.Services.Interfaces;

namespace managerCMN.Services.Implementations;

public class ProjectAccessService : IProjectAccessService
{
    private readonly IUnitOfWork _unitOfWork;

    public ProjectAccessService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ProjectMemberRole?> GetRoleAsync(int projectId, int employeeId)
        => await _unitOfWork.ProjectMembers.GetMemberRoleAsync(projectId, employeeId);

    public async Task<bool> IsMemberAsync(int projectId, int employeeId)
        => await _unitOfWork.Projects.IsMemberAsync(projectId, employeeId);

    public async Task EnsureIsMemberAsync(int projectId, int employeeId)
    {
        if (!await IsMemberAsync(projectId, employeeId))
            throw new UnauthorizedAccessException("Bạn không phải thành viên của dự án này.");
    }

    public async Task EnsureCanManageMembersAsync(int projectId, int employeeId)
    {
        var role = await GetRoleAsync(projectId, employeeId);
        if (role == null || (role != ProjectMemberRole.ProjectOwner && role != ProjectMemberRole.ProjectManager))
            throw new UnauthorizedAccessException("Chỉ ProjectOwner và ProjectManager mới được quản lý thành viên.");
    }

    public async Task EnsureCanManageManagersAsync(int projectId, int employeeId)
    {
        var role = await GetRoleAsync(projectId, employeeId);
        if (role != ProjectMemberRole.ProjectOwner)
            throw new UnauthorizedAccessException("Chỉ ProjectOwner mới được bổ nhiệm/thu hồi ProjectManager.");
    }

    public async Task EnsureCanManageTaskAsync(int projectId, int employeeId)
    {
        var role = await GetRoleAsync(projectId, employeeId);
        if (role == null || role == ProjectMemberRole.ProjectViewer)
            throw new UnauthorizedAccessException("Bạn không có quyền quản lý công việc trong dự án này.");
    }
}
