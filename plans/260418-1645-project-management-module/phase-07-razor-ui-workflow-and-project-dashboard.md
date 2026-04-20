# Phase 07 - Razor UI Workflow and Project Dashboard

## Context Links

- Shared layout menu: e:\Study\C#\managerCMN\managerCMN\managerCMN\Views\Shared\_Layout.cshtml
- Existing style system: e:\Study\C#\managerCMN\managerCMN\managerCMN\wwwroot\css\site.css

## Overview

- Priority: P1
- Status: Pending
- Mục tiêu: cung cấp giao diện quản lí dự án dễ dùng, có cây công việc, checklist, phân công người, và dashboard tiến độ tổng.

## Requirements

- Functional:
  - Trang danh sách dự án theo membership của user.
  - Trang quản trị thành viên theo phân cấp:
    - ProjectOwner: bổ nhiệm/thu hồi ProjectManager.
    - ProjectManager: thêm/bớt ProjectStaff, phân công task.
  - Trang chi tiết dự án gồm:
    - progress bar tổng,
    - danh sách task dạng tree,
    - bộ lọc theo assignee/status/deadline,
    - thống kê quá hạn.
  - Form thêm member vào project.
  - Form tạo task/subtask/checklist ngay trong project.
- Non-functional:
  - Tối ưu thao tác cho desktop; mobile tối thiểu đọc và cập nhật checklist.

## Architecture

- Razor views + partial views:
  - Project/Index.cshtml
  - Project/Details.cshtml
  - Project/_TaskTree.cshtml
  - Project/_TaskFormModal.cshtml
  - Project/_MemberManagement.cshtml
- JS nhẹ cho expand/collapse tree và update checklist async.

## Related Code Files

- Create:
  - e:\Study\C#\managerCMN\managerCMN\managerCMN\Views\Project\Index.cshtml
  - e:\Study\C#\managerCMN\managerCMN\managerCMN\Views\Project\Details.cshtml
  - e:\Study\C#\managerCMN\managerCMN\managerCMN\Views\Project\_TaskTree.cshtml
  - e:\Study\C#\managerCMN\managerCMN\managerCMN\wwwroot\js\project-management.js
  - e:\Study\C#\managerCMN\managerCMN\managerCMN\wwwroot\css\project-management.css
- Modify:
  - e:\Study\C#\managerCMN\managerCMN\managerCMN\Views\Shared\_Layout.cshtml

## Implementation Steps

1. Thêm menu entry Project theo permission/membership.
2. Xây trang My Projects + filter nhanh.
3. Xây trang Project Details với widget KPI và task tree.
4. Tích hợp modal tạo/sửa task và quản lí member.
5. Tích hợp cập nhật checklist + refresh progress theo AJAX.
6. Khóa/ẩn action theo role để tránh thao tác vượt quyền ngay trên UI.

## Todo List

- [ ] Menu project hiển thị đúng quyền.
- [ ] UI task tree usable và rõ trạng thái.
- [ ] Checklist update realtime ổn định.
- [ ] Dashboard progress phản ánh đúng dữ liệu backend.
- [ ] UI phân quyền Owner/Manager/Staff nhất quán với backend guard.

## Success Criteria

- User trong project quản lí được project end-to-end trên UI.
- User ngoài project không thấy dữ liệu project trong bất kỳ màn hình nào.

## Risk Assessment

- Task tree render full có thể chậm với dự án quá lớn.

## Security Considerations

- Không render dữ liệu task/member nếu guard backend fail.
- Mọi AJAX endpoint dùng anti-forgery token.

## Next Steps

- Sang phase 08 cho test, rollout, tài liệu vận hành.
