using Microsoft.AspNetCore.Hosting;
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
    private readonly IWebHostEnvironment _env;

    public ProjectService(IUnitOfWork unitOfWork, IProjectAccessService accessService,
        ISystemLogService logService, ApplicationDbContext context, IWebHostEnvironment env)
    {
        _unitOfWork = unitOfWork;
        _accessService = accessService;
        _logService = logService;
        _context = context;
        _env = env;
    }

    public async Task<IEnumerable<ProjectListViewModel>> GetMyProjectsAsync(int employeeId, bool includeAll = false)
    {
        var projects = includeAll
            ? await _context.Projects
                .AsNoTracking()
                .Include(p => p.CreatedByEmployee)
                .Include(p => p.Members)
                    .ThenInclude(m => m.Employee)
                .OrderByDescending(p => p.CreatedDate)
                .ToListAsync()
            : (await _unitOfWork.Projects.GetProjectsByMemberAsync(employeeId)).ToList();
        var now = DateTime.Today;
        var result = new List<ProjectListViewModel>();

        foreach (var p in projects)
        {
            var myRole = p.Members.FirstOrDefault(m => m.EmployeeId == employeeId)?.Role ?? ProjectMemberRole.ProjectViewer;
            var owner = p.Members.FirstOrDefault(m => m.Role == ProjectMemberRole.ProjectOwner);
            var isArchived = p.IsArchived || p.Status == ProjectStatus.Archived;

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
                Status = isArchived ? ProjectStatus.Archived : p.Status,
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                Progress = p.Progress,
                MemberCount = p.Members.Count,
                TaskCount = taskStats?.Total ?? 0,
                OverdueTaskCount = taskStats?.Overdue ?? 0,
                MyRole = myRole,
                IsSystemAdmin = includeAll,
                OwnerName = owner?.Employee?.FullName ?? p.CreatedByEmployee?.FullName ?? string.Empty,
                CreatedDate = p.CreatedDate,
                IsArchived = isArchived
            });
        }

        return result;
    }

    public async Task<ProjectDetailsViewModel?> GetDetailsAsync(int projectId, int employeeId, bool ignoreAccessCheck = false)
    {
        if (!ignoreAccessCheck)
            await _accessService.EnsureIsMemberAsync(projectId, employeeId);

        var project = await _unitOfWork.Projects.GetWithDetailsAsync(projectId);
        if (project == null) return null;

        var myRole = project.Members.FirstOrDefault(m => m.EmployeeId == employeeId)?.Role ?? ProjectMemberRole.ProjectViewer;
        var isSystemAdmin = ignoreAccessCheck || _accessService.IsSystemAdmin();
        var owner = project.Members.FirstOrDefault(m => m.Role == ProjectMemberRole.ProjectOwner);
        var isArchived = project.IsArchived || project.Status == ProjectStatus.Archived;
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
            Status = isArchived ? ProjectStatus.Archived : project.Status,
            StartDate = project.StartDate,
            EndDate = project.EndDate,
            Progress = project.Progress,
            OwnerName = owner?.Employee?.FullName ?? project.CreatedByEmployee?.FullName ?? string.Empty,
            CreatedDate = project.CreatedDate,
            MyRole = myRole,
            IsSystemAdmin = isSystemAdmin,
            Members = members,
            TotalTasks = allTasks.Count,
            DoneTasks = allTasks.Count(t => t.Status == ProjectTaskStatus.Done),
            OverdueTasks = allTasks.Count(t => t.DueDate < now && t.Status != ProjectTaskStatus.Done && t.Status != ProjectTaskStatus.Cancelled),
            IsArchived = isArchived
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
        if (!_accessService.IsSystemAdmin() && role != ProjectMemberRole.ProjectOwner)
            throw new UnauthorizedAccessException("Chỉ ProjectOwner hoặc admin hệ thống mới được sửa thông tin dự án.");

        var project = await _unitOfWork.Projects.GetByIdAsync(vm.ProjectId)
            ?? throw new InvalidOperationException("Dự án không tồn tại.");

        if (project.IsArchived || project.Status == ProjectStatus.Archived)
            throw new InvalidOperationException("Dự án đã lưu trữ chỉ có thể xem, không thể chỉnh sửa.");

        if (vm.Status == ProjectStatus.Archived)
            throw new InvalidOperationException("Hãy dùng nút Lưu trữ để chuyển dự án vào khu lưu trữ.");

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
        if (!_accessService.IsSystemAdmin())
        {
            var role = await _accessService.GetRoleAsync(projectId, employeeId);
            if (role != ProjectMemberRole.ProjectOwner)
                throw new UnauthorizedAccessException("Chỉ ProjectOwner hoặc admin hệ thống mới được lưu trữ dự án.");
        }

        var project = await _unitOfWork.Projects.GetByIdAsync(projectId)
            ?? throw new InvalidOperationException("Dự án không tồn tại.");

        if (project.IsArchived && project.Status == ProjectStatus.Archived)
            return;

        project.IsArchived = true;
        project.Status = ProjectStatus.Archived;
        project.ModifiedDate = DateTime.Now;
        _unitOfWork.Projects.Update(project);
        await _unitOfWork.SaveChangesAsync();
        await _logService.LogAsync(null, "Archive", "Project", null, new { project.ProjectId, project.Name }, null);
    }

    public async Task RestoreAsync(int projectId, int employeeId)
    {
        if (!_accessService.IsSystemAdmin())
            throw new UnauthorizedAccessException("Chỉ admin hệ thống mới được phục hồi dự án từ lưu trữ.");

        var project = await _unitOfWork.Projects.GetByIdAsync(projectId)
            ?? throw new InvalidOperationException("Dự án không tồn tại.");

        if (!project.IsArchived && project.Status != ProjectStatus.Archived)
            return;

        project.IsArchived = false;
        project.Status = ProjectStatus.Planning;
        project.ModifiedDate = DateTime.Now;
        _unitOfWork.Projects.Update(project);
        await _unitOfWork.SaveChangesAsync();
        await _logService.LogAsync(null, "Restore", "Project", null, new { project.ProjectId, project.Name }, null);
    }

    public async Task DeleteAsync(int projectId, int employeeId)
    {
        if (!_accessService.IsSystemAdmin())
            throw new UnauthorizedAccessException("Chỉ admin hệ thống mới được xoá dự án.");

        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.ProjectId == projectId)
            ?? throw new InvalidOperationException("Dự án không tồn tại.");

        if (!project.IsArchived && project.Status != ProjectStatus.Archived)
            throw new InvalidOperationException("Chỉ được xoá dự án đã nằm trong lưu trữ.");

        var taskIds = await _context.ProjectTasks
            .Where(t => t.ProjectId == projectId)
            .Select(t => t.ProjectTaskId)
            .ToListAsync();

        var attachmentPaths = new List<string>();

        await using var transaction = await _context.Database.BeginTransactionAsync();

        if (taskIds.Count > 0)
        {
            var dependencies = await _context.ProjectTaskDependencies
                .Where(d => taskIds.Contains(d.PredecessorTaskId) || taskIds.Contains(d.SuccessorTaskId))
                .ToListAsync();
            _context.ProjectTaskDependencies.RemoveRange(dependencies);

            var updateIds = await _context.ProjectTaskUpdates
                .Where(u => taskIds.Contains(u.ProjectTaskId))
                .Select(u => u.ProjectTaskUpdateId)
                .ToListAsync();

            var attachments = updateIds.Count == 0
                ? new List<ProjectTaskAttachment>()
                : await _context.ProjectTaskAttachments
                    .Where(a => updateIds.Contains(a.ProjectTaskUpdateId))
                    .ToListAsync();
            attachmentPaths.AddRange(attachments.Select(a => a.FilePath));
            _context.ProjectTaskAttachments.RemoveRange(attachments);

            var updates = updateIds.Count == 0
                ? new List<ProjectTaskUpdate>()
                : await _context.ProjectTaskUpdates
                    .Where(u => updateIds.Contains(u.ProjectTaskUpdateId))
                    .ToListAsync();
            _context.ProjectTaskUpdates.RemoveRange(updates);

            var checklistItems = await _context.ProjectTaskChecklistItems
                .Where(c => taskIds.Contains(c.ProjectTaskId))
                .ToListAsync();
            _context.ProjectTaskChecklistItems.RemoveRange(checklistItems);

            var assignments = await _context.ProjectTaskAssignments
                .Where(a => taskIds.Contains(a.ProjectTaskId))
                .ToListAsync();
            _context.ProjectTaskAssignments.RemoveRange(assignments);

            var tasks = await _context.ProjectTasks
                .Where(t => t.ProjectId == projectId)
                .ToListAsync();

            foreach (var task in tasks)
                task.ParentTaskId = null;

            await _context.SaveChangesAsync();
            _context.ProjectTasks.RemoveRange(tasks);
        }

        var members = await _context.ProjectMembers
            .Where(m => m.ProjectId == projectId)
            .ToListAsync();
        _context.ProjectMembers.RemoveRange(members);
        _context.Projects.Remove(project);

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        DeleteAttachmentFiles(attachmentPaths);
        await _logService.LogAsync(null, "Delete", "Project", new { project.ProjectId, project.Name }, null, null);
    }

    public async Task AddMemberAsync(AddMemberViewModel vm, int actorEmployeeId)
    {
        await _accessService.EnsureCanManageMembersAsync(vm.ProjectId, actorEmployeeId);

        var actorRole = await _accessService.GetRoleAsync(vm.ProjectId, actorEmployeeId);
        var isSystemAdmin = _accessService.IsSystemAdmin();

        if (vm.Role == ProjectMemberRole.ProjectOwner)
            throw new InvalidOperationException("Không thể bổ nhiệm ProjectOwner thứ hai.");

        // Manager cannot add Owner or another Manager
        if (!isSystemAdmin
            && actorRole == ProjectMemberRole.ProjectManager
            && (vm.Role == ProjectMemberRole.ProjectOwner || vm.Role == ProjectMemberRole.ProjectManager))
            throw new InvalidOperationException("ProjectManager không được bổ nhiệm Owner hoặc Manager khác.");

        var employeeIds = (vm.EmployeeIds ?? new List<int>())
            .Append(vm.EmployeeId)
            .Where(id => id > 0)
            .Distinct()
            .ToList();

        if (employeeIds.Count == 0)
            throw new InvalidOperationException("Vui lòng chọn ít nhất một nhân viên.");

        var addedIds = new List<int>();
        foreach (var employeeId in employeeIds)
        {
            var existing = await _unitOfWork.ProjectMembers.GetMemberAsync(vm.ProjectId, employeeId);
            if (existing != null)
                continue;

            var member = new ProjectMember
            {
                ProjectId = vm.ProjectId,
                EmployeeId = employeeId,
                Role = vm.Role,
                AddedByEmployeeId = actorEmployeeId,
                JoinedDate = DateTime.Now
            };

            await _unitOfWork.ProjectMembers.AddAsync(member);
            addedIds.Add(employeeId);
        }

        if (addedIds.Count == 0)
            throw new InvalidOperationException("Các nhân viên đã chọn đều đã là thành viên của dự án.");

        await _unitOfWork.SaveChangesAsync();
        await _logService.LogAsync(null, "AddMember", "Project", null, new { vm.ProjectId, EmployeeIds = addedIds }, null);
    }

    public async Task RemoveMemberAsync(int projectId, int targetEmployeeId, int actorEmployeeId)
    {
        await _accessService.EnsureCanManageMembersAsync(projectId, actorEmployeeId);
        var isSystemAdmin = _accessService.IsSystemAdmin();

        var targetRole = await _accessService.GetRoleAsync(projectId, targetEmployeeId);
        if (!isSystemAdmin && targetRole == ProjectMemberRole.ProjectOwner)
            throw new InvalidOperationException("Không thể xoá ProjectOwner khỏi dự án.");

        var actorRole = await _accessService.GetRoleAsync(projectId, actorEmployeeId);
        if (!isSystemAdmin
            && actorRole == ProjectMemberRole.ProjectManager
            && targetRole == ProjectMemberRole.ProjectManager)
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

    public async Task<IEnumerable<ProjectMemberViewModel>> GetMembersAsync(int projectId, int employeeId, bool ignoreAccessCheck = false)
    {
        if (!ignoreAccessCheck)
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

    private void DeleteAttachmentFiles(IEnumerable<string> filePaths)
    {
        if (string.IsNullOrWhiteSpace(_env.WebRootPath))
            return;

        var webRootPath = Path.GetFullPath(_env.WebRootPath);
        if (!webRootPath.EndsWith(Path.DirectorySeparatorChar))
            webRootPath += Path.DirectorySeparatorChar;

        foreach (var filePath in filePaths.Where(p => !string.IsNullOrWhiteSpace(p)).Distinct())
        {
            try
            {
                var relativePath = filePath
                    .TrimStart('~', '/', '\\')
                    .Replace('/', Path.DirectorySeparatorChar)
                    .Replace('\\', Path.DirectorySeparatorChar);
                var fullPath = Path.GetFullPath(Path.Combine(webRootPath, relativePath));

                if (fullPath.StartsWith(webRootPath, StringComparison.OrdinalIgnoreCase) && File.Exists(fullPath))
                    File.Delete(fullPath);
            }
            catch
            {
                // Attachment files are best-effort cleanup; database delete already succeeded.
            }
        }
    }
}
