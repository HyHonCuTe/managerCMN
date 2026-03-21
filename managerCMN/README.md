# CMN Management System

Hệ thống quản lý nhân sự doanh nghiệp toàn diện với chấm công, quản lý nghỉ phép, và báo cáo Excel.

## Công nghệ sử dụng

| Thành phần | Phiên bản | Mục đích |
|------------|-----------|----------|
| .NET | 9.0 | Backend Framework |
| ASP.NET Core MVC | 9.0 | Web Framework |
| Entity Framework Core | 9.0 | ORM & Database |
| SQL Server | 2019+ | Database |
| Google OAuth 2.0 | - | Xác thực |
| ClosedXML | 0.104+ | Import/Export Excel |
| Bootstrap | 5.3 | Giao diện |

## Tính năng chính

### Quản lý nhân viên
- Hồ sơ nhân viên đầy đủ
- Import/Export Excel
- Quản lý phòng ban, chức vụ, vị trí
- Liên hệ khẩn cấp
- Xóa nhân viên (hard delete) kèm dữ liệu liên quan

### Quản lý hợp đồng
- 5 loại hợp đồng: Thử việc, Có thời hạn, Không thời hạn, Thời vụ, Bán thời gian
- Theo dõi lương và lịch sử
- Upload file hợp đồng

### Chấm công
- Check in/out với tính thời gian tự động
- Phát hiện đi muộn, làm thêm giờ
- Tích hợp ngày nghỉ lễ
- Xuất Excel với format P/K/L có màu

### Quản lý nghỉ phép
- Cấp phép theo quý (3 ngày/quý từ ngày 26)
- Phép bảo lưu từ năm trước (hết hạn 31/03)
- Điều chỉnh phép thủ công (Admin)
- Tự động tính toán và hiển thị chính xác

### Đơn từ & Phê duyệt
- 4 loại: Nghỉ phép, Sửa chấm công, Vắng mặt, Làm việc từ xa
- Quy trình phê duyệt nhiều cấp
- Đính kèm file
- Thông báo real-time

### Quản lý tài sản
- 7+ loại tài sản: Laptop, Màn hình, Điện thoại, Máy in...
- Phân bổ tài sản cho nhân viên
- Lịch sử và vòng đời tài sản

### Hệ thống Ticket nội bộ
- Tạo ticket nhiều người nhận
- Tin nhắn theo luồng
- Đính kèm file
- Độ ưu tiên và phân loại

## Cấu trúc dự án

```
managerCMN/
├── managerCMN/               # Dự án ASP.NET Core chính
│   ├── Controllers/          # 17+ MVC Controllers
│   ├── Models/
│   │   ├── Entities/         # 31+ Entity classes
│   │   ├── Enums/            # 21+ Enum definitions
│   │   └── ViewModels/       # 21+ ViewModels
│   ├── Views/                # Razor Views
│   ├── Data/                 # Database Context
│   ├── Services/             # Business Logic Layer
│   ├── Repositories/         # Data Access Layer
│   ├── Authorization/        # Custom Authorization
│   ├── Helpers/              # Utility Classes
│   ├── wwwroot/              # Static Files & Uploads
│   └── Migrations/           # EF Database Migrations
├── deploy.sh                 # Script deploy tự động
├── README.md                 # File này
└── DEPLOY.md                 # Hướng dẫn deploy chi tiết
```

## Cài đặt Development

### Yêu cầu
- .NET 9.0 SDK
- SQL Server 2019+ hoặc SQL Server Express
- Visual Studio 2022 / VS Code
- Git

### Các bước

```bash
# Clone repository
git clone https://github.com/HyHonCuTe/managerCMN.git
cd managerCMN/managerCMN

# Restore packages
dotnet restore

# Cấu hình connection string trong appsettings.json
# Sửa ConnectionStrings:DefaultConnection

# Chạy migrations
dotnet ef database update

# Chạy ứng dụng
dotnet run

# Truy cập: http://localhost:5257
```

## Production

- **Server**: Ubuntu 22.04.5 LTS
- **Database**: SQL Server với connection pooling
- **Web Server**: Nginx reverse proxy với SSL
- **Domain**: https://hyhon.io.vn
- **Service**: systemd với auto-restart

Xem **[DEPLOY.md](./DEPLOY.md)** để biết chi tiết triển khai.

## Lệnh thường dùng

```bash
# Kiểm tra service
systemctl status cmnmanager

# Xem logs
journalctl -u cmnmanager -f

# Restart service
systemctl restart cmnmanager

# Deploy mới
./deploy.sh
```

## Phiên bản

- **v2.2 (03/2026)**: Sửa lỗi xóa nhân viên, cộng phép thủ công, deploy script
- **v2.1 (03/2026)**: Quản lý ngày nghỉ lễ + Excel Export nâng cao
- **v2.0**: Production deployment với các tính năng nâng cao
- **v1.0**: Phiên bản đầu tiên với chức năng HR cơ bản

## Thông tin

- **Developer**: hyhoncute
- **Domain**: hyhon.io.vn
- **License**: Private Enterprise License
