# Tong hop action thong bao va chan loi

- Ngay quet: 2026-04-20
- Workspace: E:/Study/C#/managerCMN

## 1) Prompt va Skill tim thay trong workspace

### Prompt files
- E:\Study\C#\managerCMN\managerCMN\prompt.txt

### Skill files/folders
- (khong tim thay file skill trong workspace nay)

## 2) Thong ke nhanh theo loai action

| Loai action | So luong dong match |
|---|---:|
| TempData | 157 |
| ModelStateAddModelError | 46 |
| JsonAndApiReturn | 131 |
| ThrowExceptions | 82 |
| FrontendAlertToast | 51 |

### 2.0) Tom tat theo y nghia action (loi/chan/sai cu phap/hoan thanh/them-sua-xoa)

| Nhom action y nghia | So luong dong match |
|---|---:|
| AddOrCreate | 31 |
| UpdateOrEdit | 50 |
| DeleteOrRemove | 35 |
| CompleteOrDone | 32 |
| ValidationSyntax | 93 |
| ErrorOrFailure | 158 |
| BlockOrForbidden | 41 |

### 2.1) Action do la cua gi (map theo module/file)

| Module | TempData | Validation | API/Json | Block/Rule | Frontend notice |
|---|---:|---:|---:|---:|---:|
| AccountController | 4 | 0 | 2 | 0 | 0 |
| Approve | 0 | 0 | 0 | 0 | 8 |
| AssetController | 6 | 1 | 7 | 0 | 0 |
| AttendanceApiController | 0 | 0 | 4 | 0 | 0 |
| AttendanceController | 7 | 1 | 7 | 0 | 0 |
| ContractController | 8 | 3 | 2 | 0 | 0 |
| Create | 0 | 0 | 0 | 0 | 2 |
| DepartmentController | 1 | 0 | 2 | 0 | 0 |
| Details | 0 | 0 | 0 | 0 | 2 |
| Edit | 0 | 0 | 0 | 0 | 2 |
| EmployeeController | 14 | 6 | 3 | 0 | 0 |
| EmployeeService | 0 | 0 | 0 | 1 | 0 |
| Index | 0 | 0 | 0 | 0 | 6 |
| LeaveService | 0 | 0 | 0 | 4 | 0 |
| MeetingRoomController | 7 | 4 | 0 | 0 | 0 |
| MeetingRoomService | 0 | 0 | 0 | 16 | 0 |
| NotificationController | 0 | 0 | 2 | 0 | 0 |
| ProfileController | 1 | 0 | 2 | 0 | 0 |
| ProjectAccessService | 0 | 0 | 0 | 4 | 0 |
| ProjectController | 10 | 2 | 14 | 0 | 0 |
| project-management | 0 | 0 | 0 | 0 | 27 |
| ProjectService | 0 | 0 | 0 | 11 | 0 |
| ProjectTaskController | 10 | 1 | 40 | 0 | 0 |
| ProjectTaskService | 0 | 0 | 0 | 34 | 0 |
| RequestController | 21 | 28 | 12 | 0 | 0 |
| RequestService | 0 | 0 | 0 | 3 | 0 |
| SettingsController | 45 | 0 | 15 | 1 | 0 |
| SetupController | 6 | 0 | 4 | 0 | 3 |
| site | 0 | 0 | 0 | 0 | 1 |
| TicketController | 17 | 0 | 15 | 0 | 0 |
| TicketService | 0 | 0 | 0 | 8 | 0 |

## 3) Action TempData (thanh cong, loi, warning, info, import, login...)

