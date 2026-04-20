---
title: "Project Management Module Implementation Plan"
description: "Bổ sung module quản lí dự án, task/subtask, checklist, phân công thành viên và theo dõi tiến độ tổng thể theo mô hình MS Project thu gọn"
status: pending
priority: P1
effort: 18d
issue:
branch: main
tags: [feature, backend, frontend, database, auth, api]
blockedBy: []
blocks: []
created: 2026-04-18
---

# Project Management Module Implementation Plan

## Overview

Mục tiêu: thêm module Quản lí dự án cho hệ thống managerCMN với năng lực cốt lõi tương tự Microsoft Project ở mức MVP+:
- Quản lí Project, thành viên dự án, vai trò trong dự án.
- Phân cấp quyền bắt buộc theo dự án:
  - Người tạo dự án = Admin tổng dự án (ProjectOwner).
  - ProjectOwner phân quyền ProjectManager.
  - ProjectManager được phân công tiếp nhân viên (ProjectStaff) vào task/công việc.
- Quản lí Work Breakdown Structure: Task, Subtask nhiều cấp, phụ thuộc cơ bản.
- Phân công nhiều người trên 1 task; theo dõi status, checklist, hạn, % hoàn thành.
- Tính tiến độ tự động theo cây công việc và tổng tiến độ dự án.
- Bảo mật dữ liệu: chỉ người thuộc project mới thấy dữ liệu trong project đó.
- Cho phép thêm/bớt thành viên vào project và giao việc linh hoạt.

Phạm vi ưu tiên theo YAGNI/KISS:
- Không làm biểu đồ Gantt drag-drop ở giai đoạn 1.
- Không làm critical path engine phức tạp ở giai đoạn 1.
- Có thể bổ sung giai đoạn 2 sau khi vận hành ổn định.

## Scope Challenge

- Existing code có thể tái sử dụng:
  - Pattern entity-repository-service-controller-view đang ổn định.
  - Pattern membership/visibility có ở Ticket (creator/recipient).
  - Pattern permission seed + Settings phân quyền đã có sẵn.
- Minimum change set:
  - Bắt buộc: model dữ liệu project/task/member/checklist, ACL theo project member, progress rollup, UI CRUD cơ bản.
  - Bắt buộc: enforce delegation chain ProjectOwner -> ProjectManager -> ProjectStaff.
  - Hoãn: timeline nâng cao, automation rule phức tạp, workload heatmap.
- Complexity check:
  - Ước tính > 8 files và > 2 service mới là bắt buộc vì đây là module domain mới.
  - Chia 8 phase để giảm rủi ro migration + bảo mật + UI.
- Scope mode chọn: HOLD SCOPE (đúng nhu cầu user, không cắt nhỏ quá mức).

## Cross-Plan Dependencies

Không có plan đang mở liên quan trong thư mục plans tại thời điểm tạo.

## Phases

| Phase | Name | Status |
|-------|------|--------|
| 1 | [Domain Definition and Bounded Scope](./phase-01-domain-definition-and-bounded-scope.md) | Pending |
| 2 | [Data Model and EF Core Migration](./phase-02-data-model-and-efcore-migration.md) | Pending |
| 3 | [Project Membership Security and Permissions](./phase-03-project-membership-security-and-permissions.md) | Pending |
| 4 | [Task Breakdown Checklist and Assignment](./phase-04-task-breakdown-checklist-and-assignment.md) | Pending |
| 5 | [Progress Calculation and Rollup Engine](./phase-05-progress-calculation-and-rollup-engine.md) | Pending |
| 6 | [Application Services and Controller APIs](./phase-06-application-services-and-controller-apis.md) | Pending |
| 7 | [Razor UI Workflow and Project Dashboard](./phase-07-razor-ui-workflow-and-project-dashboard.md) | Pending |
| 8 | [Testing Data Migration and Rollout](./phase-08-testing-data-migration-and-rollout.md) | Pending |

## Key Dependencies

- ASP.NET Core MVC + Razor existing stack.
- EF Core SQL Server migration pipeline hiện tại.
- Existing permission system (Permission + RolePermission + SettingsController).
- Existing employee identity claim EmployeeId để map người dùng hiện tại.

## Success Criteria (Global)

- Người tạo project tự động là ProjectOwner ngay khi tạo project.
- Chỉ ProjectOwner được bổ nhiệm/thu hồi ProjectManager.
- ProjectManager có thể thêm ProjectStaff vào project và phân công task cho ProjectStaff.
- ProjectManager không được nâng quyền người khác thành ProjectOwner/ProjectManager.
- User ngoài project không đọc được detail project/task/checklist của project đó.
- Mỗi task có thể có nhiều assignee; mỗi assignee nhìn đúng task được giao trong project thuộc quyền.
- Hệ thống hiển thị:
  - tiến độ từng subtask,
  - tiến độ task cha,
  - tiến độ toàn project,
  - checklist completion.
- Khi add thành viên mới vào project, người đó truy cập được dữ liệu project ngay.
- Khi remove thành viên, quyền truy cập dữ liệu project bị thu hồi ngay lập tức.

## Risks

- Hiệu năng rollup nếu tính đệ quy real-time cho project lớn.
- Rủi ro lộ dữ liệu nếu quên filter theo project membership ở 1 endpoint.
- Dữ liệu không nhất quán nếu cho sửa trực tiếp percent hoàn thành và đồng thời có checklist/subtask auto-calc.

## Mitigation

- Chuẩn hóa 1 nguồn sự thật cho progress theo rule ưu tiên.
- Tạo query helper dùng chung để enforce membership filter.
- Cache/denormalize progress ở bảng ProjectTask khi cập nhật sự kiện.

## Cook Handoff

/ck:cook e:\Study\C#\managerCMN\plans\260418-1645-project-management-module\plan.md
