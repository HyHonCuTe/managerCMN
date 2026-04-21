using managerCMN.Models.Enums;
using managerCMN.Repositories.Interfaces;
using managerCMN.Services.Interfaces;
using Microsoft.AspNetCore.Http;

namespace managerCMN.Services.Implementations;

public class ProjectAccessService : IProjectAccessService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ProjectAccessService(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor)
    {
        _unitOfWork = unitOfWork;
        _httpContextAccessor = httpContextAccessor;
    }

    public bool IsSystemAdmin()
        => _httpContextAccessor.HttpContext?.User?.IsInRole("Admin") == true;

    public async Task<ProjectMemberRole?> GetRoleAsync(int projectId, int employeeId)
        => await _unitOfWork.ProjectMembers.GetMemberRoleAsync(projectId, employeeId);

    public async Task<bool> IsMemberAsync(int projectId, int employeeId)
        => IsSystemAdmin() || await _unitOfWork.Projects.IsMemberAsync(projectId, employeeId);

    public async Task EnsureIsMemberAsync(int projectId, int employeeId)
    {
        if (IsSystemAdmin())
            return;

        if (!await IsMemberAsync(projectId, employeeId))
            throw new UnauthorizedAccessException("Bạn không phải thành viên của dự án này.");
    }

    public async Task EnsureCanManageMembersAsync(int projectId, int employeeId)
    {
        if (IsSystemAdmin())
            return;

        var role = await GetRoleAsync(projectId, employeeId);
        if (role != ProjectMemberRole.ProjectOwner)
            throw new UnauthorizedAccessException("Chỉ ProjectOwner hoặc admin hệ thống mới được quản lý thành viên dự án.");
    }

    public async Task EnsureCanManageManagersAsync(int projectId, int employeeId)
    {
        if (IsSystemAdmin())
            return;

        var role = await GetRoleAsync(projectId, employeeId);
        if (role != ProjectMemberRole.ProjectOwner)
            throw new UnauthorizedAccessException("Chỉ ProjectOwner mới được bổ nhiệm/thu hồi ProjectManager.");
    }

    public async Task EnsureCanManageTaskAsync(int projectId, int employeeId)
    {
        if (IsSystemAdmin())
            return;

        var role = await GetRoleAsync(projectId, employeeId);
        if (role != ProjectMemberRole.ProjectOwner)
            throw new UnauthorizedAccessException("Chỉ ProjectOwner hoặc admin hệ thống mới được quản lý toàn bộ công việc trong dự án này.");
    }
}