- managerCMN\managerCMN\Controllers\AccountController.cs:108:TempData["LoginError"] = $"Email {email} không có trong danh sách nhân viên. Vui lòng liên hệ Admin để được cấp quyền truy cập.";
- managerCMN\managerCMN\Controllers\AccountController.cs:385:TempData["BirthdayCelebration"] = celebrationJson;
- managerCMN\managerCMN\Controllers\AccountController.cs:61:TempData["LoginError"] = "Đăng nhập Google chưa được cấu hình trên máy chủ. Hãy cấu hình biến môi trường trước khi sử dụng.";
- managerCMN\managerCMN\Controllers\AccountController.cs:75:TempData["LoginError"] = "Đăng nhập Google hiện chưa sẵn sàng trên máy chủ.";
- managerCMN\managerCMN\Controllers\AssetController.cs:126:TempData["Error"] = "Không tìm thấy thông tin nhân viên.";
- managerCMN\managerCMN\Controllers\AssetController.cs:261:TempData["Success"] = "Đã cập nhật thông tin tài sản thành công!";
- managerCMN\managerCMN\Controllers\AssetController.cs:296:TempData["Success"] = "Tài sản đã được cấp phát thành công!";
- managerCMN\managerCMN\Controllers\AssetController.cs:308:TempData["Success"] = "Tài sản đã được thu hồi thành công!";
- managerCMN\managerCMN\Controllers\AssetController.cs:337:TempData["Success"] = "Import tài sản thành công!";
- managerCMN\managerCMN\Controllers\AssetController.cs:342:TempData["Error"] = $"Lỗi khi import tài sản: {ex.Message}";
- managerCMN\managerCMN\Controllers\AttendanceController.cs:440:TempData["Success"] = "Import chấm công thành công!";
- managerCMN\managerCMN\Controllers\AttendanceController.cs:634:TempData["Success"] = $"Đã cập nhật {updatedCount} bản ghi chấm công với thông tin phút đi muộn.";
- managerCMN\managerCMN\Controllers\AttendanceController.cs:638:TempData["Info"] = "Không có bản ghi nào cần cập nhật.";
- managerCMN\managerCMN\Controllers\AttendanceController.cs:643:TempData["Error"] = $"Lỗi khi cập nhật: {ex.Message}";
- managerCMN\managerCMN\Controllers\AttendanceController.cs:666:TempData["Success"] = $"Đã đồng bộ lại {updatedCount} bản ghi chấm công (CheckIn/CheckOut) từ PunchRecords.";
- managerCMN\managerCMN\Controllers\AttendanceController.cs:670:TempData["Info"] = "Tất cả giờ chấm công đã đúng, không có bản ghi nào cần cập nhật.";
- managerCMN\managerCMN\Controllers\AttendanceController.cs:675:TempData["Error"] = $"Lỗi khi đồng bộ: {ex.Message}";
- managerCMN\managerCMN\Controllers\ContractController.cs:170:TempData["Success"] = "Đã cập nhật hợp đồng thành công!";
- managerCMN\managerCMN\Controllers\ContractController.cs:183:TempData["Error"] = "Không tìm thấy hợp đồng cần xóa.";
- managerCMN\managerCMN\Controllers\ContractController.cs:198:TempData["Success"] = "Đã xóa hợp đồng thành công!";
- managerCMN\managerCMN\Controllers\ContractController.cs:202:TempData["Error"] = $"Lỗi khi xóa hợp đồng: {ex.Message}";
- managerCMN\managerCMN\Controllers\ContractController.cs:269:TempData["ImportError"] = validationResult.ErrorMessage ?? "File không hợp lệ.";
- managerCMN\managerCMN\Controllers\ContractController.cs:389:TempData["ImportErrors"] = string.Join("|", errors);
- managerCMN\managerCMN\Controllers\ContractController.cs:401:TempData["ImportSuccess"] = $"Đã nhập thành công {toCreate.Count} hợp đồng.";
- managerCMN\managerCMN\Controllers\ContractController.cs:415:TempData["Error"] = "Lỗi nghiêm trọng khi xử lý file. Vui lòng kiểm tra định dạng file và thử lại.";
- managerCMN\managerCMN\Controllers\DepartmentController.cs:55:TempData["Success"] = "Đã cập nhật phòng ban thành công!";
- managerCMN\managerCMN\Controllers\EmployeeController.cs:166:TempData["SuccessMessage"] = $"Đã thêm nhân viên {employee.FullName} thành công!";
- managerCMN\managerCMN\Controllers\EmployeeController.cs:256:TempData["Success"] = "Đã cập nhật thông tin nhân viên thành công!";
- managerCMN\managerCMN\Controllers\EmployeeController.cs:269:TempData["Error"] = "Không được xoá nhân viên có mã A00000";
- managerCMN\managerCMN\Controllers\EmployeeController.cs:274:TempData["Success"] = "Đã xóa nhân viên thành công.";
- managerCMN\managerCMN\Controllers\EmployeeController.cs:278:TempData["Error"] = ex.Message;
- managerCMN\managerCMN\Controllers\EmployeeController.cs:283:TempData["Error"] = "Có lỗi xảy ra khi xóa nhân viên: " + ex.Message;
- managerCMN\managerCMN\Controllers\EmployeeController.cs:299:TempData["Success"] = "Đã cập nhật số phép cho nhân viên.";
- managerCMN\managerCMN\Controllers\EmployeeController.cs:304:TempData["Error"] = ex.Message;
- managerCMN\managerCMN\Controllers\EmployeeController.cs:309:TempData["Error"] = "Có lỗi xảy ra khi cập nhật số phép: " + ex.Message;
- managerCMN\managerCMN\Controllers\EmployeeController.cs:365:TempData["ImportError"] = validationResult.ErrorMessage ?? "File không hợp lệ.";
- managerCMN\managerCMN\Controllers\EmployeeController.cs:544:TempData["ImportErrors"] = string.Join("|", errors);
- managerCMN\managerCMN\Controllers\EmployeeController.cs:569:TempData["ImportSuccess"] = $"Đã nhập thành công {toCreate.Count} nhân viên.";
- managerCMN\managerCMN\Controllers\EmployeeController.cs:583:TempData["Error"] = "Đã xảy ra lỗi không mong muốn. Vui lòng thử lại sau.";
- managerCMN\managerCMN\Controllers\EmployeeController.cs:592:TempData["Error"] = "Lỗi nghiêm trọng khi xử lý file. Vui lòng kiểm tra định dạng file và thử lại.";
- managerCMN\managerCMN\Controllers\MeetingRoomController.cs:118:TempData["Error"] = "Không xác định được hồ sơ nhân viên hiện tại.";
- managerCMN\managerCMN\Controllers\MeetingRoomController.cs:125:TempData["Success"] = "Đã hủy lịch họp.";
- managerCMN\managerCMN\Controllers\MeetingRoomController.cs:129:TempData["Error"] = ex.Message;
- managerCMN\managerCMN\Controllers\MeetingRoomController.cs:151:TempData["Error"] = ex.Message;
- managerCMN\managerCMN\Controllers\MeetingRoomController.cs:37:TempData["Error"] = "Không xác định được hồ sơ nhân viên hiện tại.";
- managerCMN\managerCMN\Controllers\MeetingRoomController.cs:62:TempData["Success"] = "Đặt phòng họp thành công.";
- managerCMN\managerCMN\Controllers\MeetingRoomController.cs:97:TempData["Success"] = "Đã thêm phòng họp mới.";
- managerCMN\managerCMN\Controllers\ProfileController.cs:122:TempData["Error"] = "Mã số thuế phải là 12 chữ số.";
- managerCMN\managerCMN\Controllers\ProjectController.cs:122:TempData["Success"] = "Cập nhật dự án thành công.";
- managerCMN\managerCMN\Controllers\ProjectController.cs:141:TempData["Success"] = "Thêm thành viên thành công.";
- managerCMN\managerCMN\Controllers\ProjectController.cs:145:TempData["Error"] = ex.Message;
- managerCMN\managerCMN\Controllers\ProjectController.cs:159:TempData["Success"] = "Đã xoá thành viên.";
- managerCMN\managerCMN\Controllers\ProjectController.cs:163:TempData["Error"] = ex.Message;
- managerCMN\managerCMN\Controllers\ProjectController.cs:177:TempData["Success"] = "Đã cập nhật vai trò.";
- managerCMN\managerCMN\Controllers\ProjectController.cs:181:TempData["Error"] = ex.Message;
- managerCMN\managerCMN\Controllers\ProjectController.cs:195:TempData["Success"] = "Dự án đã được lưu trữ. Bạn vẫn có thể xem nhưng không thể chỉnh sửa thêm.";
- managerCMN\managerCMN\Controllers\ProjectController.cs:199:TempData["Error"] = ex.Message;
- managerCMN\managerCMN\Controllers\ProjectController.cs:77:TempData["Success"] = "Tạo dự án thành công.";
- managerCMN\managerCMN\Controllers\ProjectTaskController.cs:103:TempData["Success"] = "Tạo công việc thành công.";
- managerCMN\managerCMN\Controllers\ProjectTaskController.cs:111:TempData["Error"] = ex.Message;
- managerCMN\managerCMN\Controllers\ProjectTaskController.cs:180:TempData["Success"] = "Cập nhật công việc thành công.";
- managerCMN\managerCMN\Controllers\ProjectTaskController.cs:216:TempData["Success"] = "Đã xoá công việc.";
- managerCMN\managerCMN\Controllers\ProjectTaskController.cs:220:TempData["Error"] = ex.Message;
- managerCMN\managerCMN\Controllers\ProjectTaskController.cs:300:TempData["Success"] = "Cập nhật phân công thành công.";
- managerCMN\managerCMN\Controllers\ProjectTaskController.cs:307:TempData["Error"] = ex.Message;
- managerCMN\managerCMN\Controllers\ProjectTaskController.cs:333:TempData["Success"] = "Đã gửi cập nhật công việc.";
- managerCMN\managerCMN\Controllers\ProjectTaskController.cs:340:TempData["Error"] = ex.Message;
- managerCMN\managerCMN\Controllers\ProjectTaskController.cs:76:TempData["Error"] = "Dữ liệu không hợp lệ.";
- managerCMN\managerCMN\Controllers\RequestController.cs:162:TempData["Warning"] = $"Không đủ số dư phép (còn {summary.TotalRemaining} ngày, cần {totalDays.Value} ngày). Đơn được chuyển sang không tính công.";
- managerCMN\managerCMN\Controllers\RequestController.cs:238:TempData["Warning"] = $"Đơn đã được tạo nhưng không thể upload file đính kèm: {ex.Message}";
- managerCMN\managerCMN\Controllers\RequestController.cs:246:TempData["Success"] = "Đã gửi đơn thành công!";
- managerCMN\managerCMN\Controllers\RequestController.cs:340:TempData["Success"] = "Đã duyệt đơn thành công!";
- managerCMN\managerCMN\Controllers\RequestController.cs:354:TempData["Success"] = "Đã từ chối đơn!";
- managerCMN\managerCMN\Controllers\RequestController.cs:367:TempData["Success"] = "Đã hoàn duyệt đơn thành công! Số phép đã được hoàn trả.";
- managerCMN\managerCMN\Controllers\RequestController.cs:371:TempData["Error"] = ex.Message;
- managerCMN\managerCMN\Controllers\RequestController.cs:375:TempData["Error"] = $"Lỗi khi hoàn duyệt: {ex.Message}";
- managerCMN\managerCMN\Controllers\RequestController.cs:388:TempData["Error"] = "Không có đơn nào được chọn.";
- managerCMN\managerCMN\Controllers\RequestController.cs:415:TempData["Success"] = $"Đã duyệt thành công {successCount} đơn!";
- managerCMN\managerCMN\Controllers\RequestController.cs:417:TempData["Warning"] = $"Có {errorCount} đơn không thể duyệt.";
- managerCMN\managerCMN\Controllers\RequestController.cs:429:TempData["Error"] = "Không có đơn nào được chọn.";
- managerCMN\managerCMN\Controllers\RequestController.cs:434:TempData["Error"] = "Vui lòng nhập lý do từ chối.";
- managerCMN\managerCMN\Controllers\RequestController.cs:461:TempData["Success"] = $"Đã từ chối thành công {successCount} đơn!";
- managerCMN\managerCMN\Controllers\RequestController.cs:463:TempData["Warning"] = $"Có {errorCount} đơn không thể từ chối.";
- managerCMN\managerCMN\Controllers\RequestController.cs:474:TempData["Success"] = "Đã hủy đơn!";
- managerCMN\managerCMN\Controllers\RequestController.cs:487:TempData["Error"] = "Bạn không có quyền chỉnh sửa đơn này.";
- managerCMN\managerCMN\Controllers\RequestController.cs:494:TempData["Error"] = "Chỉ có thể chỉnh sửa đơn đang chờ duyệt.";
- managerCMN\managerCMN\Controllers\RequestController.cs:544:TempData["Error"] = "Bạn không có quyền chỉnh sửa đơn này.";
- managerCMN\managerCMN\Controllers\RequestController.cs:551:TempData["Error"] = "Chỉ có thể chỉnh sửa đơn đang chờ duyệt.";
- managerCMN\managerCMN\Controllers\RequestController.cs:665:TempData["Success"] = "Đã cập nhật đơn thành công!";
- managerCMN\managerCMN\Controllers\SettingsController.cs:145:TempData["Error"] = "Tên phòng ban không được để trống.";
- managerCMN\managerCMN\Controllers\SettingsController.cs:156:TempData["Success"] = "Thêm phòng ban thành công!";
- managerCMN\managerCMN\Controllers\SettingsController.cs:170:TempData["Success"] = "Cập nhật phòng ban thành công!";
- managerCMN\managerCMN\Controllers\SettingsController.cs:178:TempData["Success"] = "Xóa phòng ban thành công!";
- managerCMN\managerCMN\Controllers\SettingsController.cs:189:TempData["Error"] = "Tên vị trí không được để trống.";
- managerCMN\managerCMN\Controllers\SettingsController.cs:201:TempData["Success"] = "Thêm vị trí thành công!";
- managerCMN\managerCMN\Controllers\SettingsController.cs:216:TempData["Success"] = "Cập nhật vị trí thành công!";
- managerCMN\managerCMN\Controllers\SettingsController.cs:229:TempData["Success"] = "Xóa vị trí thành công!";
- managerCMN\managerCMN\Controllers\SettingsController.cs:240:TempData["Error"] = "Tên chức vụ không được để trống.";
- managerCMN\managerCMN\Controllers\SettingsController.cs:252:TempData["Success"] = "Thêm chức vụ thành công!";
- managerCMN\managerCMN\Controllers\SettingsController.cs:267:TempData["Success"] = "Cập nhật chức vụ thành công!";
- managerCMN\managerCMN\Controllers\SettingsController.cs:280:TempData["Success"] = "Xóa chức vụ thành công!";
- managerCMN\managerCMN\Controllers\SettingsController.cs:291:TempData["Error"] = "Tên danh mục không được để trống.";
- managerCMN\managerCMN\Controllers\SettingsController.cs:297:TempData["Success"] = "Thêm danh mục tài sản thành công!";
- managerCMN\managerCMN\Controllers\SettingsController.cs:310:TempData["Success"] = "Cập nhật danh mục thành công!";
- managerCMN\managerCMN\Controllers\SettingsController.cs:323:TempData["Success"] = "Xóa danh mục thành công!";
- managerCMN\managerCMN\Controllers\SettingsController.cs:334:TempData["Error"] = "Tên hãng sản xuất không được để trống.";
- managerCMN\managerCMN\Controllers\SettingsController.cs:340:TempData["Success"] = "Thêm hãng sản xuất thành công!";
- managerCMN\managerCMN\Controllers\SettingsController.cs:353:TempData["Success"] = "Cập nhật hãng sản xuất thành công!";
- managerCMN\managerCMN\Controllers\SettingsController.cs:366:TempData["Success"] = "Xóa hãng sản xuất thành công!";
- managerCMN\managerCMN\Controllers\SettingsController.cs:377:TempData["Error"] = "Tên nhà cung cấp không được để trống.";
- managerCMN\managerCMN\Controllers\SettingsController.cs:388:TempData["Success"] = "Thêm nhà cung cấp thành công!";
- managerCMN\managerCMN\Controllers\SettingsController.cs:403:TempData["Success"] = "Cập nhật nhà cung cấp thành công!";
- managerCMN\managerCMN\Controllers\SettingsController.cs:416:TempData["Success"] = "Xóa nhà cung cấp thành công!";
- managerCMN\managerCMN\Controllers\SettingsController.cs:427:TempData["Error"] = "Tên ngày nghỉ không được để trống.";
- managerCMN\managerCMN\Controllers\SettingsController.cs:434:TempData["Error"] = "Ngày này đã có trong danh sách nghỉ lễ.";
- managerCMN\managerCMN\Controllers\SettingsController.cs:447:TempData["Success"] = "Thêm ngày nghỉ lễ thành công!";
- managerCMN\managerCMN\Controllers\SettingsController.cs:460:TempData["Error"] = "Ngày này đã có trong danh sách nghỉ lễ.";
- managerCMN\managerCMN\Controllers\SettingsController.cs:470:TempData["Success"] = "Cập nhật ngày nghỉ lễ thành công!";
- managerCMN\managerCMN\Controllers\SettingsController.cs:483:TempData["Success"] = "Xóa ngày nghỉ lễ thành công!";
- managerCMN\managerCMN\Controllers\SettingsController.cs:497:TempData["Success"] = $"Đã thêm {emp.FullName} vào danh sách người duyệt!";
- managerCMN\managerCMN\Controllers\SettingsController.cs:509:TempData["Success"] = $"Đã xóa {emp.FullName} khỏi danh sách người duyệt!";
- managerCMN\managerCMN\Controllers\SettingsController.cs:562:TempData["Error"] = "Không tìm thấy người dùng cần cập nhật.";
- managerCMN\managerCMN\Controllers\SettingsController.cs:574:TempData["Error"] = "Chỉ admin có mã nhân viên A00000 mới được thay đổi vai trò của người dùng đang là Admin.";
- managerCMN\managerCMN\Controllers\SettingsController.cs:580:TempData["Error"] = "Chỉ admin có mã nhân viên A00000 mới được cấp vai trò Admin cho người dùng khác.";
- managerCMN\managerCMN\Controllers\SettingsController.cs:62:TempData["Error"] = "Tính năng chấm công đầy đủ chưa sẵn sàng vì database chưa có bảng FullAttendanceEmployees. Vui lòng chạy migration mới nhất bằng 'dotnet ef database update'.";
- managerCMN\managerCMN\Controllers\SettingsController.cs:639:TempData["Success"] = "Đã cập nhật vai trò người dùng thành công!";
- managerCMN\managerCMN\Controllers\SettingsController.cs:644:TempData["Error"] = "Có lỗi xảy ra: " + ex.Message;
- managerCMN\managerCMN\Controllers\SettingsController.cs:671:TempData["Error"] = "Nhân viên này đã được thêm vào danh sách!";
- managerCMN\managerCMN\Controllers\SettingsController.cs:688:TempData["Success"] = $"Đã thêm {emp.FullName} vào danh sách chấm công đầy đủ!";
- managerCMN\managerCMN\Controllers\SettingsController.cs:693:TempData["Error"] = "Có lỗi xảy ra: " + ex.Message;
- managerCMN\managerCMN\Controllers\SettingsController.cs:713:TempData["Success"] = "Đã cập nhật thông tin thành công!";
- managerCMN\managerCMN\Controllers\SettingsController.cs:718:TempData["Error"] = "Có lỗi xảy ra: " + ex.Message;
- managerCMN\managerCMN\Controllers\SettingsController.cs:738:TempData["Success"] = $"Đã xóa {empName} khỏi danh sách chấm công đầy đủ!";
- managerCMN\managerCMN\Controllers\SettingsController.cs:743:TempData["Error"] = "Có lỗi xảy ra: " + ex.Message;
- managerCMN\managerCMN\Controllers\SetupController.cs:107:TempData["Error"] = "Không tìm thấy người dùng.";
- managerCMN\managerCMN\Controllers\SetupController.cs:114:TempData["Error"] = "Người dùng này đã là Admin.";
- managerCMN\managerCMN\Controllers\SetupController.cs:143:TempData["Success"] = $"Đã gán quyền Admin cho {user.Email} thành công!";
- managerCMN\managerCMN\Controllers\SetupController.cs:150:TempData["Error"] = "Có lỗi xảy ra khi gán quyền Admin: " + ex.Message;
- managerCMN\managerCMN\Controllers\SetupController.cs:92:TempData["Error"] = "Lỗi: Không tìm thấy role Admin trong hệ thống.";
- managerCMN\managerCMN\Controllers\SetupController.cs:99:TempData["Error"] = "Chỉ admin có mã nhân viên A00000 mới được cấp thêm quyền Admin.";
- managerCMN\managerCMN\Controllers\TicketController.cs:144:TempData["Success"] = "Đã gửi ticket thành công!";
- managerCMN\managerCMN\Controllers\TicketController.cs:206:TempData["Error"] = "Không tìm thấy thông tin nhân viên. Vui lòng đăng nhập lại.";
- managerCMN\managerCMN\Controllers\TicketController.cs:212:TempData["Error"] = "Vui lòng nhập nội dung phản hồi.";
- managerCMN\managerCMN\Controllers\TicketController.cs:219:TempData["Success"] = "Đã gửi phản hồi!";
- managerCMN\managerCMN\Controllers\TicketController.cs:223:TempData["Error"] = "Ticket không tồn tại hoặc đã bị xóa.";
- managerCMN\managerCMN\Controllers\TicketController.cs:228:TempData["Error"] = "Bạn không có quyền phản hồi ticket này.";
- managerCMN\managerCMN\Controllers\TicketController.cs:231:TempData["Success"] = "Đã gửi phản hồi!";
- managerCMN\managerCMN\Controllers\TicketController.cs:246:TempData["Error"] = "Không tìm thấy thông tin nhân viên. Vui lòng đăng nhập lại.";
- managerCMN\managerCMN\Controllers\TicketController.cs:252:TempData["Error"] = "Vui lòng chọn ít nhất một người nhận.";
- managerCMN\managerCMN\Controllers\TicketController.cs:259:TempData["Success"] = "Đã chuyển tiếp ticket!";
- managerCMN\managerCMN\Controllers\TicketController.cs:263:TempData["Error"] = "Ticket không tồn tại hoặc đã bị xóa.";
- managerCMN\managerCMN\Controllers\TicketController.cs:268:TempData["Error"] = "Bạn không có quyền chuyển tiếp ticket này.";
- managerCMN\managerCMN\Controllers\TicketController.cs:271:TempData["Success"] = "Đã chuyển tiếp ticket!";
- managerCMN\managerCMN\Controllers\TicketController.cs:290:TempData["Success"] = "Đã cập nhật trạng thái!";
- managerCMN\managerCMN\Controllers\TicketController.cs:356:TempData["Success"] = "Đã cập nhật ticket thành đã giải quyết.";
- managerCMN\managerCMN\Controllers\TicketController.cs:360:TempData["Error"] = "Ticket không tồn tại hoặc đã bị xóa.";
- managerCMN\managerCMN\Controllers\TicketController.cs:365:TempData["Error"] = "Bạn không có quyền giải quyết ticket này.";

