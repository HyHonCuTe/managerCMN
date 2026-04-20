# Phase 08 - Testing Data Migration and Rollout

## Context Links

- Solution file: e:\Study\C#\managerCMN\managerCMN\managerCMN.slnx
- Deployment docs: e:\Study\C#\managerCMN\managerCMN\DEPLOY.md
- Existing manual docs: e:\Study\C#\managerCMN\docs\manual

## Overview

- Priority: P1
- Status: Pending
- Mục tiêu: kiểm thử đầy đủ, bảo đảm migration an toàn, rollout không gián đoạn module hiện hữu.

## Requirements

- Functional test coverage:
  - Project CRUD.
  - Membership add/remove + role update.
  - Delegation chain:
    - Creator auto ProjectOwner.
    - ProjectOwner appoint/revoke ProjectManager.
    - ProjectManager add ProjectStaff và phân công task.
    - ProjectManager không được nâng quyền Manager/Owner.
  - Task/subtask/checklist/assignment lifecycle.
  - Progress rollup leaf-parent-project.
  - Privacy test user ngoài project.
- Non-functional:
  - Build pass.
  - Migration pass trên staging clone dữ liệu thực.
  - Không regression các module cũ (Request/Ticket/Asset).

## Architecture

- Test layers:
  - Service-level tests cho progress engine.
  - Controller integration tests cho authorization path.
  - Manual UAT checklist cho luồng người dùng.

## Related Code Files

- Create:
  - e:\Study\C#\managerCMN\managerCMN\managerCMN\Tests\ProjectManagement\*.cs
  - e:\Study\C#\managerCMN\docs\manual\HUONG_DAN_QUAN_LY_DU_AN.md
- Modify:
  - e:\Study\C#\managerCMN\README.md
  - e:\Study\C#\managerCMN\managerCMN\README.md

## Implementation Steps

1. Viết test case matrix theo role:
  - ProjectOwner, ProjectManager, ProjectStaff, User ngoài project.
2. Chạy migration dry-run trên DB copy và ghi nhận thời gian downtime dự kiến.
3. Chạy smoke test các module cũ trọng yếu sau deploy.
4. Viết runbook rollback:
   - rollback app version,
   - rollback migration (nếu cần),
   - data recovery points.
5. Viết tài liệu hướng dẫn sử dụng module project cho admin và nhân viên.

## Todo List

- [ ] Test matrix đầy đủ theo role và privacy.
- [ ] Test matrix escalation/delegation pass.
- [ ] Migration dry-run có biên bản.
- [ ] Smoke test module cũ pass.
- [ ] Runbook rollback rõ ràng.
- [ ] Tài liệu user hoàn tất.

## Success Criteria

- Release module project không làm lỗi module hiện hữu.
- Dữ liệu project và tiến độ nhất quán sau deploy.

## Risk Assessment

- Migration dài nếu dữ liệu task lớn ngay từ đầu rollout.

## Security Considerations

- Test đầy đủ IDOR/broken access control cho project/task endpoints.
- Rà soát logging tránh lộ dữ liệu project ở log dùng chung.

## Next Steps

- Triển khai pilot cho 1-2 phòng ban trước khi mở toàn hệ thống.
