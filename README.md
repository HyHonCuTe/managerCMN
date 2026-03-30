# managerCMN

Hệ thống quản lý nội bộ doanh nghiệp gồm 2 phần chính:

- `managerCMN/managerCMN`: ứng dụng ASP.NET Core MVC quản lý nhân sự, chấm công, đơn từ, tài sản, hợp đồng, ticket, thông báo và phân quyền.
- `PY_post`: script Python đồng bộ dữ liệu chấm công từ máy ZKTeco/ZK sang API của ứng dụng web.

README này được viết lại theo đúng mã nguồn hiện có trong repo, ưu tiên các bước cài đặt và vận hành thực tế thay vì mô tả chung chung.

## 1. Tổng quan chức năng

### Ứng dụng web `managerCMN`

- Đăng nhập Google OAuth 2.0 bằng email nhân viên đã tồn tại trong hệ thống.
- `DevLogin` cho môi trường `Development`.
- Quản lý nhân viên, phòng ban, chức vụ, vị trí, người duyệt.
- Quản lý hợp đồng lao động và theo dõi hợp đồng sắp hết hạn.
- Quản lý chấm công:
  - import Excel;
  - đồng bộ API từ thiết bị chấm công;
  - tính giờ làm, giờ tăng ca, đi muộn;
  - xem lịch theo tháng;
  - xuất Excel;
  - báo cáo đi muộn;
  - cấu hình danh sách nhân sự được tính đủ công.
- Quản lý đơn từ:
  - nghỉ phép;
  - quên check-in/check-out;
  - vắng mặt;
  - work from home;
  - luồng duyệt 2 cấp;
  - duyệt hàng loạt;
  - hoàn tác duyệt;
  - kiểm tra số phép còn lại.
- Quản lý tài sản:
  - CRUD tài sản;
  - import Excel;
  - gán/trả tài sản;
  - lịch sử vòng đời tài sản;
  - xem tài sản cá nhân.
- Hệ thống ticket nội bộ:
  - tạo ticket;
  - nhiều người nhận;
  - reply/forward;
  - gán người xử lý;
  - đính kèm;
  - đóng ticket.
- Dashboard, thông báo, hồ sơ cá nhân, nhật ký hệ thống.
- Phân quyền theo role và permission, dữ liệu seed sẵn qua EF Core migration.

### Đồng bộ chấm công `PY_post`

- Kết nối máy chấm công ZK qua `pyzk`.
- Đọc log chấm công mới theo kiểu incremental.
- Gửi batch dữ liệu lên `POST /api/attendance/punch`.
- Có khóa chống chạy song song.
- Có retry, log file, cron, cấu hình bỏ qua khung giờ đêm.
- Thiết kế để chạy trên Linux/Unix, không phải đường chạy chính trên Windows.

## 2. Công nghệ sử dụng

| Thành phần | Công nghệ |
| --- | --- |
| Web framework | ASP.NET Core MVC |
| Runtime target | .NET 9 (`net9.0`) |
| ORM | Entity Framework Core SQL Server |
| Database | SQL Server |
| Auth | Cookie + Google OAuth |
| Excel | ClosedXML |
| Frontend | Razor Views + Bootstrap + jQuery |
| Attendance sync | Python + `pyzk` + `requests` + `pytz` |

## 3. Cấu trúc repo

```text
.
├── managerCMN/
│   ├── managerCMN.slnx
│   ├── README.md                  # README ngắn, trỏ về tài liệu gốc
│   ├── DEPLOY.md
│   ├── deploy.sh
│   ├── DownANDrestoreDB.txt
│   ├── nginx_config_crm.conf
│   ├── sql_admin01.sql
│   ├── trienkhaiupdate.txt
│   └── managerCMN/
│       ├── Program.cs
│       ├── appsettings.json
│       ├── appsettings.Development.json
│       ├── appsettings.Production.json
│       ├── Controllers/
│       ├── Data/
│       ├── Models/
│       ├── Repositories/
│       ├── Services/
│       ├── Views/
│       ├── Migrations/
│       └── wwwroot/
└── PY_post/
    ├── post.py
    ├── config.json
    ├── requirements.txt
    ├── install.sh
    └── README.md
```

Lưu ý: tên thư mục bị lặp (`managerCMN/managerCMN`), nên khi chạy lệnh cần phân biệt:

- thư mục solution: `managerCMN/`
- thư mục project ASP.NET: `managerCMN/managerCMN/`

## 4. Hành vi hệ thống cần biết trước khi cài