## 4) Action validation / sai cu phap du lieu (ModelState.AddModelError)

- managerCMN\managerCMN\Controllers\AssetController.cs:330:ModelState.AddModelError("", validationResult.ErrorMessage ?? "File không hợp lệ.");
- managerCMN\managerCMN\Controllers\AttendanceController.cs:433:ModelState.AddModelError("", validationResult.ErrorMessage ?? "File không hợp lệ.");
- managerCMN\managerCMN\Controllers\ContractController.cs:153:ModelState.AddModelError("ContractNumber", "Số hợp đồng này đã tồn tại.");
- managerCMN\managerCMN\Controllers\ContractController.cs:70:ModelState.AddModelError("ContractNumber", "Số hợp đồng này đã tồn tại.");
- managerCMN\managerCMN\Controllers\ContractController.cs:85:ModelState.AddModelError("ContractFile", validationResult.ErrorMessage ?? "File không hợp lệ.");
- managerCMN\managerCMN\Controllers\EmployeeController.cs:102:ModelState.AddModelError(nameof(model.Email), "Email đã tồn tại trong hệ thống");
- managerCMN\managerCMN\Controllers\EmployeeController.cs:111:ModelState.AddModelError(nameof(model.EmployeeCode), "Mã nhân viên đã tồn tại");
- managerCMN\managerCMN\Controllers\EmployeeController.cs:71:ModelState.AddModelError(nameof(model.DateOfBirth), "Ngày sinh phải là ngày trong quá khứ");
- managerCMN\managerCMN\Controllers\EmployeeController.cs:75:ModelState.AddModelError(nameof(model.DateOfBirth), "Nhân viên phải ít nhất 15 tuổi");
- managerCMN\managerCMN\Controllers\EmployeeController.cs:86:ModelState.AddModelError(nameof(model.StartWorkingDate), "Ngày vào làm phải sau ngày sinh ít nhất 15 tuổi");
- managerCMN\managerCMN\Controllers\EmployeeController.cs:94:ModelState.AddModelError(nameof(model.IdCardIssueDate), "Ngày cấp CCCD phải sau ngày sinh");
- managerCMN\managerCMN\Controllers\MeetingRoomController.cs:102:ModelState.AddModelError("NewRoom.Name", ex.Message);
- managerCMN\managerCMN\Controllers\MeetingRoomController.cs:251:ModelState.AddModelError(modelKey, invalidMessage);
- managerCMN\managerCMN\Controllers\MeetingRoomController.cs:257:ModelState.AddModelError(modelKey, invalidMessage);
- managerCMN\managerCMN\Controllers\MeetingRoomController.cs:67:ModelState.AddModelError(string.Empty, ex.Message);
- managerCMN\managerCMN\Controllers\ProjectController.cs:127:ModelState.AddModelError("", ex.Message);
- managerCMN\managerCMN\Controllers\ProjectController.cs:72:ModelState.AddModelError("EndDate", "Ngày kết thúc phải sau ngày bắt đầu.");
- managerCMN\managerCMN\Controllers\ProjectTaskController.cs:185:ModelState.AddModelError("", ex.Message);
- managerCMN\managerCMN\Controllers\RequestController.cs:1013:ModelState.AddModelError(nameof(RequestCreateViewModel.StartTime), ex.Message);
- managerCMN\managerCMN\Controllers\RequestController.cs:102:ModelState.AddModelError("RequestType", BuildAbsenceLimitMessage(attendancePolicy));
- managerCMN\managerCMN\Controllers\RequestController.cs:1022:ModelState.AddModelError(nameof(RequestCreateViewModel.Title), "Vui lòng nhập tiêu đề.");
- managerCMN\managerCMN\Controllers\RequestController.cs:1027:ModelState.AddModelError(nameof(RequestCreateViewModel.LeaveReason), "Vui lòng chọn lý do.");
- managerCMN\managerCMN\Controllers\RequestController.cs:1032:ModelState.AddModelError(nameof(RequestCreateViewModel.Approver1Id), "Vui lòng chọn người duyệt 1.");
- managerCMN\managerCMN\Controllers\RequestController.cs:1037:ModelState.AddModelError(nameof(RequestCreateViewModel.Approver2Id), "Vui lòng chọn người duyệt 2.");
- managerCMN\managerCMN\Controllers\RequestController.cs:1042:ModelState.AddModelError(nameof(RequestCreateViewModel.StartTime), "Vui lòng chọn thời gian bắt đầu.");
- managerCMN\managerCMN\Controllers\RequestController.cs:1047:ModelState.AddModelError(nameof(RequestCreateViewModel.EndTime), "Vui lòng chọn thời gian kết thúc.");
- managerCMN\managerCMN\Controllers\RequestController.cs:116:ModelState.AddModelError("RequestType", BuildCheckInOutLimitMessage(attendancePolicy, model.CheckInOutType));
- managerCMN\managerCMN\Controllers\RequestController.cs:131:ModelState.AddModelError("StartTime",
- managerCMN\managerCMN\Controllers\RequestController.cs:561:ModelState.AddModelError("Approver1Id", "Vui lòng chọn người duyệt 1.");
- managerCMN\managerCMN\Controllers\RequestController.cs:579:ModelState.AddModelError("RequestType", BuildAbsenceLimitMessage(attendancePolicy));
- managerCMN\managerCMN\Controllers\RequestController.cs:600:ModelState.AddModelError("RequestType", BuildCheckInOutLimitMessage(attendancePolicy, model.CheckInOutType));
- managerCMN\managerCMN\Controllers\RequestController.cs:616:ModelState.AddModelError("StartTime",
- managerCMN\managerCMN\Controllers\RequestController.cs:848:ModelState.AddModelError(nameof(RequestCreateViewModel.CheckInOutType), "Vui lòng chọn loại checkin/out.");
- managerCMN\managerCMN\Controllers\RequestController.cs:856:ModelState.AddModelError(nameof(RequestCreateViewModel.EndClock), "Vui lòng chọn giờ check out.");
- managerCMN\managerCMN\Controllers\RequestController.cs:862:ModelState.AddModelError(nameof(RequestCreateViewModel.EndClock), "Giờ check out không hợp lệ.");
- managerCMN\managerCMN\Controllers\RequestController.cs:869:ModelState.AddModelError(nameof(RequestCreateViewModel.StartClock), "Vui lòng chọn giờ check in.");
- managerCMN\managerCMN\Controllers\RequestController.cs:875:ModelState.AddModelError(nameof(RequestCreateViewModel.StartClock), "Giờ check in không hợp lệ.");
- managerCMN\managerCMN\Controllers\RequestController.cs:889:ModelState.AddModelError(nameof(RequestCreateViewModel.StartClock), "Vui lòng chọn giờ bắt đầu.");
- managerCMN\managerCMN\Controllers\RequestController.cs:89:ModelState.AddModelError("Approver1Id", "Vui lòng chọn người duyệt 1.");
- managerCMN\managerCMN\Controllers\RequestController.cs:894:ModelState.AddModelError(nameof(RequestCreateViewModel.EndClock), "Vui lòng chọn giờ kết thúc.");
- managerCMN\managerCMN\Controllers\RequestController.cs:899:ModelState.AddModelError(nameof(RequestCreateViewModel.StartClock), "Giờ bắt đầu không hợp lệ.");
- managerCMN\managerCMN\Controllers\RequestController.cs:904:ModelState.AddModelError(nameof(RequestCreateViewModel.EndClock), "Giờ kết thúc không hợp lệ.");
- managerCMN\managerCMN\Controllers\RequestController.cs:911:ModelState.AddModelError(nameof(RequestCreateViewModel.EndClock), "Giờ kết thúc phải sau giờ bắt đầu.");
- managerCMN\managerCMN\Controllers\RequestController.cs:943:ModelState.AddModelError(nameof(RequestCreateViewModel.StartClock),
- managerCMN\managerCMN\Controllers\RequestController.cs:957:ModelState.AddModelError(nameof(RequestCreateViewModel.EndClock),
- managerCMN\managerCMN\Controllers\RequestController.cs:962:ModelState.AddModelError(nameof(RequestCreateViewModel.EndClock),

## 5) Action response API/Json (success, error, forbidden, notfound...)

- managerCMN\managerCMN\Controllers\AccountController.cs:188:return NotFound();
- managerCMN\managerCMN\Controllers\AccountController.cs:200:if (emp == null) return NotFound();
- managerCMN\managerCMN\Controllers\AssetController.cs:142:return Unauthorized();
- managerCMN\managerCMN\Controllers\AssetController.cs:156:return NotFound();
- managerCMN\managerCMN\Controllers\AssetController.cs:164:return Forbid();
- managerCMN\managerCMN\Controllers\AssetController.cs:175:if (asset == null) return NotFound();
- managerCMN\managerCMN\Controllers\AssetController.cs:187:if (asset == null) return NotFound();
- managerCMN\managerCMN\Controllers\AssetController.cs:244:if (asset == null) return NotFound();
- managerCMN\managerCMN\Controllers\AssetController.cs:269:if (asset == null) return NotFound();
- managerCMN\managerCMN\Controllers\AttendanceApiController.cs:124:return Ok(new { message = "Import thành công.", count = punchRecords.Count });
- managerCMN\managerCMN\Controllers\AttendanceApiController.cs:138:return StatusCode(500, new { error = "Lỗi server khi xử lý dữ liệu.", details = ex.Message });
- managerCMN\managerCMN\Controllers\AttendanceApiController.cs:53:return BadRequest(new { error = "Dữ liệu trống." });
- managerCMN\managerCMN\Controllers\AttendanceApiController.cs:82:return BadRequest(new { error = "Không có bản ghi hợp lệ." });
- managerCMN\managerCMN\Controllers\AttendanceController.cs:689:return StatusCode(StatusCodes.Status403Forbidden, new
- managerCMN\managerCMN\Controllers\AttendanceController.cs:704:return Json(new { success = false, error = "Date is required" });
- managerCMN\managerCMN\Controllers\AttendanceController.cs:710:return Json(new { success = false, error = $"Invalid date format: {cleanedDate}" });
- managerCMN\managerCMN\Controllers\AttendanceController.cs:731:return Json(new { success = true, data = result });
- managerCMN\managerCMN\Controllers\AttendanceController.cs:736:return Json(new { success = false, error = ex.Message });
- managerCMN\managerCMN\Controllers\AttendanceController.cs:747:return NotFound();
- managerCMN\managerCMN\Controllers\AttendanceController.cs:769:return Json(new {
- managerCMN\managerCMN\Controllers\ContractController.cs:124:if (contract == null) return NotFound();
- managerCMN\managerCMN\Controllers\ContractController.cs:159:if (contract == null) return NotFound();
- managerCMN\managerCMN\Controllers\DepartmentController.cs:25:if (department == null) return NotFound();
- managerCMN\managerCMN\Controllers\DepartmentController.cs:44:if (department == null) return NotFound();
- managerCMN\managerCMN\Controllers\EmployeeController.cs:173:if (employee == null) return NotFound();
- managerCMN\managerCMN\Controllers\EmployeeController.cs:224:if (employee == null) return NotFound();
- managerCMN\managerCMN\Controllers\EmployeeController.cs:46:if (employee == null) return NotFound();
- managerCMN\managerCMN\Controllers\NotificationController.cs:48:return Forbid();
- managerCMN\managerCMN\Controllers\NotificationController.cs:76:return Json(new { count });
- managerCMN\managerCMN\Controllers\ProfileController.cs:136:if (employee == null) return NotFound();
- managerCMN\managerCMN\Controllers\ProfileController.cs:85:if (employee == null) return NotFound();
- managerCMN\managerCMN\Controllers\ProjectController.cs:107:return NotFound();
- managerCMN\managerCMN\Controllers\ProjectController.cs:115:if (employeeId == 0) return Forbid();
- managerCMN\managerCMN\Controllers\ProjectController.cs:136:if (employeeId == 0) return Forbid();
- managerCMN\managerCMN\Controllers\ProjectController.cs:154:if (employeeId == 0) return Forbid();
- managerCMN\managerCMN\Controllers\ProjectController.cs:172:if (employeeId == 0) return Forbid();
- managerCMN\managerCMN\Controllers\ProjectController.cs:190:if (employeeId == 0) return Forbid();
- managerCMN\managerCMN\Controllers\ProjectController.cs:28:if (employeeId == 0) return Forbid();
- managerCMN\managerCMN\Controllers\ProjectController.cs:36:if (employeeId == 0) return Forbid();
- managerCMN\managerCMN\Controllers\ProjectController.cs:41:if (details == null) return NotFound();
- managerCMN\managerCMN\Controllers\ProjectController.cs:53:return NotFound();
- managerCMN\managerCMN\Controllers\ProjectController.cs:66:if (employeeId == 0) return Forbid();
- managerCMN\managerCMN\Controllers\ProjectController.cs:84:if (employeeId == 0) return Forbid();
- managerCMN\managerCMN\Controllers\ProjectController.cs:89:if (details == null) return NotFound();
- managerCMN\managerCMN\Controllers\ProjectController.cs:92:return Forbid();
- managerCMN\managerCMN\Controllers\ProjectTaskController.cs:109:return Json(new { success = false, message = ex.Message });
- managerCMN\managerCMN\Controllers\ProjectTaskController.cs:119:if (employeeId == 0) return Forbid();
- managerCMN\managerCMN\Controllers\ProjectTaskController.cs:124:if (task == null) return NotFound();
- managerCMN\managerCMN\Controllers\ProjectTaskController.cs:155:return NotFound();
- managerCMN\managerCMN\Controllers\ProjectTaskController.cs:163:if (employeeId == 0) return Forbid();
- managerCMN\managerCMN\Controllers\ProjectTaskController.cs:211:if (employeeId == 0) return Forbid();
- managerCMN\managerCMN\Controllers\ProjectTaskController.cs:229:if (employeeId == 0) return Json(new { success = false, message = "Không xác thực được người dùng." });
- managerCMN\managerCMN\Controllers\ProjectTaskController.cs:235:return Json(new
- managerCMN\managerCMN\Controllers\ProjectTaskController.cs:245:return Json(new { success = false, message = ex.Message });
- managerCMN\managerCMN\Controllers\ProjectTaskController.cs:253:if (employeeId == 0) return Json(new { success = false, message = "Không xác thực được người dùng." });
- managerCMN\managerCMN\Controllers\ProjectTaskController.cs:259:return Json(new
- managerCMN\managerCMN\Controllers\ProjectTaskController.cs:269:return Json(new { success = false, message = ex.Message });
- managerCMN\managerCMN\Controllers\ProjectTaskController.cs:277:if (employeeId == 0) return Forbid();
- managerCMN\managerCMN\Controllers\ProjectTaskController.cs:286:return Json(new
- managerCMN\managerCMN\Controllers\ProjectTaskController.cs:30:if (employeeId == 0) return Forbid();
- managerCMN\managerCMN\Controllers\ProjectTaskController.cs:305:return Json(new { success = false, message = ex.Message });
- managerCMN\managerCMN\Controllers\ProjectTaskController.cs:316:if (employeeId == 0) return Forbid();
- managerCMN\managerCMN\Controllers\ProjectTaskController.cs:324:return Json(new
- managerCMN\managerCMN\Controllers\ProjectTaskController.cs:338:return Json(new { success = false, message = ex.Message });
- managerCMN\managerCMN\Controllers\ProjectTaskController.cs:35:if (task == null) return NotFound();
- managerCMN\managerCMN\Controllers\ProjectTaskController.cs:350:if (employeeId == 0) return Json(new { success = false });
- managerCMN\managerCMN\Controllers\ProjectTaskController.cs:356:return Json(new
- managerCMN\managerCMN\Controllers\ProjectTaskController.cs:366:return Json(new { success = false, message = ex.Message });
- managerCMN\managerCMN\Controllers\ProjectTaskController.cs:374:if (employeeId == 0) return Json(new { success = false });
- managerCMN\managerCMN\Controllers\ProjectTaskController.cs:38:if (project == null) return NotFound();
- managerCMN\managerCMN\Controllers\ProjectTaskController.cs:380:return Json(new
- managerCMN\managerCMN\Controllers\ProjectTaskController.cs:389:return Json(new { success = false, message = ex.Message });
- managerCMN\managerCMN\Controllers\ProjectTaskController.cs:397:if (employeeId == 0) return Json(new { success = false });
- managerCMN\managerCMN\Controllers\ProjectTaskController.cs:403:return Json(new
- managerCMN\managerCMN\Controllers\ProjectTaskController.cs:412:return Json(new { success = false, message = ex.Message });
- managerCMN\managerCMN\Controllers\ProjectTaskController.cs:419:if (employeeId == 0) return Forbid();
- managerCMN\managerCMN\Controllers\ProjectTaskController.cs:424:if (attachment == null) return NotFound();
- managerCMN\managerCMN\Controllers\ProjectTaskController.cs:433:return Forbid();
- managerCMN\managerCMN\Controllers\ProjectTaskController.cs:435:return NotFound();
- managerCMN\managerCMN\Controllers\ProjectTaskController.cs:442:return BadRequest(ex.Message);
- managerCMN\managerCMN\Controllers\ProjectTaskController.cs:446:return Forbid();
- managerCMN\managerCMN\Controllers\ProjectTaskController.cs:61:return NotFound();
- managerCMN\managerCMN\Controllers\ProjectTaskController.cs:69:if (employeeId == 0) return Forbid();
- managerCMN\managerCMN\Controllers\ProjectTaskController.cs:74:return Json(new { success = false, message = "Dữ liệu không hợp lệ." });
- managerCMN\managerCMN\Controllers\ProjectTaskController.cs:86:return Json(new
- managerCMN\managerCMN\Controllers\RequestController.cs:255:if (request == null) return NotFound();
- managerCMN\managerCMN\Controllers\RequestController.cs:259:return Forbid();
- managerCMN\managerCMN\Controllers\RequestController.cs:280:return Forbid();
- managerCMN\managerCMN\Controllers\RequestController.cs:334:if (!IsPrivileged()) return Forbid();
- managerCMN\managerCMN\Controllers\RequestController.cs:348:if (!IsPrivileged()) return Forbid();
- managerCMN\managerCMN\Controllers\RequestController.cs:385:if (!IsPrivileged()) return Forbid();
- managerCMN\managerCMN\Controllers\RequestController.cs:426:if (!IsPrivileged()) return Forbid();
- managerCMN\managerCMN\Controllers\RequestController.cs:482:if (request == null) return NotFound();
- managerCMN\managerCMN\Controllers\RequestController.cs:530:if (request == null) return NotFound();
- managerCMN\managerCMN\Controllers\RequestController.cs:681:return Json(reasons);
- managerCMN\managerCMN\Controllers\RequestController.cs:704:return Json(new
- managerCMN\managerCMN\Controllers\RequestController.cs:718:return BadRequest(new
- managerCMN\managerCMN\Controllers\SettingsController.cs:164:if (dept == null) return NotFound();
- managerCMN\managerCMN\Controllers\SettingsController.cs:209:if (pos == null) return NotFound();
- managerCMN\managerCMN\Controllers\SettingsController.cs:260:if (jt == null) return NotFound();
- managerCMN\managerCMN\Controllers\SettingsController.cs:305:if (cat == null) return NotFound();
- managerCMN\managerCMN\Controllers\SettingsController.cs:348:if (brand == null) return NotFound();
- managerCMN\managerCMN\Controllers\SettingsController.cs:396:if (sup == null) return NotFound();
- managerCMN\managerCMN\Controllers\SettingsController.cs:455:if (holiday == null) return NotFound();
- managerCMN\managerCMN\Controllers\SettingsController.cs:493:if (emp == null) return NotFound();
- managerCMN\managerCMN\Controllers\SettingsController.cs:505:if (emp == null) return NotFound();
- managerCMN\managerCMN\Controllers\SettingsController.cs:519:return Json(permissions.Select(p => p.PermissionId));
- managerCMN\managerCMN\Controllers\SettingsController.cs:530:return Json(new { success = true, message = "Đã cập nhật phân quyền thành công!" });
- managerCMN\managerCMN\Controllers\SettingsController.cs:534:return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
- managerCMN\managerCMN\Controllers\SettingsController.cs:676:if (emp == null) return NotFound();
- managerCMN\managerCMN\Controllers\SettingsController.cs:707:if (fullAttendanceEmp == null) return NotFound();
- managerCMN\managerCMN\Controllers\SettingsController.cs:732:if (fullAttendanceEmp == null) return NotFound();
- managerCMN\managerCMN\Controllers\SetupController.cs:100:return Forbid();
- managerCMN\managerCMN\Controllers\SetupController.cs:42:return NotFound();
- managerCMN\managerCMN\Controllers\SetupController.cs:56:return Forbid();
- managerCMN\managerCMN\Controllers\SetupController.cs:84:return NotFound();
- managerCMN\managerCMN\Controllers\TicketController.cs:102:return StatusCode(StatusCodes.Status403Forbidden, new { success = false });
- managerCMN\managerCMN\Controllers\TicketController.cs:106:return StatusCode(StatusCodes.Status503ServiceUnavailable, new { success = false });
- managerCMN\managerCMN\Controllers\TicketController.cs:155:if (ticket == null) return NotFound();
- managerCMN\managerCMN\Controllers\TicketController.cs:171:return Forbid();
- managerCMN\managerCMN\Controllers\TicketController.cs:284:if (ticket == null) return NotFound();
- managerCMN\managerCMN\Controllers\TicketController.cs:287:if (recipient == null) return Forbid();
- managerCMN\managerCMN\Controllers\TicketController.cs:301:if (attachment == null) return NotFound();
- managerCMN\managerCMN\Controllers\TicketController.cs:305:return NotFound();
- managerCMN\managerCMN\Controllers\TicketController.cs:309:return Forbid();
- managerCMN\managerCMN\Controllers\TicketController.cs:318:return Forbid();
- managerCMN\managerCMN\Controllers\TicketController.cs:319:if (!System.IO.File.Exists(filePath)) return NotFound();
- managerCMN\managerCMN\Controllers\TicketController.cs:333:if (ticket == null) return NotFound();
- managerCMN\managerCMN\Controllers\TicketController.cs:89:return Unauthorized(new { success = false });
- managerCMN\managerCMN\Controllers\TicketController.cs:94:return Json(new { success = true, isStarred });
- managerCMN\managerCMN\Controllers\TicketController.cs:98:return NotFound(new { success = false });

## 6) Action chan thao tac / rule nghiep vu (throw exception)

- managerCMN\managerCMN\Controllers\SettingsController.cs:552:throw new InvalidOperationException("Không tìm thấy role Admin trong hệ thống.");
- managerCMN\managerCMN\Services\Implementations\EmployeeService.cs:89:throw new InvalidOperationException("Không được xoá nhân viên có mã A00000");
- managerCMN\managerCMN\Services\Implementations\LeaveService.cs:314:throw new InvalidOperationException("Không thể giảm phép năm xuống thấp hơn số phép đã sử dụng.");
- managerCMN\managerCMN\Services\Implementations\LeaveService.cs:320:throw new InvalidOperationException("Không thể giảm phép bảo lưu xuống nhỏ hơn 0.");
- managerCMN\managerCMN\Services\Implementations\LeaveService.cs:54:?? throw new InvalidOperationException("Không tìm thấy nhân viên để tính phép.");
- managerCMN\managerCMN\Services\Implementations\LeaveService.cs:543:?? throw new InvalidOperationException("Không tìm thấy nhân viên để tính phép.");
- managerCMN\managerCMN\Services\Implementations\MeetingRoomService.cs:101:?? throw new ValidationException("Phòng họp không tồn tại.");
- managerCMN\managerCMN\Services\Implementations\MeetingRoomService.cs:105:throw new ValidationException("Phòng họp này hiện không khả dụng để đặt lịch.");
- managerCMN\managerCMN\Services\Implementations\MeetingRoomService.cs:109:?? throw new ValidationException("Không tìm thấy nhân viên đặt phòng.");
- managerCMN\managerCMN\Services\Implementations\MeetingRoomService.cs:116:throw new ValidationException("Khung giờ này đã có người đặt. Vui lòng chọn thời gian khác.");
- managerCMN\managerCMN\Services\Implementations\MeetingRoomService.cs:149:?? throw new ValidationException("Không tìm thấy lịch đặt phòng.");
- managerCMN\managerCMN\Services\Implementations\MeetingRoomService.cs:158:throw new ValidationException("Bạn không có quyền hủy lịch đặt phòng này.");
- managerCMN\managerCMN\Services\Implementations\MeetingRoomService.cs:163:throw new ValidationException("Lịch họp đã tới giờ bắt đầu. Chỉ admin mới có thể hủy lịch này.");
- managerCMN\managerCMN\Services\Implementations\MeetingRoomService.cs:204:throw new ValidationException("Vui lòng chọn phòng họp.");
- managerCMN\managerCMN\Services\Implementations\MeetingRoomService.cs:209:throw new ValidationException("Không xác định được nhân viên đặt phòng.");
- managerCMN\managerCMN\Services\Implementations\MeetingRoomService.cs:214:throw new ValidationException("Vui lòng nhập tiêu đề cuộc họp.");
- managerCMN\managerCMN\Services\Implementations\MeetingRoomService.cs:219:throw new ValidationException("Giờ kết thúc phải sau giờ bắt đầu.");
- managerCMN\managerCMN\Services\Implementations\MeetingRoomService.cs:224:throw new ValidationException("Lịch họp chỉ được đặt trong cùng một ngày.");
- managerCMN\managerCMN\Services\Implementations\MeetingRoomService.cs:229:throw new ValidationException("Bạn phải đặt phòng trước ít nhất 30 phút so với giờ bắt đầu.");
- managerCMN\managerCMN\Services\Implementations\MeetingRoomService.cs:48:throw new ValidationException("Tên phòng họp đã tồn tại.");
- managerCMN\managerCMN\Services\Implementations\MeetingRoomService.cs:67:?? throw new ValidationException("Không tìm thấy phòng họp.");
- managerCMN\managerCMN\Services\Implementations\MeetingRoomService.cs:76:throw new ValidationException("Không thể ngưng sử dụng phòng đang có lịch họp sắp tới.");
- managerCMN\managerCMN\Services\Implementations\ProjectAccessService.cs:25:throw new UnauthorizedAccessException("Bạn không phải thành viên của dự án này.");
- managerCMN\managerCMN\Services\Implementations\ProjectAccessService.cs:32:throw new UnauthorizedAccessException("Chỉ ProjectOwner và ProjectManager mới được quản lý thành viên.");
- managerCMN\managerCMN\Services\Implementations\ProjectAccessService.cs:39:throw new UnauthorizedAccessException("Chỉ ProjectOwner mới được bổ nhiệm/thu hồi ProjectManager.");
- managerCMN\managerCMN\Services\Implementations\ProjectAccessService.cs:46:throw new UnauthorizedAccessException("Bạn không có quyền quản lý công việc trong dự án này.");
- managerCMN\managerCMN\Services\Implementations\ProjectService.cs:156:throw new UnauthorizedAccessException("Chỉ Owner hoặc Manager mới được sửa thông tin dự án.");
- managerCMN\managerCMN\Services\Implementations\ProjectService.cs:159:?? throw new InvalidOperationException("Dự án không tồn tại.");
- managerCMN\managerCMN\Services\Implementations\ProjectService.cs:177:?? throw new InvalidOperationException("Dự án không tồn tại.");
- managerCMN\managerCMN\Services\Implementations\ProjectService.cs:194:throw new InvalidOperationException("ProjectManager không được bổ nhiệm Owner hoặc Manager khác.");
- managerCMN\managerCMN\Services\Implementations\ProjectService.cs:197:if (existing != null) throw new InvalidOperationException("Nhân viên đã là thành viên của dự án.");
- managerCMN\managerCMN\Services\Implementations\ProjectService.cs:219:throw new InvalidOperationException("Không thể xoá ProjectOwner khỏi dự án.");
- managerCMN\managerCMN\Services\Implementations\ProjectService.cs:223:throw new InvalidOperationException("ProjectManager không thể xoá Manager khác.");
- managerCMN\managerCMN\Services\Implementations\ProjectService.cs:226:?? throw new InvalidOperationException("Thành viên không tồn tại.");
- managerCMN\managerCMN\Services\Implementations\ProjectService.cs:235:throw new InvalidOperationException("Không thể bổ nhiệm ProjectOwner thứ hai.");
- managerCMN\managerCMN\Services\Implementations\ProjectService.cs:243:?? throw new InvalidOperationException("Thành viên không tồn tại.");
- managerCMN\managerCMN\Services\Implementations\ProjectService.cs:246:throw new InvalidOperationException("Không thể thay đổi role của ProjectOwner.");
- managerCMN\managerCMN\Services\Implementations\ProjectTaskService.cs:107:?? throw new InvalidOperationException("Task không tồn tại.");
- managerCMN\managerCMN\Services\Implementations\ProjectTaskService.cs:114:throw new InvalidOperationException("Task đã hoàn thành nên không thể hoàn tác trạng thái.");
- managerCMN\managerCMN\Services\Implementations\ProjectTaskService.cs:168:?? throw new InvalidOperationException("Task không tồn tại.");
- managerCMN\managerCMN\Services\Implementations\ProjectTaskService.cs:172:throw new InvalidOperationException("Task đã hoàn thành nên không thể xoá hoặc hoàn tác.");
- managerCMN\managerCMN\Services\Implementations\ProjectTaskService.cs:176:throw new UnauthorizedAccessException("ProjectStaff không được xoá task.");
- managerCMN\managerCMN\Services\Implementations\ProjectTaskService.cs:193:?? throw new InvalidOperationException("Task không tồn tại.");
- managerCMN\managerCMN\Services\Implementations\ProjectTaskService.cs:200:throw new InvalidOperationException("Task đã hoàn thành nên không thể hoàn tác trạng thái.");
- managerCMN\managerCMN\Services\Implementations\ProjectTaskService.cs:238:?? throw new InvalidOperationException("Task không tồn tại.");
- managerCMN\managerCMN\Services\Implementations\ProjectTaskService.cs:245:throw new InvalidOperationException("Task đã hoàn thành nên không thể giảm tiến độ.");
- managerCMN\managerCMN\Services\Implementations\ProjectTaskService.cs:251:throw new InvalidOperationException("Task đang ở chế độ Auto. Chuyển sang Manual để nhập % thủ công.");
- managerCMN\managerCMN\Services\Implementations\ProjectTaskService.cs:278:?? throw new InvalidOperationException("Task không tồn tại.");
- managerCMN\managerCMN\Services\Implementations\ProjectTaskService.cs:284:throw new UnauthorizedAccessException("ProjectStaff không được phân công task.");
- managerCMN\managerCMN\Services\Implementations\ProjectTaskService.cs:298:?? throw new InvalidOperationException("Task không tồn tại.");
- managerCMN\managerCMN\Services\Implementations\ProjectTaskService.cs:302:throw new InvalidOperationException("Task đã hoàn thành nên không thể thêm checklist mới.");
- managerCMN\managerCMN\Services\Implementations\ProjectTaskService.cs:340:?? throw new InvalidOperationException("Checklist item không tồn tại.");
- managerCMN\managerCMN\Services\Implementations\ProjectTaskService.cs:345:throw new InvalidOperationException("Task đã hoàn thành nên không thể thay đổi checklist.");
- managerCMN\managerCMN\Services\Implementations\ProjectTaskService.cs:348:throw new InvalidOperationException("Checklist đã hoàn thành nên không thể bỏ tick.");
- managerCMN\managerCMN\Services\Implementations\ProjectTaskService.cs:371:?? throw new InvalidOperationException("Checklist item không tồn tại.");
- managerCMN\managerCMN\Services\Implementations\ProjectTaskService.cs:375:throw new InvalidOperationException("Không thể xoá checklist đã hoàn thành.");
- managerCMN\managerCMN\Services\Implementations\ProjectTaskService.cs:390:?? throw new InvalidOperationException("Task không tồn tại.");
- managerCMN\managerCMN\Services\Implementations\ProjectTaskService.cs:396:throw new InvalidOperationException("Vui lòng nhập nội dung hoặc đính kèm tệp.");
- managerCMN\managerCMN\Services\Implementations\ProjectTaskService.cs:400:throw new InvalidOperationException(validation!.ErrorMessage ?? "Tệp đính kèm không hợp lệ.");
- managerCMN\managerCMN\Services\Implementations\ProjectTaskService.cs:426:throw new InvalidOperationException("Cơ sở dữ liệu chưa cập nhật phần nhật ký công việc. Cần chạy database update trước khi gửi cập nhật/file.");
- managerCMN\managerCMN\Services\Implementations\ProjectTaskService.cs:445:throw new InvalidOperationException("Cơ sở dữ liệu chưa cập nhật phần file đính kèm của task.");
- managerCMN\managerCMN\Services\Implementations\ProjectTaskService.cs:468:throw new UnauthorizedAccessException("Chỉ người được giao task này mới được đánh dấu hoàn thành. ProjectOwner có thể tick hộ; ProjectManager không được tick hộ nếu không được giao task.");
- managerCMN\managerCMN\Services\Implementations\ProjectTaskService.cs:486:throw new InvalidOperationException("Hạn hoàn thành không được nhỏ hơn ngày bắt đầu.");
- managerCMN\managerCMN\Services\Implementations\ProjectTaskService.cs:489:?? throw new InvalidOperationException("Dự án không tồn tại.");
- managerCMN\managerCMN\Services\Implementations\ProjectTaskService.cs:497:throw new InvalidOperationException("Subtask phải có ngày bắt đầu và ngày kết thúc.");
- managerCMN\managerCMN\Services\Implementations\ProjectTaskService.cs:501:throw new InvalidOperationException("Task cha không hợp lệ.");
- managerCMN\managerCMN\Services\Implementations\ProjectTaskService.cs:504:throw new InvalidOperationException("Task cha phải có ngày bắt đầu và ngày kết thúc trước khi tạo hoặc cập nhật subtask.");
- managerCMN\managerCMN\Services\Implementations\ProjectTaskService.cs:508:throw new InvalidOperationException("Không thể tạo task sâu hơn 5 cấp.");
- managerCMN\managerCMN\Services\Implementations\ProjectTaskService.cs:531:throw new InvalidOperationException("Task cha có subtask phải giữ đủ ngày bắt đầu và ngày kết thúc.");
- managerCMN\managerCMN\Services\Implementations\ProjectTaskService.cs:556:throw new InvalidOperationException(message);
- managerCMN\managerCMN\Services\Implementations\ProjectTaskService.cs:559:throw new InvalidOperationException(message);
- managerCMN\managerCMN\Services\Implementations\RequestService.cs:117:throw new InvalidOperationException("Không thể trừ phép - số dư không đủ hoặc có lỗi xảy ra.");
- managerCMN\managerCMN\Services\Implementations\RequestService.cs:357:throw new InvalidOperationException("Chỉ có thể hoàn duyệt đơn đã được duyệt.");
- managerCMN\managerCMN\Services\Implementations\RequestService.cs:526:throw new ValidationException("Ngày kết thúc phải lớn hơn hoặc bằng ngày bắt đầu.");
- managerCMN\managerCMN\Services\Implementations\TicketService.cs:124:throw new KeyNotFoundException($"Ticket {ticketId} was not found.");
- managerCMN\managerCMN\Services\Implementations\TicketService.cs:128:throw new UnauthorizedAccessException("Current employee cannot resolve this ticket.");
- managerCMN\managerCMN\Services\Implementations\TicketService.cs:234:throw new KeyNotFoundException($"Ticket {ticketId} was not found.");
- managerCMN\managerCMN\Services\Implementations\TicketService.cs:241:throw new UnauthorizedAccessException("Current employee cannot reply to this ticket.");
- managerCMN\managerCMN\Services\Implementations\TicketService.cs:300:throw new KeyNotFoundException($"Ticket {ticketId} was not found.");
- managerCMN\managerCMN\Services\Implementations\TicketService.cs:307:throw new UnauthorizedAccessException("Current employee cannot forward this ticket.");
- managerCMN\managerCMN\Services\Implementations\TicketService.cs:453:throw new KeyNotFoundException($"Ticket {ticketId} was not found.");
- managerCMN\managerCMN\Services\Implementations\TicketService.cs:460:throw new UnauthorizedAccessException("Current employee cannot star this ticket.");

## 7) Action thong bao phia frontend (toast, panel notice, alert)

- managerCMN\managerCMN\Controllers\SetupController.cs:48:ViewBag.Message = "Lỗi: Không tìm thấy role Admin trong hệ thống.";
- managerCMN\managerCMN\Controllers\SetupController.cs:60:ViewBag.Message = "Hệ thống đã có tài khoản Admin.";
- managerCMN\managerCMN\Controllers\SetupController.cs:65:ViewBag.Message = "Chưa có tài khoản Admin. Vui lòng đăng nhập bằng Google trước, sau đó gán role Admin.";
- managerCMN\managerCMN\Views\Attendance\Index.cshtml:705:alert('Lỗi: Không lấy được thông tin nhân viên hoặc ngày. EmployeeId=' + employeeId + ', Date=' + date);
- managerCMN\managerCMN\Views\Contract\Create.cshtml:216:alert('Vui lòng chọn ngày bắt đầu trước.');
- managerCMN\managerCMN\Views\Contract\Edit.cshtml:200:if (!val) { alert('Vui lòng chọn ngày kết thúc mới.'); return; }
- managerCMN\managerCMN\Views\Profile\Edit.cshtml:393:alert('Họ tên phải có ít nhất 2 ký tự');
- managerCMN\managerCMN\Views\Request\Approve.cshtml:619:alert('Vui lòng chọn ít nhất một đơn để duyệt.');
- managerCMN\managerCMN\Views\Request\Approve.cshtml:643:alert('Vui lòng chọn ít nhất một đơn để từ chối.');
- managerCMN\managerCMN\Views\Request\Approve.cshtml:670:alert('Vui lòng nhập lý do từ chối.');
- managerCMN\managerCMN\Views\Request\Approve.cshtml:716:alert('Lỗi: Không tìm thấy mã đơn. Vui lòng thử lại.');
- managerCMN\managerCMN\Views\Request\Approve.cshtml:722:alert('Vui lòng nhập lý do từ chối.');
- managerCMN\managerCMN\Views\Request\Approve.cshtml:758:alert('Lỗi: Không tìm thấy mã đơn. Vui lòng thử lại.');
- managerCMN\managerCMN\Views\Request\Approve.cshtml:764:alert('Vui lòng nhập lý do hoàn duyệt.');
- managerCMN\managerCMN\Views\Request\Approve.cshtml:787:alert('Lỗi: Không tìm thấy mã đơn. Vui lòng thử lại.');
- managerCMN\managerCMN\Views\Request\Details.cshtml:380:alert('Vui lòng nhập lý do hoàn duyệt.');
- managerCMN\managerCMN\Views\Settings\Index.cshtml:1385:alert('Vui lòng chọn vai trò');
- managerCMN\managerCMN\Views\Settings\Index.cshtml:1403:alert(response.message);
- managerCMN\managerCMN\Views\Settings\Index.cshtml:1405:alert(response.message || 'Có lỗi xảy ra!');
- managerCMN\managerCMN\Views\Settings\Index.cshtml:1409:alert('Có lỗi xảy ra khi lưu phân quyền!');
- managerCMN\managerCMN\Views\Ticket\Create.cshtml:487:alert('Vui lòng chọn ít nhất một người nhận!');
- managerCMN\managerCMN\Views\Ticket\Details.cshtml:821:alert('Vui lòng chọn ít nhất một người nhận!');
- managerCMN\managerCMN\Views\Ticket\Index.cshtml:89:window.alert('Khong the cap nhat dau sao luc nay.');
- managerCMN\managerCMN\wwwroot\js\project-management.js:1106:function showToast(message, type = 'info') {
- managerCMN\managerCMN\wwwroot\js\project-management.js:160:showPanelNotice(data.message || 'Đã cập nhật trạng thái.', 'success');
- managerCMN\managerCMN\wwwroot\js\project-management.js:164:showPanelNotice(error.message || 'Không cập nhật được trạng thái.', 'danger');
- managerCMN\managerCMN\wwwroot\js\project-management.js:182:showPanelNotice('Không tìm thấy thanh tiến độ.', 'danger');
- managerCMN\managerCMN\wwwroot\js\project-management.js:200:showPanelNotice(data.message || 'Đã cập nhật tiến độ.', 'success');
- managerCMN\managerCMN\wwwroot\js\project-management.js:202:showPanelNotice(error.message || 'Không cập nhật được tiến độ.', 'danger');
- managerCMN\managerCMN\wwwroot\js\project-management.js:217:showPanelNotice('Không tìm thấy trạng thái của task.', 'danger');
- managerCMN\managerCMN\wwwroot\js\project-management.js:223:showPanelNotice('Chỉ assignee hoặc Owner được tick hoàn thành.', 'danger');
- managerCMN\managerCMN\wwwroot\js\project-management.js:247:showPanelNotice('Checklist đã hoàn thành nên không thể bỏ tick.', 'warning');
- managerCMN\managerCMN\wwwroot\js\project-management.js:262:showPanelNotice(data.message || 'Đã cập nhật checklist.', 'success');
- managerCMN\managerCMN\wwwroot\js\project-management.js:266:showPanelNotice(error.message || 'Không cập nhật được checklist.', 'danger');
- managerCMN\managerCMN\wwwroot\js\project-management.js:278:showPanelNotice('Bấm lại nút xoá checklist để xác nhận.', 'warning');
- managerCMN\managerCMN\wwwroot\js\project-management.js:296:showPanelNotice(data.message || 'Đã xoá checklist.', 'success');
- managerCMN\managerCMN\wwwroot\js\project-management.js:299:showPanelNotice(error.message || 'Không xoá được checklist.', 'danger');
- managerCMN\managerCMN\wwwroot\js\project-management.js:325:showPanelNotice(data.message || 'Đã thêm checklist.', 'success');
- managerCMN\managerCMN\wwwroot\js\project-management.js:327:showPanelNotice(error.message || 'Không thêm được checklist.', 'danger');
- managerCMN\managerCMN\wwwroot\js\project-management.js:41:showToast('Không tìm thấy khu vực hiển thị chi tiết task.', 'danger');
- managerCMN\managerCMN\wwwroot\js\project-management.js:435:showPanelNotice(data.message || 'Đã gửi cập nhật công việc.', 'success');
- managerCMN\managerCMN\wwwroot\js\project-management.js:437:showPanelNotice(error.message || 'Không gửi được cập nhật công việc.', 'danger');
- managerCMN\managerCMN\wwwroot\js\project-management.js:462:showPanelNotice(data.message || 'Đã lưu phân công.', 'success');
- managerCMN\managerCMN\wwwroot\js\project-management.js:464:showPanelNotice(error.message || 'Không lưu được phân công.', 'danger');
- managerCMN\managerCMN\wwwroot\js\project-management.js:489:showPanelNotice(data.message || 'Tạo subtask thành công.', 'success');
- managerCMN\managerCMN\wwwroot\js\project-management.js:491:showPanelNotice(error.message || 'Không tạo được subtask.', 'danger');
- managerCMN\managerCMN\wwwroot\js\project-management.js:575:showPanelNotice(`Chỉ được upload tối đa 2 files. Bạn đã chọn ${fileCount} files.`, 'danger');
- managerCMN\managerCMN\wwwroot\js\project-management.js:586:function showPanelNotice(message, type = 'info') {
- managerCMN\managerCMN\wwwroot\js\project-management.js:589:showToast(message, type);
- managerCMN\managerCMN\wwwroot\js\project-management.js:60:.catch(() => showToast('Không tải được chi tiết task. Mình đã chặn lỗi im lặng; nếu vẫn còn, mình sẽ truy tiếp route này.', 'danger'));
- managerCMN\managerCMN\wwwroot\js\site.js:212:alert('Vui lòng khắc phục các lỗi file upload trước khi gửi form.');

## 8) Ghi chu trien khai tiep theo

- Moi dong da gom: file, line, action message hoac statement.
- Co the dung file nay de chuan hoa he thong notification theo ma mau: success, info, warning, error, blocked, validation.
- Co the tach theo module uu tien: ProjectTask, Request, Settings, Employee, Contract, Attendance, Ticket.
