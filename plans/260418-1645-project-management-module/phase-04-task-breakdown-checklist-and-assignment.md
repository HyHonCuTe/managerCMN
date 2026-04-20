# Phase 04 - Task Breakdown Checklist and Assignment

## Context Links

- Ticket assignment reference: e:\Study\C#\managerCMN\managerCMN\managerCMN\Models\Entities\TicketRecipient.cs
- Ticket workflow controller: e:\Study\C#\managerCMN\managerCMN\managerCMN\Controllers\TicketController.cs

## Overview

- Priority: P1
- Status: Pending
- Mục tiêu: xây WBS cho project gồm task/subtask/checklist, phân công nhiều người và quản lí trạng thái công việc.

## Requirements

- Functional:
  - Tạo task gốc và subtask nhiều cấp (giới hạn depth ban đầu: 5).
  - Phân công 1-n assignee cho task.
  - ProjectManager được phép phân công ProjectStaff vào task trong project do họ quản lí.
  - Checklist trong task với trạng thái done/undone.
  - Trường thời gian: StartDate, DueDate, EstimatedHours, ActualHours.
  - Trường quản trị: Priority, Status, ProgressMode (Auto/Manual).
- Non-functional:
  - Không cho tạo vòng lặp parent-child.
  - Không cho dependency tự tham chiếu.

## Architecture

- Adjacency list cho task tree:
  - ProjectTaskId
  - ParentTaskId nullable
- Assignment table tách riêng để mở rộng workload theo user sau này.
- Checklist item có thứ tự hiển thị và cờ hoàn thành.

## Related Code Files

- Create:
  - e:\Study\C#\managerCMN\managerCMN\managerCMN\Models\ViewModels\ProjectTask*.cs
  - e:\Study\C#\managerCMN\managerCMN\managerCMN\Services\Interfaces\IProjectTaskService.cs
  - e:\Study\C#\managerCMN\managerCMN\managerCMN\Services\Implementations\ProjectTaskService.cs
  - e:\Study\C#\managerCMN\managerCMN\managerCMN\Repositories\Interfaces\IProjectTaskAssignmentRepository.cs
  - e:\Study\C#\managerCMN\managerCMN\managerCMN\Repositories\Interfaces\IProjectTaskChecklistRepository.cs
- Modify:
  - e:\Study\C#\managerCMN\managerCMN\managerCMN\Repositories\Interfaces\IUnitOfWork.cs
  - e:\Study\C#\managerCMN\managerCMN\managerCMN\Repositories\Implementations\UnitOfWork.cs

## Implementation Steps

1. Tạo command/service cho create/update/delete task/subtask.
2. Tạo command/service cho assignment nhiều người.
3. Tạo command/service cho checklist CRUD + toggle done.
4. Cài validation business:
   - due date không nhỏ hơn start date,
   - parent task phải cùng project,
   - assignee phải là member của project.
5. Cài validation delegation:
  - Chỉ ProjectOwner/ProjectManager được gán assignee.
  - ProjectManager chỉ gán được ProjectStaff trong cùng project.
6. Thêm activity log cho thay đổi task/assignment/checklist.

## Todo List

- [ ] CRUD task/subtask hoàn chỉnh.
- [ ] Multiple assignee hoạt động ổn định.
- [ ] Checklist hoạt động ổn định.
- [ ] Rule phân công theo phân cấp quyền pass.
- [ ] Validation rule chống dữ liệu bẩn pass.

## Success Criteria

- Có thể quản lí đầy đủ công việc dự án theo cây.
- Có thể phân chia người trong task rõ ràng.

## Risk Assessment

- Đệ quy sâu gây nặng query khi load cây task lớn.

## Security Considerations

- Chỉ thành viên project mới thao tác task/checklist của project đó.

## Next Steps

- Sang phase 05 để tính progress tự động từ dữ liệu phase 04.