- Ứng dụng web dùng `SQL Server` duy nhất. Không có đường chạy SQLite trong mã nguồn chính.
- Migration không tự chạy khi app khởi động. Phần auto-migrate trong `Program.cs` đang bị comment.
- Đăng nhập Google chỉ thành công nếu email đã tồn tại trong bảng `Employees`.
- User mới tạo qua Google sẽ được tạo tự động trong bảng `Users`.
- Role seed mặc định:
  - `Admin`
  - `Manager`
  - `User`
- Permission seed mặc định: 25 quyền.
- Các `JobTitle` mặc định cũng được seed qua migration.
- API chấm công nhận `AttendanceCode`, không phải `EmployeeCode`.
- Ứng dụng được cấu hình culture `vi-VN`.

## 5. Yêu cầu môi trường

### Web app

- .NET SDK 9.x
- SQL Server 2019+ hoặc SQL Server Express
- Visual Studio 2022, VS Code hoặc CLI
- Windows hoặc Linux đều chạy được phần web

### Python sync

- Python 3.8+ trên Linux/Unix
- `cron`
- mạng tới máy chấm công ZK
- mạng HTTPS tới server web

## 6. Cấu hình bắt buộc

### 6.1. `managerCMN/managerCMN/appsettings.json`

Không nên dùng nguyên giá trị đang có trong repo cho môi trường thật. Hãy thay bằng cấu hình của riêng bạn:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=managerCMN;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "Authentication": {
    "Google": {
      "ClientId": "YOUR_GOOGLE_CLIENT_ID",
      "ClientSecret": "YOUR_GOOGLE_CLIENT_SECRET"
    }
  },
  "ApiSettings": {
    "AttendanceApiKey": "CHANGE_THIS_API_KEY"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

### 6.2. `managerCMN/managerCMN/appsettings.Production.json`

- Dùng connection string riêng cho production.
- Nếu deploy Linux behind Nginx, hãy cấu hình `ASPNETCORE_ENVIRONMENT=Production`.
- Review lại toàn bộ domain, secret, user SQL trước khi publish.

### 6.3. `PY_post/config.json`

```json
{
  "device": {
    "ip": "192.168.100.13",
    "port": 4370,
    "timeout": 10
  },
  "api": {
    "endpoint": "https://your-domain/api/attendance/punch",
    "key": "CHANGE_THIS_API_KEY",
    "timeout": 60
  },
  "sync": {
    "interval_minutes": 5,
    "skip_night_hours": true,
    "max_retries": 3,
    "retry_delay_seconds": 10,
    "batch_size": 500,
    "batch_threshold": 100,
    "first_time_days": 30
  }
}
```

## 7. Cài đặt local cho web app

### 7.1. Restore và build

Từ thư mục gốc repo:

```bash
cd managerCMN
dotnet restore managerCMN.slnx
dotnet build managerCMN.slnx -p:UseAppHost=false
```

Ghi chú:

- `-p:UseAppHost=false` hữu ích khi máy đang có tiến trình giữ file `apphost.exe` hoặc `managerCMN.exe`.
- Nếu build bình thường bị lỗi `Access to the path ... apphost.exe is denied`, hãy tắt tiến trình `dotnet`/ứng dụng đang chạy rồi build lại.

### 7.2. Chuẩn bị database

Ví dụ tạo database trống:

```sql
CREATE DATABASE managerCMN;
GO
```

### 7.3. Chạy migration

Từ thư mục project:

```bash
cd managerCMN/managerCMN
dotnet ef database update
```

Do migration không auto-run khi startup, bước này là bắt buộc.

### 7.4. Bootstrap tài khoản đầu tiên

#### Cách A: development

- Chạy app bằng `Development`.
- Vào trang đăng nhập.
- Dùng nút `DevLogin`.
- Nếu database chưa có employee, app vẫn có thể tạo user dev admin, nhưng bạn nên tạo dữ liệu nhân viên sớm để các luồng liên quan `EmployeeId` hoạt động đầy đủ.

#### Cách B: production hoặc dữ liệu thật

- Tạo trước một bản ghi `Employees` với email Google hợp lệ.
- Đăng nhập bằng Google để app tự tạo `Users`.
- Truy cập `/Setup` để gán role `Admin` nếu hệ thống chưa có admin.
- Hoặc sửa email trong `managerCMN/sql_admin01.sql`, sau đó chạy script này để tạo employee + user + admin role trực tiếp trong SQL Server.

Lưu ý quan trọng:

- `sql_admin01.sql` hiện là script theo môi trường thật, có email hard-code. Phải sửa trước khi dùng.
- Trong `AccountController` đang có logic gán `Admin` tự động cho một email master cố định. Hãy review lại trước khi đưa lên môi trường mới.

### 7.5. Chạy ứng dụng local

Từ thư mục project:

```bash
dotnet run --launch-profile https
```

Launch profile hiện có:

- HTTP: `http://localhost:5257`
- HTTPS + HTTP: `https://localhost:7048`, `http://localhost:5257`

Khuyến nghị local:

- dùng `dotnet run --launch-profile https`
- hoặc `dotnet run --launch-profile http`
- không chạy trực tiếp `bin/Debug/net9.0/managerCMN.dll` ở `Production` trên Windows nếu bạn chưa cấu hình HTTPS port và quyền Event Log

## 8. Luồng đăng nhập và phân quyền

### 8.1. Đăng nhập

- `Development`: dùng `DevLogin`.
- `Production`: dùng Google OAuth.
- Google login bị chặn nếu email chưa có trong bảng `Employees`.

### 8.2. Role

- `Admin`: toàn quyền hệ thống.
- `Manager`: một phần quyền nhân sự, đơn từ, chấm công, tài sản.
- `User`: quyền cơ bản.

### 8.3. Permission seed

Các nhóm quyền mặc định:

- `Employee.*`
- `Request.*`
- `Attendance.*`
- `Asset.*`
- `Settings.*`
- `System.*`

Một số policy đang dùng trong controller:

- `AdminOnly`
- `ManagerOrAdmin`
- `Authenticated`

## 9. Nghiệp vụ chính theo module

### 9.1. Nhân sự

- CRUD nhân viên.
- Import nhân viên từ Excel.
- Điều chỉnh phép thủ công.
- Quản lý thông tin cơ bản, liên hệ, mã chấm công, phòng ban, chức vụ, vị trí.

### 9.2. Hợp đồng

- CRUD hợp đồng.
- Import Excel.
- Upload file hợp đồng.
- Theo dõi hợp đồng sắp hết hạn.

### 9.3. Chấm công

- Import Excel.
- Nhận dữ liệu từ API chấm công.
- Tính giờ làm, đi muộn, giờ tăng ca.
- Xem punch record theo ngày.
- Recalculate giờ công và late minutes bằng action admin.
- Export Excel tổng hợp tháng.

### 9.4. Đơn từ

Các loại đơn:

- `Leave`
- `CheckInOut`
- `Absence`
- `WorkFromHome`

Các lý do chính:

- nghỉ phép hưởng lương;
- nghỉ không lương;
- nghỉ ốm có giấy;
- nghỉ tang;
- thai sản;
- kết hôn;
- quên check-in/check-out;
- công việc công ty;
- việc cá nhân;
- làm việc tại nhà.

### 9.5. Tài sản

- CRUD tài sản.
- Import Excel.
- Gán/trả tài sản.
- Theo dõi lịch sử vòng đời.
- Quản lý brand, supplier, asset category trong phần settings.

### 9.6. Ticket

- Tạo ticket.
- Gửi tới nhiều người nhận.
- Reply/forward.
- Gán người xử lý.
- Resolve/close.
- Download attachment.

### 9.7. Dashboard và logs

- Thống kê tổng quan.
- Thông báo chưa đọc.
- Nhật ký hệ thống.
- Lịch sử POST API chấm công (`PostHistory`) hiển thị trên dashboard.

## 10. Upload file và giới hạn hiện tại

Theo `Helpers/FileUploadHelper.cs`:

- tối đa `5 MB` mỗi file;
- tối đa `2 file` mỗi lần upload;
- Excel: `.xlsx`, `.xls`;
- tài liệu: `.pdf`, `.doc`, `.docx`, `.txt`;
- hình ảnh: `.jpg`, `.jpeg`, `.png`, `.gif`, `.bmp`.

Đảm bảo thư mục upload có quyền ghi:

- `wwwroot/uploads/contracts`
- `wwwroot/uploads/requests`
- `wwwroot/uploads/tickets`
- các thư mục con khác do ứng dụng/sync tạo trong runtime

## 11. API chấm công

### Endpoint

```http
POST /api/attendance/punch
```

### Header

```http
X-API-Key: <AttendanceApiKey>
Content-Type: application/json
```

### Body

```json
[
  {
    "UserId": "A00001",
    "Time": "2026-03-24T08:01:00+07:00"
  }
]
```

Ý nghĩa:

- `UserId`: `AttendanceCode` của nhân viên.
- `Time`: thời điểm chấm công, sẽ được chuẩn hóa sang giờ Việt Nam.

### Response thành công

```json
{
  "message": "Import thành công.",
  "count": 1
}
```

### Điều kiện để API hoạt động đúng

- `ApiSettings:AttendanceApiKey` trên web phải khớp `PY_post/config.json`.
- Nhân viên phải có `AttendanceCode`.
- Database đã migrate đầy đủ.

## 12. Cài đặt `PY_post`

`PY_post` dành cho máy trung gian Linux/Unix hoặc server nội bộ có thể kết nối cả máy chấm công và web server.

### 12.1. Cài nhanh

```bash
cd PY_post
chmod +x install.sh
./install.sh
```

Script `install.sh` sẽ:

- kiểm tra Python 3 và `pip3`;
- validate `config.json`;
- copy file vào `/opt/attendance-sync`;
- cài dependencies;
- test kết nối máy chấm công;
- tạo cron mỗi 5 phút.

### 12.2. Chạy tay để test

```bash
cd /opt/attendance-sync
python3 post.py
```

### 12.3. Hành vi sync

- có file lock `/tmp/attendance_sync.lock`;
- bỏ qua khung giờ `20:00` đến `07:00` nếu `skip_night_hours=true`;
- lần sync đầu chỉ lấy dữ liệu của `30` ngày gần nhất theo mặc định;
- gửi batch nếu dữ liệu lớn hơn `batch_threshold`;
- retry theo `max_retries`.

### 12.4. Lưu ý tương thích

- `post.py` import `fcntl`, vì vậy đây là script hướng Linux/Unix.
- Không nên xem đây là script chạy chuẩn trên Windows.

## 13. Deploy production cho web app

Target deploy hiện tại trong repo là Ubuntu + Nginx + SQL Server + systemd.

### 13.1. Các file vận hành có sẵn

- `managerCMN/DEPLOY.md`
- `managerCMN/deploy.sh`
- `managerCMN/nginx_config_crm.conf`
- `managerCMN/trienkhaiupdate.txt`
- `managerCMN/DownANDrestoreDB.txt`

### 13.2. Quy trình khuyến nghị

1. Cài .NET runtime/SDK và SQL Server trên server.
2. Clone repo vào thư mục ổn định, ví dụ `/var/www/cmnmanager-src`.
3. Cấu hình `appsettings.Production.json`.
4. Publish ứng dụng.
5. Chạy migration.
6. Tạo và phân quyền thư mục upload.
7. Cấu hình `systemd`.
8. Cấu hình `Nginx` reverse proxy.

### 13.3. Lệnh publish mẫu

```bash
cd /var/www/cmnmanager-src/managerCMN/managerCMN/managerCMN
dotnet restore
dotnet publish -c Release -o ./bin/Release/publish
ASPNETCORE_ENVIRONMENT=Production dotnet ef database update
```

### 13.4. Quyền thư mục upload

Ví dụ:

```bash
mkdir -p ./bin/Release/publish/wwwroot/uploads
chown -R www-data:www-data ./bin/Release/publish/wwwroot/uploads
chmod -R 775 ./bin/Release/publish/wwwroot/uploads
```

### 13.5. Lưu ý với `deploy.sh`

Script `managerCMN/deploy.sh` hiện:

- có `git reset --hard origin/main`;
- có logic backup kiểu SQLite cũ không phải đường chạy chính của dự án;
- set quyền `777` cho thư mục upload;
- giả định cấu trúc thư mục server rất cụ thể.

Chỉ dùng script này khi bạn đã review kỹ và chấp nhận đúng môi trường đó.

## 14. Backup và restore

Repo có sẵn tài liệu ghi chú:

- `managerCMN/DownANDrestoreDB.txt`
- `managerCMN/sql_admin01.sql`

Ngoài ra, khi chạy production, bạn nên backup ít nhất 2 phần:

- SQL Server database
- `wwwroot/uploads`

Nếu dựng hệ thống mới, đừng quên khôi phục cả file upload chứ không chỉ database.

## 15. Kiểm thử và smoke test nên làm sau khi cài

Hiện repo chưa có project test tự động. Cách kiểm tra tối thiểu:

### Web app

```bash
cd managerCMN
dotnet build managerCMN.slnx -p:UseAppHost=false
cd managerCMN
dotnet ef database update
dotnet run --launch-profile https
```

Sau đó kiểm tra:

- mở `/Account/Login`;
- đăng nhập `DevLogin` ở local hoặc Google ở production;
- tạo/sửa một nhân viên;
- import thử Excel nhân viên hoặc chấm công;
- tạo một đơn từ;
- upload một file hợp đồng;
- tạo một ticket có attachment;
- mở `/Setup` nếu cần bootstrap admin;
- gọi thử API `/api/attendance/punch`.

### Python sync

- kiểm tra syntax bằng cách parse file hoặc chạy `python3 post.py`;
- test kết nối máy chấm công;
- test gọi API thật bằng `config.json`;
- kiểm tra log trong `logs/attendance_sync.log`.

## 16. Troubleshooting

### Lỗi build `apphost.exe` hoặc `managerCMN.exe` bị khóa

Nguyên nhân thường gặp:

- đang có tiến trình `dotnet` chạy project;
- Visual Studio/IIS Express giữ file build cũ.

Cách xử lý:

- dừng app đang chạy;
- đóng Visual Studio nếu cần;
- build lại với:

```bash
dotnet build managerCMN.slnx -p:UseAppHost=false
```

### Chạy DLL trực tiếp trên Windows ở `Production` nhưng request bị rớt

Nguyên nhân thường gặp:

- app cố redirect HTTPS nhưng không biết cổng HTTPS;
- logging Event Log trên Windows không có quyền ghi.

Khuyến nghị:

- local hãy dùng `dotnet run --launch-profile https` hoặc `http`;
- nếu chạy production trên Windows, cần review lại logging provider và HTTPS configuration.

### Đăng nhập Google bị báo không có quyền

Kiểm tra:

- email Google có tồn tại trong bảng `Employees` chưa;
- employee đó có `Email` đúng chính tả;
- Google OAuth callback URL đã cấu hình đúng chưa.

### `dotnet ef database update` không chạy

Kiểm tra:

- connection string đúng server chưa;
- SQL Server đã chạy chưa;
- build project thành công chưa;
- bạn đang đứng đúng thư mục project `managerCMN/managerCMN`.

### API chấm công trả `401 Unauthorized`

Kiểm tra:

- `X-API-Key` gửi lên có đúng không;
- `ApiSettings:AttendanceApiKey` trên web và `PY_post/config.json` có khớp không.

### Dữ liệu chấm công không map vào nhân viên

Kiểm tra:

- `UserId` gửi lên API có đúng `AttendanceCode` không;
- `AttendanceCode` trong bảng `Employees` có bị trùng hoặc để trống không.

### Upload file lỗi quyền

Kiểm tra:

- thư mục `wwwroot/uploads` có tồn tại không;
- process user chạy app có quyền ghi không;
- Nginx/systemd có chạy bằng user dự kiến không.

### `PY_post` không chạy trên Windows

Đây là giới hạn thiết kế hiện tại:

- script dùng `fcntl`;
- `install.sh` và cron đều là Unix-style.

Hãy triển khai `PY_post` trên Linux/Unix.

## 17. Khuyến nghị trước khi đưa lên production

- thay toàn bộ secret đang commit trong repo bằng secret riêng của môi trường mới;
- review email master admin hard-code trong `AccountController`;
- review `deploy.sh` trước khi chạy;
- xóa dữ liệu upload mẫu nếu không muốn public;
- đảm bảo backup database + uploads;
- xác nhận Google OAuth redirect URI đúng domain mới;
- kiểm tra phân quyền `Admin`, `Manager`, `User` sau migration.

## 18. Tài liệu tham chiếu nhanh trong repo

- Web app source: `managerCMN/managerCMN`
- Solution file: `managerCMN/managerCMN.slnx`
- Deploy notes: `managerCMN/DEPLOY.md`
- Deploy script: `managerCMN/deploy.sh`
- Admin bootstrap SQL: `managerCMN/sql_admin01.sql`
- Backup/restore notes: `managerCMN/DownANDrestoreDB.txt`
- Attendance sync: `PY_post/post.py`
- Attendance sync installer: `PY_post/install.sh`

## 19. Trạng thái xác minh khi viết README này

Các bước đã kiểm tra trực tiếp từ mã nguồn:

- web app build thành công với `dotnet build managerCMN.slnx -p:UseAppHost=false`;
- app có thể boot local bằng `dotnet bin/Debug/net9.0/managerCMN.dll --urls http://127.0.0.1:5099`;
- repo hiện chưa có test project tự động;
- `PY_post` đã được đọc và đối chiếu đầy đủ, nhưng cần môi trường Linux + thiết bị ZK thật để test end-to-end.

Vì vậy, README này phản ánh đúng kiến trúc và cách vận hành hiện có, nhưng việc nghiệm thu cuối cùng vẫn nên thực hiện trên:

- một SQL Server thật;
- một Google OAuth app thật;
- một máy ZK thật hoặc nguồn punch data thật;
- môi trường deploy Linux/Nginx nếu bạn dùng production path của repo.
