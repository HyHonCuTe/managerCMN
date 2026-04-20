# Phase 05 - Progress Calculation and Rollup Engine

## Context Links

- Existing dashboard service pattern: e:\Study\C#\managerCMN\managerCMN\managerCMN\Services\Implementations\DashboardService.cs
- Existing date/reporting style: e:\Study\C#\managerCMN\managerCMN\managerCMN\Controllers\DashboardController.cs

## Overview

- Priority: P1
- Status: Pending
- Mục tiêu: tính tiến độ task con, task cha, project tổng hợp; hiển thị số liệu đáng tin và nhất quán.

## Requirements

- Functional:
  - Task leaf auto progress theo checklist ratio nếu có checklist.
  - Task parent auto progress theo weighted average từ con.
  - Project progress theo weighted average từ root tasks.
  - Cập nhật % theo event-driven khi task/checklist/assignment/status thay đổi.
  - Hiển thị KPI:
    - % hoàn thành dự án,
    - số task quá hạn,
    - tốc độ hoàn thành tuần.
- Non-functional:
  - Sai số làm tròn chuẩn hóa 2 chữ số thập phân.
  - Query progress không quá nặng cho project khoảng 2,000 tasks.

## Architecture

- ProgressRuleEngine service:
  - RecalculateTaskProgress(taskId)
  - BubbleUpParentProgress(taskId)
  - RecalculateProjectProgress(projectId)
- Dùng transactional update để tránh race condition khi update checklist song song.

## Related Code Files

- Create:
  - e:\Study\C#\managerCMN\managerCMN\managerCMN\Services\Interfaces\IProjectProgressService.cs
  - e:\Study\C#\managerCMN\managerCMN\managerCMN\Services\Implementations\ProjectProgressService.cs
  - e:\Study\C#\managerCMN\managerCMN\managerCMN\Models\ViewModels\ProjectProgressSummaryViewModel.cs
- Modify:
  - e:\Study\C#\managerCMN\managerCMN\managerCMN\Models\Entities\Project.cs
  - e:\Study\C#\managerCMN\managerCMN\managerCMN\Models\Entities\ProjectTask.cs
  - e:\Study\C#\managerCMN\managerCMN\managerCMN\Services\Implementations\ProjectTaskService.cs

## Implementation Steps

1. Định nghĩa công thức progress thống nhất trong service riêng.
2. Áp dụng recalc tại các điểm thay đổi dữ liệu.
3. Tạo scheduled health-check command để backfill progress lệch (nếu có).
4. Thêm endpoint/API lấy summary progress cho dashboard.
5. Benchmark trên dataset giả lập dự án lớn.

## Todo List

- [ ] Engine progress hoạt động đúng trên dữ liệu mẫu.
- [ ] Parent-project rollup đúng công thức.
- [ ] Có job/command kiểm tra consistency.
- [ ] Có benchmark baseline hiệu năng.

## Success Criteria

- Progress hiển thị nhất quán giữa màn task và màn project.
- Không có trạng thái hoàn thành ảo khi checklist chưa đạt.

## Risk Assessment

- Rule chồng chéo Auto/Manual làm người dùng khó hiểu.

## Security Considerations

- Chỉ expose summary của project user có membership.

## Next Steps

- Sang phase 06 để public logic qua service và controller/API.
