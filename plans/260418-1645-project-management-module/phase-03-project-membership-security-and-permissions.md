# Phase 03 - Project Membership Security and Permissions

## Context Links

- AuthZ handler: e:\Study\C#\managerCMN\managerCMN\managerCMN\Authorization\PermissionAuthorizationHandler.cs
- Program policy setup: e:\Study\C#\managerCMN\managerCMN\managerCMN\Program.cs
- Permission seed source: e:\Study\C#\managerCMN\managerCMN\managerCMN\Data\ApplicationDbContext.cs
- Admin settings: e:\Study\C#\managerCMN\managerCMN\managerCMN\Controllers\SettingsController.cs

## Overview

- Priority: P1
- Status: Pending
- Mục tiêu: enforce nguyên tắc chỉ thành viên project mới thấy dữ liệu project; đồng thời enforce phân cấp ProjectOwner -> ProjectManager -> ProjectStaff.

## Requirements

- Functional:
  - Permission mới:
    - Project.View
    - Project.Create
    - Project.Edit
    - Project.ManageMembers
    - Project.ManageManagers
    - ProjectTask.Manage
    - ProjectTask.UpdateProgress
  - Rule truy cập:
    - Người tạo project tự động là ProjectOwner.
    - User phải là ProjectMember để xem project.
    - Chỉ ProjectOwner được bổ nhiệm/thu hồi ProjectManager.
    - ProjectOwner và ProjectManager được thêm/xóa ProjectStaff/ProjectViewer.
    - ProjectStaff chỉnh task được phân công hoặc được cấp quyền task trong project.
    - ProjectManager không được chỉnh role của ProjectOwner và không được tự tạo thêm ProjectManager.
- Non-functional:
  - Membership check dùng chung, không lặp ở từng controller action.

## Architecture

- Tạo service/hàm guard thống nhất:
  - EnsureProjectMembership(projectId, employeeId)
  - EnsureCanManageMembers(projectId, employeeId)
  - EnsureCanManageManagers(projectId, employeeId)
  - EnsureCanEditTask(taskId, employeeId)
- Tất cả repository query có filter ProjectId + membership join khi trả data user-facing.

## Related Code Files

- Modify:
  - e:\Study\C#\managerCMN\managerCMN\managerCMN\Program.cs
  - e:\Study\C#\managerCMN\managerCMN\managerCMN\Data\ApplicationDbContext.cs
  - e:\Study\C#\managerCMN\managerCMN\managerCMN\Controllers\SettingsController.cs
- Create:
  - e:\Study\C#\managerCMN\managerCMN\managerCMN\Authorization\ProjectMembershipRequirement.cs
  - e:\Study\C#\managerCMN\managerCMN\managerCMN\Authorization\ProjectMembershipAuthorizationHandler.cs
  - e:\Study\C#\managerCMN\managerCMN\managerCMN\Services\Interfaces\IProjectAccessService.cs
  - e:\Study\C#\managerCMN\managerCMN\managerCMN\Services\Implementations\ProjectAccessService.cs

## Implementation Steps

1. Bổ sung permission key mới vào seed và default role map.
2. Thêm policy authorization cho project module.
3. Viết access service dùng trong controller/service nghiệp vụ.
4. Cập nhật màn Settings để cấu hình quyền mới theo role.
5. Cài guard role transition để chặn escalation trái phép.
6. Hard-test các case bypass URL trực tiếp với user không thuộc project.

## Todo List

- [ ] Permission keys được seed và map role mặc định.
- [ ] Policy mới đăng ký trong Program.
- [ ] Membership guard áp vào mọi endpoint project/task/checklist.
- [ ] Guard escalation chặn Manager tự nâng quyền pass.
- [ ] Test chặn truy cập trái phép pass.

## Success Criteria

- User không thuộc project luôn nhận Forbid/NotFound phù hợp khi truy cập URL project.
- ProjectOwner bổ nhiệm ProjectManager thành công theo quy tắc.
- ProjectManager thêm ProjectStaff và phân công công việc thành công theo quy tắc.

## Risk Assessment

- Quên gắn guard ở endpoint Ajax nhỏ gây hở dữ liệu.

## Security Considerations

- Tránh trả thông báo lỗi lộ existence của project cho user ngoài project.
- Log lại mọi hành động add/remove member vào SystemLog.

## Next Steps

- Sang phase 04 để hiện thực WBS, checklist, assignment theo security đã chốt.
