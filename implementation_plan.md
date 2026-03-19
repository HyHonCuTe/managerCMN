# Thiết kế lại hệ thống Ticket — Email-like Ticket System

Nâng cấp hệ thống Ticket từ đơn giản (tạo → gán → giải quyết → đóng) thành hệ thống giao tiếp giống email: có deadline, mức độ ưu tiên/quan trọng, đính kèm file, chọn nhiều người nhận, forward với nội dung bổ sung, và báo cáo trạng thái từ người nhận.

## User Review Required

> [!IMPORTANT]
> Hệ thống Ticket sẽ thay đổi lớn cả model lẫn UI. Dữ liệu ticket cũ có thể cần migration thủ công. Nếu DB hiện tại có dữ liệu ticket quan trọng, cần backup trước.

> [!WARNING]
> Phần forward tạo chuỗi TicketMessage giống email thread. Mỗi lần forward sẽ tạo `TicketMessage` mới kèm nội dung cũ, tương tự "Reply/Forward" trong email.

## Proposed Changes

### 1. New Enums

#### [NEW] [TicketUrgency.cs](file:///e:/Study/C%23/managerCMN/managerCMN/managerCMN/Models/Enums/TicketUrgency.cs)
Mức độ gấp gáp: `Normal`, `Urgent`, `VeryUrgent`, `Immediate`

#### [MODIFY] [TicketPriority.cs](file:///e:/Study/C%23/managerCMN/managerCMN/managerCMN/Models/Enums/TicketPriority.cs)
Giữ nguyên (Low, Medium, High, Critical) — đại diện mức độ quan trọng.

#### [MODIFY] [TicketStatus.cs](file:///e:/Study/C%23/managerCMN/managerCMN/managerCMN/Models/Enums/TicketStatus.cs)
Thêm `Forwarded`, `OnHold`, `Cancelled` — để hỗ trợ forward flow và suspend.

---

### 2. New/Modified Entities

