# Phase 06 - Application Services and Controller APIs

## Context Links

- Controller pattern: e:\Study\C#\managerCMN\managerCMN\managerCMN\Controllers\TicketController.cs
- Service registration: e:\Study\C#\managerCMN\managerCMN\managerCMN\Program.cs

## Overview

- Priority: P1
- Status: Pending
- Mục tiêu: cung cấp API/controller đầy đủ cho project module, đảm bảo code path nào cũng đi qua security + domain validation.

## Requirements

- Functional:
  - ProjectController:
    - Index/MyProjects
    - Details
    - Create/Edit/Archive
    - ManageManagers (appoint/revoke ProjectManager - Owner only)
    - ManageMembers (add/remove/update ProjectStaff/Viewer)
  - ProjectTaskController:
    - CreateTask/CreateSubtask
    - UpdateTask/MoveTask
    - AssignMembers
    - Checklist CRUD + toggle
    - UpdateStatus/UpdateProgress
  - Optional API endpoints cho AJAX realtime refresh.
- Non-functional:
  - Trả lỗi chuẩn hóa bằng TempData/ProblemDetails tùy context MVC/API.

## Architecture

- Mỗi action flow:
  - Resolve current employee -> access guard -> service method -> response.
- Tách ViewModel riêng cho form tạo/sửa task/project/member.

## Related Code Files

- Create:
  - e:\Study\C#\managerCMN\managerCMN\managerCMN\Controllers\ProjectController.cs
  - e:\Study\C#\managerCMN\managerCMN\managerCMN\Controllers\ProjectTaskController.cs
  - e:\Study\C#\managerCMN\managerCMN\managerCMN\Services\Interfaces\IProjectService.cs
  - e:\Study\C#\managerCMN\managerCMN\managerCMN\Services\Implementations\ProjectService.cs
- Modify:
  - e:\Study\C#\managerCMN\managerCMN\managerCMN\Program.cs
  - e:\Study\C#\managerCMN\managerCMN\managerCMN\Filters\SystemLogActionFilter.cs

## Implementation Steps

1. Đăng ký DI cho repo/service project module.
2. Tạo controller và route thống nhất.
3. Áp guard membership vào toàn bộ action.
4. Áp guard phân cấp quyền:
  - endpoint ManageManagers chỉ cho ProjectOwner,
  - endpoint ManageMembers cho ProjectOwner/ProjectManager trong giới hạn role.
5. Chuẩn hóa log module name là Project/ProjectTask.
6. Tạo endpoint hỗ trợ UI tree load/lazy load.

## Todo List

- [ ] Controller và service compile pass.
- [ ] DI registration đầy đủ.
- [ ] Toàn bộ action có security guard.
- [ ] Endpoint bổ nhiệm quản lí chỉ Owner gọi được.
- [ ] Log hệ thống phản ánh đúng actor và action.

## Success Criteria

- Luồng CRUD project-task-member-checklist chạy đầy đủ trên UI.
- Không có endpoint bỏ lọt authorization.

## Risk Assessment

- Controller phình to nếu không tách service/command hợp lý.

## Security Considerations

- Validate anti-forgery cho toàn bộ POST.
- Verify assignee/project member ownership trước mọi update.

## Next Steps

- Sang phase 07 để hoàn thiện UX và dashboard tiến độ.
