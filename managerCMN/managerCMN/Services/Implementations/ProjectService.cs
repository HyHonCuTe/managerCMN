using Microsoft.EntityFrameworkCore;
using managerCMN.Data;
using managerCMN.Models.Entities;
using managerCMN.Models.Enums;
using managerCMN.Models.ViewModels;
using managerCMN.Repositories.Interfaces;
using managerCMN.Services.Interfaces;

namespace managerCMN.Services.Implementations;

public class ProjectService : IProjectService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IProjectAccessService _accessService;
    private readonly ISystemLogService _logService;
    private readonly ApplicationDbContext _context;

    public ProjectService(IUnitOfWork unitOfWork, IProjectAccessService accessService,
        ISystemLogService logService, ApplicationDbContext context)
    {
        _unitOfWork = unitOfWork;
        _accessService = accessService;
        _logService = logService;
        _context = context;
    }

    public async Task<IEnumerable<ProjectListViewModel>> GetMyProjectsAsync(int employeeId)
    {
        var projects = await _unitOfWork.Projects.GetProjectsByMemberAsync(employeeId);
        var now = DateTime.Today;
        var result = new List<ProjectListViewModel>();

        foreach (var p in projects)
        {
            var myRole = p.Members.FirstOrDefault(m => m.EmployeeId == employeeId)?.Role ?? ProjectMemberRole.ProjectViewer;
            var owner = p.Members.FirstOrDefault(m => m.Role == ProjectMemberRole.ProjectOwner);

            var taskStats = await _context.ProjectTasks
                .Where(t => t.ProjectId == p.ProjectId)
                .GroupBy(t => 1)
                .Select(g => new {
                    Total = g.Count(),
                    Overdue = g.Count(t => t.DueDate < now && t.Status != ProjectTaskStatus.Done && t.Status != ProjectTaskStatus.Cancelled)
                })
                .FirstOrDefaultAsync();

            result.Add(new ProjectListViewModel
            {
                ProjectId = p.ProjectId,
                Name = p.Name,
                Description = p.Description,
                Status = p.Status,
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                Progress = p.Progress,
                MemberCount = p.Members.Count,
                TaskCount = taskStats?.Total ?? 0,
                OverdueTaskCount = taskStats?.Overdue ?? 0,
                MyRole = myRole,
                OwnerName = owner?.Employee?.FullName ?? p.CreatedByEmployee?.FullName ?? string.Empty,
                CreatedDate = p.CreatedDate,
                IsArchived = p.IsArchived
            });
        }

        return result;
    }

    public async Task<ProjectDetailsViewModel?> GetDetailsAsync(int projectId, int employeeId)
    {
        await _accessService.EnsureIsMemberAsync(projectId, employeeId);

        var project = await _unitOfWork.Projects.GetWithDetailsAsync(projectId);
        if (project == null) return null;

        var myRole = project.Members.FirstOrDefault(m => m.EmployeeId == employeeId)?.Role ?? ProjectMemberRole.ProjectViewer;
        var owner = project.Members.FirstOrDefault(m => m.Role == ProjectMemberRole.ProjectOwner);
        var now = DateTime.Today;

        var allTasks = await _context.ProjectTasks
            .Where(t => t.ProjectId == projectId)
            .ToListAsync();

        var members = project.Members.Select(m => new ProjectMemberViewModel
        {
            ProjectMemberId = m.ProjectMemberId,
            EmployeeId = m.EmployeeId,
            EmployeeName = m.Employee?.FullName ?? string.Empty,
            EmployeeCode = m.Employee?.EmployeeCode ?? string.Empty,
            Department = m.Employee?.Department?.DepartmentName,
            Role = m.Role,
            JoinedDate = m.JoinedDate
        }).OrderBy(m => m.Role).ToList();

        return new ProjectDetailsViewModel
        {
            ProjectId = project.ProjectId,
            Name = project.Name,
            Description = project.Description,
            Status = project.Status,
            StartDate = project.StartDate,
            EndDate = project.EndDate,
            Progress = project.Progress,
            OwnerName = owner?.Employee?.FullName ?? project.CreatedByEmployee?.FullName ?? string.Empty,
            CreatedDate = project.CreatedDate,
            MyRole = myRole,
            Members = members,
            TotalTasks = allTasks.Count,
            DoneTasks = allTasks.Count(t => t.Status == ProjectTaskStatus.Done),
            OverdueTasks = allTasks.Count(t => t.DueDate < now && t.Status != ProjectTaskStatus.Done && t.Status != ProjectTaskStatus.Cancelled),
            IsArchived = project.IsArchived
        };
    }

    public async Task<Project?> GetByIdAsync(int projectId)
        => await _unitOfWork.Projects.GetByIdAsync(projectId);

    public async Task<int> CreateAsync(ProjectCreateViewModel vm, int creatorEmployeeId)
    {
        var project = new Project
        {
            Name = vm.Name,
            Description = vm.Description,
            StartDate = vm.StartDate,
            EndDate = vm.EndDate,
            Status = ProjectStatus.Planning,
            CreatedByEmployeeId = creatorEmployeeId,
            CreatedDate = DateTime.Now
        };

        await _unitOfWork.Projects.AddAsync(project);
        await _unitOfWork.SaveChangesAsync();

        var ownerMember = new ProjectMember
        {
            ProjectId = project.ProjectId,
            EmployeeId = creatorEmployeeId,
            Role = ProjectMemberRole.ProjectOwner,
            AddedByEmployeeId = creatorEmployeeId,
            JoinedDate = DateTime.Now
        };

        await _unitOfWork.ProjectMembers.AddAsync(ownerMember);
        await _unitOfWork.SaveChangesAsync();

        await _logService.LogAsync(null, "Create", "Project", null, new { project.ProjectId, project.Name }, null);

        return project.ProjectId;
    }

    public async Task UpdateAsync(ProjectEditViewModel vm, int employeeId)
    {
        await _accessService.EnsureIsMemberAsync(vm.ProjectId, employeeId);
        var role = await _accessService.GetRoleAsync(vm.ProjectId, employeeId);
        if (role != ProjectMemberRole.ProjectOwner && role != ProjectMemberRole.ProjectManager)
            throw new UnauthorizedAccessException("Chỉ Owner hoặc Manager mới được sửa thông tin dự án.");

        var project = await _unitOfWork.Projects.GetByIdAsync(vm.ProjectId)
            ?? throw new InvalidOperationException("Dự án không tồn tại.");

        project.Name = vm.Name;
        project.Description = vm.Description;
        project.StartDate = vm.StartDate;
        project.EndDate = vm.EndDate;
        project.Status = vm.Status;
        project.ModifiedDate = DateTime.Now;

        _unitOfWork.Projects.Update(project);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task ArchiveAsync(int projectId, int employeeId)
    {
        await _accessService.EnsureCanManageManagersAsync(projectId, employeeId);

        var project = await _unitOfWork.Projects.GetByIdAsync(projectId)
            ?? throw new InvalidOperationException("Dự án không tồn tại.");

        project.IsArchived = true;
        project.ModifiedDate = DateTime.Now;
        _unitOfWork.Projects.Update(project);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task AddMemberAsync(AddMemberViewModel vm, int actorEmployeeId)
    {
        await _accessService.EnsureCanManageMembersAsync(vm.ProjectId, actorEmployeeId);

        var actorRole = await _accessService.GetRoleAsync(vm.ProjectId, actorEmployeeId);

        // Manager cannot add Owner or another Manager
        if (actorRole == ProjectMemberRole.ProjectManager
            && (vm.Role == ProjectMemberRole.ProjectOwner || vm.Role == ProjectMemberRole.ProjectManager))
            throw new InvalidOperationException("ProjectManager không được bổ nhiệm Owner hoặc Manager khác.");

        var existing = await _unitOfWork.ProjectMembers.GetMemberAsync(vm.ProjectId, vm.EmployeeId);
        if (existing != null) throw new InvalidOperationException("Nhân viên đã là thành viên của dự án.");

        var member = new ProjectMember
        {
            ProjectId = vm.ProjectId,
            EmployeeId = vm.EmployeeId,
            Role = vm.Role,
            AddedByEmployeeId = actorEmployeeId,
            JoinedDate = DateTime.Now
        };

        await _unitOfWork.ProjectMembers.AddAsync(member);
        await _unitOfWork.SaveChangesAsync();
        await _logService.LogAsync(null, "AddMember", "Project", null, new { vm.ProjectId, vm.EmployeeId }, null);
    }

    public async Task RemoveMemberAsync(int projectId, int targetEmployeeId, int actorEmployeeId)
    {
        await _accessService.EnsureCanManageMembersAsync(projectId, actorEmployeeId);

        var targetRole = await _accessService.GetRoleAsync(projectId, targetEmployeeId);
        if (targetRole == ProjectMemberRole.ProjectOwner)
            throw new InvalidOperationException("Không thể xoá ProjectOwner khỏi dự án.");

        var actorRole = await _accessService.GetRoleAsync(projectId, actorEmployeeId);
        if (actorRole == ProjectMemberRole.ProjectManager && targetRole == ProjectMemberRole.ProjectManager)
            throw new InvalidOperationException("ProjectManager không thể xoá Manager khác.");

        var member = await _unitOfWork.ProjectMembers.GetMemberAsync(projectId, targetEmployeeId)
            ?? throw new InvalidOperationException("Thành viên không tồn tại.");

        _unitOfWork.ProjectMembers.Remove(member);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task ChangeMemberRoleAsync(ChangeMemberRoleViewModel vm, int actorEmployeeId)
    {
        if (vm.NewRole == ProjectMemberRole.ProjectOwner)
            throw new InvalidOperationException("Không thể bổ nhiệm ProjectOwner thứ hai.");

        if (vm.NewRole == ProjectMemberRole.ProjectManager)
            await _accessService.EnsureCanManageManagersAsync(vm.ProjectId, actorEmployeeId);
        else
            await _accessService.EnsureCanManageMembersAsync(vm.ProjectId, actorEmployeeId);

        var member = await _unitOfWork.ProjectMembers.GetMemberAsync(vm.ProjectId, vm.EmployeeId)
            ?? throw new InvalidOperationException("Thành viên không tồn tại.");

        if (member.Role == ProjectMemberRole.ProjectOwner)
            throw new InvalidOperationException("Không thể thay đổi role của ProjectOwner.");

        member.Role = vm.NewRole;
        _unitOfWork.ProjectMembers.Update(member);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<IEnumerable<ProjectMemberViewModel>> GetMembersAsync(int projectId, int employeeId)
    {
        await _accessService.EnsureIsMemberAsync(projectId, employeeId);
        var members = await _unitOfWork.ProjectMembers.GetProjectMembersAsync(projectId);

        return members.Select(m => new ProjectMemberViewModel
        {
            ProjectMemberId = m.ProjectMemberId,
            EmployeeId = m.EmployeeId,
            EmployeeName = m.Employee?.FullName ?? string.Empty,
            EmployeeCode = m.Employee?.EmployeeCode ?? string.Empty,
            Role = m.Role,
            JoinedDate = m.JoinedDate
        });
    }
}