#### [MODIFY] [Ticket.cs](file:///e:/Study/C%23/managerCMN/managerCMN/managerCMN/Models/Entities/Ticket.cs)
Thêm:
- `Deadline` (DateTime?) — hạn deadline
- `Urgency` (TicketUrgency) — mức độ gấp gáp
- Navigation: `Recipients`, `Messages`, `Attachments`
- Bỏ `AssignedTo`/[Assignee](file:///e:/Study/C%23/managerCMN/managerCMN/managerCMN/Services/Implementations/TicketService.cs#26-28) (thay bằng `TicketRecipient`)

#### [NEW] [TicketRecipient.cs](file:///e:/Study/C%23/managerCMN/managerCMN/managerCMN/Models/Entities/TicketRecipient.cs)
Quan hệ N-N giữa Ticket và Employee:
- `TicketRecipientId`, `TicketId`, [EmployeeId](file:///e:/Study/C%23/managerCMN/managerCMN/managerCMN/Controllers/TicketController.cs#96-101)
- [Status](file:///e:/Study/C%23/managerCMN/managerCMN/managerCMN/Services/Implementations/TicketService.cs#20-22) (Pending/Read/InProgress/Completed/Forwarded)
- `ReadDate`, `CompletedDate`
- Navigation: [Ticket](file:///e:/Study/C%23/managerCMN/managerCMN/managerCMN/Models/Entities/Ticket.cs#6-34), [Employee](file:///e:/Study/C%23/managerCMN/managerCMN/managerCMN/Controllers/TicketController.cs#96-101)

#### [NEW] [TicketMessage.cs](file:///e:/Study/C%23/managerCMN/managerCMN/managerCMN/Models/Entities/TicketMessage.cs)
Mỗi message = 1 lần reply/forward/status update trong thread:
- `TicketMessageId`, `TicketId`
- `SenderId` (Employee FK)
- `Content` — nội dung tin nhắn
- `MessageType` (Reply/Forward/StatusUpdate/Note)
- `ForwardedToId` (Employee FK, nullable) — người nhận forward
- `CreatedDate`
- Navigation: [Ticket](file:///e:/Study/C%23/managerCMN/managerCMN/managerCMN/Models/Entities/Ticket.cs#6-34), `Sender`, `ForwardedTo`, `Attachments`

#### [NEW] [TicketAttachment.cs](file:///e:/Study/C%23/managerCMN/managerCMN/managerCMN/Models/Entities/TicketAttachment.cs)
File đính kèm cho ticket hoặc message:
- `TicketAttachmentId`, `TicketId` (nullable), `TicketMessageId` (nullable)
- `FileName`, `FilePath`, `FileSize`, `ContentType`
- `UploadedById`, `UploadedDate`

#### [NEW] [TicketMessageType.cs](file:///e:/Study/C%23/managerCMN/managerCMN/managerCMN/Models/Enums/TicketMessageType.cs)
`Reply`, `Forward`, `StatusUpdate`, `Note`

---

### 3. Database & Repository

#### [MODIFY] [ApplicationDbContext.cs](file:///e:/Study/C%23/managerCMN/managerCMN/managerCMN/Data/ApplicationDbContext.cs)
- Thêm `DbSet<TicketRecipient>`, `DbSet<TicketMessage>`, `DbSet<TicketAttachment>`
- Cấu hình relationship + cascade rules trong [OnModelCreating](file:///e:/Study/C%23/managerCMN/managerCMN/managerCMN/Data/ApplicationDbContext.cs#54-364)
- Giữ backward compatibility với `AssignedTo` (soft deprecate)

#### [MODIFY] [ITicketRepository.cs](file:///e:/Study/C%23/managerCMN/managerCMN/managerCMN/Repositories/Interfaces/ITicketRepository.cs)
Thêm methods: `GetByRecipientAsync`, `GetTicketWithDetailsAsync` (eager load Recipients, Messages, Attachments)

#### [MODIFY] [TicketRepository.cs](file:///e:/Study/C%23/managerCMN/managerCMN/managerCMN/Repositories/Implementations/TicketRepository.cs)
Implement mới với Include chain cho Messages/Recipients/Attachments.

#### [MODIFY] [IUnitOfWork.cs](file:///e:/Study/C%23/managerCMN/managerCMN/managerCMN/Repositories/Interfaces/IUnitOfWork.cs) + [UnitOfWork.cs](file:///e:/Study/C%23/managerCMN/managerCMN/managerCMN/Repositories/Implementations/UnitOfWork.cs)
Không cần thêm repo riêng — dùng DbContext trực tiếp trong service cho TicketRecipient, TicketMessage, TicketAttachment.

---

### 4. Service Layer

#### [MODIFY] [ITicketService.cs](file:///e:/Study/C%23/managerCMN/managerCMN/managerCMN/Services/Interfaces/ITicketService.cs)
Thêm:
- `GetReceivedTicketsAsync(int employeeId)` — ticket nhận được
- `GetTicketDetailAsync(int ticketId)` — full detail với messages
- `CreateWithRecipientsAsync(Ticket, List<int> recipientIds, List<IFormFile>)` — tạo ticket + chọn người nhận + file
- `ForwardAsync(int ticketId, int senderId, List<int> recipientIds, string content, List<IFormFile>)` — forward
- `ReplyAsync(int ticketId, int senderId, string content, List<IFormFile>)` — reply/thêm nội dung
- `UpdateRecipientStatusAsync(int ticketRecipientId, TicketRecipientStatus)` — cập nhật trạng thái

#### [MODIFY] [TicketService.cs](file:///e:/Study/C%23/managerCMN/managerCMN/managerCMN/Services/Implementations/TicketService.cs)
Implement toàn bộ logic mới, tích hợp [INotificationService](file:///e:/Study/C%23/managerCMN/managerCMN/managerCMN/Services/Interfaces/INotificationService.cs#5-16) để thông báo cho người nhận.

---

### 5. ViewModel

#### [MODIFY] [TicketCreateViewModel.cs](file:///e:/Study/C%23/managerCMN/managerCMN/managerCMN/Models/ViewModels/TicketCreateViewModel.cs)
Thêm: `Deadline`, `Urgency`, `RecipientIds` (List\<int\>), `Attachments` (List\<IFormFile\>)

#### [NEW] [TicketDetailViewModel.cs](file:///e:/Study/C%23/managerCMN/managerCMN/managerCMN/Models/ViewModels/TicketDetailViewModel.cs)
Chứa Ticket + Messages thread + Recipients + Attachments + form reply/forward

#### [NEW] [TicketForwardViewModel.cs](file:///e:/Study/C%23/managerCMN/managerCMN/managerCMN/Models/ViewModels/TicketForwardViewModel.cs)
`TicketId`, `Content`, `RecipientIds`, `Attachments`

---

### 6. Controller

#### [MODIFY] [TicketController.cs](file:///e:/Study/C%23/managerCMN/managerCMN/managerCMN/Controllers/TicketController.cs)
- [Index](file:///e:/Study/C%23/managerCMN/managerCMN/managerCMN/Controllers/AssetController.cs#101-119) — hiển thị tab "Gửi đi" / "Nhận được" / "Tất cả" (admin)
- [Create](file:///e:/Study/C%23/managerCMN/managerCMN/managerCMN/Controllers/TicketController.cs#41-58) (GET) — form tạo ticket với multi-select người nhận, file upload, deadline, urgency
- [Create](file:///e:/Study/C%23/managerCMN/managerCMN/managerCMN/Controllers/TicketController.cs#41-58) (POST) — xử lý tạo + upload file + gửi notification
- [Details](file:///e:/Study/C%23/managerCMN/managerCMN/managerCMN/Controllers/TicketController.cs#32-38) — hiển thị thread giống email, form reply/forward
- `Reply` (POST) — thêm message vào thread
- `Forward` (POST) — forward ticket cho người khác kèm nội dung
- `UpdateStatus` (POST) — người nhận cập nhật trạng thái (đã đọc, đang xử lý, hoàn thành)
- Bỏ [Assign](file:///e:/Study/C%23/managerCMN/managerCMN/managerCMN/Controllers/TicketController.cs#70-78)/[Resolve](file:///e:/Study/C%23/managerCMN/managerCMN/managerCMN/Controllers/TicketController.cs#79-86)/[Close](file:///e:/Study/C%23/managerCMN/managerCMN/managerCMN/Controllers/TicketController.cs#87-95) actions cũ (thay bằng flow mới)
- `DownloadAttachment` — tải file đính kèm

---

### 7. Views (UI Premium)

#### [MODIFY] [Index.cshtml](file:///e:/Study/C%23/managerCMN/managerCMN/managerCMN/Views/Ticket/Index.cshtml)
- Thiết kế lại với tabs: **Gửi đi** / **Nhận được** / **Tất cả** (admin)
- Hiển thị badge deadline (sắp hết hạn/quá hạn), priority, urgency
- Card-based hoặc list view đẹp

#### [MODIFY] [Create.cshtml](file:///e:/Study/C%23/managerCMN/managerCMN/managerCMN/Views/Ticket/Create.cshtml)
- Multi-select người nhận (Select2 hoặc dropdown checkbox)
- Date picker cho deadline
- Priority + Urgency selectors đẹp
- Drag & drop file upload area
- Rich text editor cho mô tả (Summernote)

#### [MODIFY] [Details.cshtml](file:///e:/Study/C%23/managerCMN/managerCMN/managerCMN/Views/Ticket/Details.cshtml)
- **Thread view** giống email: timeline các messages
- Sidebar: info ticket, recipients + trạng thái, deadline countdown
- Form reply inline
- Button forward mở modal
- Danh sách file đính kèm có thể download
- Người nhận có thể cập nhật trạng thái

#### [DELETE] [Assign.cshtml](file:///e:/Study/C%23/managerCMN/managerCMN/managerCMN/Views/Ticket/Assign.cshtml)
Không cần nữa — chọn người nhận ngay khi tạo ticket.

---

### 8. EF Migration

- Tạo migration mới cho schema changes
- Giữ backward compat: `AssignedTo` nullable, dữ liệu cũ vẫn tồn tại

## Verification Plan

### Automated Tests
- `dotnet build` — đảm bảo compile thành công
- `dotnet ef migrations add TicketSystemRedesign` — migration tạo đúng

### Manual Verification
1. Truy cập `/Ticket` → Kiểm tra tab layout (Gửi đi/Nhận được)
2. Truy cập `/Ticket/Create` → Kiểm tra form mới có multi-select người nhận, deadline, urgency, file upload
3. Tạo ticket mới → Kiểm tra người nhận nhận được notification, ticket hiển thị đúng trong tab "Nhận được" của họ
4. Truy cập `/Ticket/Details/{id}` → Kiểm tra thread view, reply form, forward button
5. Reply vào ticket → Message mới xuất hiện trong thread
6. Forward ticket → Người forward nhận ticket, thread hiển thị lịch sử forward
7. Upload file → File appear in attachment list, có thể download
8. Cập nhật trạng thái → Badge trạng thái thay đổi, người gửi thấy cập nhật
