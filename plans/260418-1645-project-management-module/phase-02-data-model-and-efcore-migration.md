# Phase 02 - Data Model and EF Core Migration

## Context Links

- DbContext: e:\Study\C#\managerCMN\managerCMN\managerCMN\Data\ApplicationDbContext.cs
- Migrations folder: e:\Study\C#\managerCMN\managerCMN\managerCMN\Migrations
- UoW contract: e:\Study\C#\managerCMN\managerCMN\managerCMN\Repositories\Interfaces\IUnitOfWork.cs

## Overview

- Priority: P1
- Status: Pending
- Mục tiêu: bổ sung schema dữ liệu chuẩn hóa để lưu project, thành viên, cây task, checklist, phụ thuộc.

## Requirements

- Functional:
  - Tạo bảng project và task tree có self-reference ParentTaskId.
  - Tạo bảng assignment many-to-many task-employee.
  - Tạo checklist item theo task.
  - Tạo bảng dependency giữa task (predecessor/successor).
- Non-functional:
  - Có index tối ưu các truy vấn phổ biến: ProjectId, ParentTaskId, Status, DueDate.
  - Ràng buộc unique tránh duplicate assignment/checklist order.

## Architecture

- Mô hình quan hệ đề xuất:
  - Project (1) - (n) ProjectMember
  - Project (1) - (n) ProjectTask
  - ProjectTask (1) - (n) ProjectTaskChecklistItem
  - ProjectTask (1) - (n) ProjectTaskAssignment
  - ProjectTask (n) - (n) ProjectTask qua ProjectTaskDependency

## Related Code Files

- Modify:
  - e:\Study\C#\managerCMN\managerCMN\managerCMN\Data\ApplicationDbContext.cs
  - e:\Study\C#\managerCMN\managerCMN\managerCMN\Repositories\Interfaces\IUnitOfWork.cs
  - e:\Study\C#\managerCMN\managerCMN\managerCMN\Repositories\Implementations\UnitOfWork.cs
- Create:
  - e:\Study\C#\managerCMN\managerCMN\managerCMN\Models\Entities\Project*.cs
  - e:\Study\C#\managerCMN\managerCMN\managerCMN\Repositories\Interfaces\IProjectRepository.cs
  - e:\Study\C#\managerCMN\managerCMN\managerCMN\Repositories\Interfaces\IProjectTaskRepository.cs
  - e:\Study\C#\managerCMN\managerCMN\managerCMN\Repositories\Implementations\ProjectRepository.cs
  - e:\Study\C#\managerCMN\managerCMN\managerCMN\Repositories\Implementations\ProjectTaskRepository.cs
  - e:\Study\C#\managerCMN\managerCMN\managerCMN\Migrations\<timestamp>_AddProjectManagementModule.cs

## Implementation Steps

1. Khai báo DbSet mới trong ApplicationDbContext.
2. Cấu hình Fluent API:
   - FK, OnDelete behavior,
   - composite unique index,
   - precision cho EstimatedHours/Progress.
3. Seed tối thiểu enum mapping nếu cần bảng lookup.
4. Tạo migration và rà soát script SQL generated.
5. Kiểm thử migrate up/down trên môi trường local sạch.

## Todo List

- [ ] DbSet + entity mapping đầy đủ.
- [ ] Migration apply thành công.
- [ ] Snapshot cập nhật sạch.
- [ ] Có index cho truy vấn tiến độ và membership.

## Success Criteria

- Database tạo đủ bảng project module, không phá vỡ module hiện hữu.
- Migrate chạy được với dữ liệu hiện tại.

## Risk Assessment

- Xung đột naming hoặc cascade delete gây mất dữ liệu task con.

## Security Considerations

- Không cascade xóa project trực tiếp nếu chưa có soft delete strategy.

## Next Steps

- Sang phase 03 để cài security layer theo project membership.
