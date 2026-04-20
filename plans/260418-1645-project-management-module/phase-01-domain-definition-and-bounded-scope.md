# Phase 01 - Domain Definition and Bounded Scope

## Context Links

- Solution: e:\Study\C#\managerCMN\managerCMN\managerCMN.slnx
- Main project: e:\Study\C#\managerCMN\managerCMN\managerCMN\managerCMN.csproj
- Current domain patterns:
  - e:\Study\C#\managerCMN\managerCMN\managerCMN\Models\Entities\Ticket.cs
  - e:\Study\C#\managerCMN\managerCMN\managerCMN\Models\Entities\Request.cs

## Overview

- Priority: P1
- Status: Pending
- Mục tiêu: chốt domain model, business rule và phạm vi MVP để cả team code cùng một ngôn ngữ domain.

## Key Insights

- Hệ thống hiện dùng EmployeeId làm định danh nghiệp vụ xuyên module.
- Menu hiện phân quyền chủ yếu theo role; cần chuyển dần sang permission + membership để đảm bảo project privacy.

## Requirements

- Functional:
  - Định nghĩa rõ các thực thể: Project, ProjectMember, ProjectTask, TaskAssignment, TaskChecklistItem, TaskDependency, ProjectActivityLog.
  - Định nghĩa role hierarchy cố định trong project:
    - ProjectOwner: admin tổng dự án, luôn là người tạo dự án khi khởi tạo.
    - ProjectManager: do ProjectOwner bổ nhiệm.
    - ProjectStaff: do ProjectOwner/ProjectManager phân vào project để thực thi công việc.
    - ProjectViewer: chỉ xem.
  - Định nghĩa trạng thái project/task/subtask/checklist.
  - Định nghĩa rule tính % hoàn thành.
- Non-functional:
  - Rule phải đủ đơn giản để debug.
  - Thuật ngữ dùng nhất quán giữa DB, service, UI.

## Architecture

- DDD-lite trong monolith hiện tại:
  - Project aggregate root quản lý thành viên và metadata.
  - Task tree thuộc project, có parent-child theo adjacency list.
  - Assignment và Checklist là child entity của task.

## Related Code Files

- Modify:
  - e:\Study\C#\managerCMN\managerCMN\managerCMN\README.md
- Create:
  - e:\Study\C#\managerCMN\managerCMN\managerCMN\Models\Entities\Project.cs
  - e:\Study\C#\managerCMN\managerCMN\managerCMN\Models\Entities\ProjectMember.cs
  - e:\Study\C#\managerCMN\managerCMN\managerCMN\Models\Entities\ProjectTask.cs
  - e:\Study\C#\managerCMN\managerCMN\managerCMN\Models\Entities\ProjectTaskAssignment.cs
  - e:\Study\C#\managerCMN\managerCMN\managerCMN\Models\Entities\ProjectTaskChecklistItem.cs
  - e:\Study\C#\managerCMN\managerCMN\managerCMN\Models\Entities\ProjectTaskDependency.cs

## Implementation Steps

1. Viết domain glossary cho module project trong README nội bộ.
2. Chốt enum trạng thái và priority cho project/task.
3. Chốt rule percent complete theo thứ tự ưu tiên:
   - Nếu task có con: progress task cha = weighted average theo effort của task con.
   - Nếu task lá có checklist: progress = checklist done ratio.
   - Nếu task lá không checklist: cho nhập manual progress.
4. Chốt matrix quyền theo project role: ProjectOwner, ProjectManager, ProjectStaff, ProjectViewer.
5. Chốt rule delegation:
  - Creator auto giữ vai trò ProjectOwner.
  - Chỉ ProjectOwner được grant/revoke ProjectManager.
  - ProjectManager chỉ được add/remove ProjectStaff/ProjectViewer và phân công task.

## Todo List

- [ ] Domain glossary được duyệt.
- [ ] Rule progress được duyệt.
- [ ] Ma trận vai trò trong project được duyệt.
- [ ] Rule phân cấp ProjectOwner -> ProjectManager -> ProjectStaff được duyệt.
- [ ] Danh sách trường bắt buộc của từng entity được duyệt.

## Success Criteria

- Team thống nhất thuật ngữ và quy tắc trước khi tạo migration.
- Không có mâu thuẫn rule giữa service và UI đặc tả.

## Risk Assessment

- Scope trôi nếu cố nhồi thêm Gantt critical-path từ phase 1.

## Security Considerations

- Tất cả nghiệp vụ đọc/ghi phải giả định membership check là bắt buộc.

## Next Steps

- Chuyển sang phase 02 để hiện thực schema EF Core và migration.
