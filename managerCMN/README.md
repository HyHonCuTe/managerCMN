# CMN Management System

Hệ thống quản lý nhân sự, chấm công, đơn từ, tài sản và ticket nội bộ doanh nghiệp.

## Mục lục

1. [Tổng quan](#tổng-quan)
2. [Tính năng](#tính-năng)
3. [Công nghệ sử dụng](#công-nghệ-sử-dụng)
4. [Cấu trúc dự án](#cấu-trúc-dự-án)
5. [Cài đặt môi trường Development](#cài-đặt-môi-trường-development)
6. [Deploy lên Ubuntu Server 22.04](#deploy-lên-ubuntu-server-2204)
7. [Quản lý SQL Server](#quản-lý-sql-server)
8. [Cấu hình hệ thống](#cấu-hình-hệ-thống)
9. [Hướng dẫn sử dụng](#hướng-dẫn-sử-dụng)
10. [Troubleshooting](#troubleshooting)

---

## Tổng quan

**CMN Management** là hệ thống quản lý nội bộ doanh nghiệp toàn diện, bao gồm:

- Quản lý nhân sự (Employee Management)
- Quản lý chấm công (Attendance Tracking)
- Quản lý đơn từ và quy trình duyệt (Request & Approval Workflow)
- Quản lý tài sản (Asset Management)
- Hệ thống ticket nội bộ (Internal Ticketing)
- Phân quyền chi tiết (Role-Based & Permission-Based Authorization)

---

## Tính năng

### Xác thực & Phân quyền
- Đăng nhập qua Google OAuth 2.0
- 3 vai trò: Admin, Manager, User
- 25 quyền chi tiết theo từng chức năng
- Hỗ trợ DevLogin cho môi trường development

### Quản lý Nhân sự
- CRUD nhân viên đầy đủ
- Import từ file Excel
- Quản lý phòng ban, chức vụ, vị trí
- Thông tin liên hệ khẩn cấp
- Theo dõi trạng thái nhân viên

### Quản lý Hợp đồng
- Các loại hợp đồng: Thử việc, Có thời hạn, Vô thời hạn, Thời vụ, Part-time
- Theo dõi lương, ngày bắt đầu/kết thúc
- Trạng thái hợp đồng

### Quản lý Chấm công
- Ghi nhận giờ vào/ra
- Tính giờ làm việc, tăng ca
- Phát hiện đi muộn
- Xem lịch chấm công
- Import/Export Excel

### Quản lý Đơn từ
- Các loại đơn: Nghỉ phép, Chấm công bù, Vắng mặt, Work From Home
- Quy trình duyệt nhiều cấp
- Đính kèm file
- Theo dõi trạng thái đơn

### Quản lý Tài sản
- Danh mục tài sản: Laptop, Màn hình, Điện thoại, Máy in...
- Gán tài sản cho nhân viên
- Theo dõi lịch sử tài sản
- Quản lý hãng, nhà cung cấp

### Hệ thống Ticket
- Tạo ticket nội bộ
- Nhiều người nhận
- Thread tin nhắn
- Đính kèm file
- Độ ưu tiên và mức độ khẩn cấp

### Dashboard & Báo cáo
- Tổng quan thống kê
- Nhật ký hệ thống
- Thông báo

---

## Công nghệ sử dụng

| Công nghệ | Phiên bản | Mục đích |
|-----------|-----------|----------|
| .NET | 9.0 | Framework |
| ASP.NET Core MVC | 9.0 | Web Framework |
| Entity Framework Core | 9.0 | ORM |
| SQL Server | 2019+ | Database |
| Google OAuth 2.0 | - | Authentication |
| ClosedXML | 0.104 | Excel Import/Export |
| Bootstrap | 5.3 | UI Framework |

---

## Cấu trúc dự án

```
managerCMN/
├── managerCMN/                    # Main project
│   ├── Controllers/               # 17 MVC Controllers
│   ├── Models/
│   │   ├── Entities/              # 31 Entity classes
│   │   ├── Enums/                 # 21 Enum types
│   │   └── ViewModels/            # 21 ViewModels
│   ├── Views/                     # Razor Views
│   ├── Data/
│   │   ├── ApplicationDbContext.cs
│   │   └── DataSeeder.cs
│   ├── Services/                  # Business Logic
│   ├── Repositories/              # Data Access
│   ├── Authorization/             # Custom Authorization
│   ├── Helpers/                   # Utility classes
│   ├── wwwroot/                   # Static files
│   ├── Migrations/                # EF Migrations
│   ├── Program.cs                 # Entry point
│   └── appsettings.json           # Configuration
└── README.md
```

---

## Cài đặt môi trường Development

### Yêu cầu
- .NET 9.0 SDK
- SQL Server 2019+ hoặc SQL Server Express
- Visual Studio 2022 / VS Code
- Git

### Các bước cài đặt

```bash
# 1. Clone repository
git clone <repository-url>
cd managerCMN/managerCMN

# 2. Restore packages
dotnet restore

# 3. Cấu hình connection string trong appsettings.json
# Sửa ConnectionStrings:DefaultConnection

# 4. Chạy migration
dotnet ef database update

# 5. Chạy ứng dụng
dotnet run

# Truy cập: http://localhost:5257
```

---

## Deploy lên Ubuntu Server 22.04

### Yêu cầu Server
- Ubuntu Server 22.04 LTS
- RAM: Tối thiểu 2GB (khuyến nghị 4GB+)
- CPU: 2 cores+
- Disk: 20GB+
- Kết nối SSH

### Bước 1: Cập nhật hệ thống

```bash
# SSH vào server
ssh username@your-server-ip

# Cập nhật packages
sudo apt update && sudo apt upgrade -y

# Cài đặt các công cụ cần thiết
sudo apt install -y wget curl gnupg2 software-properties-common apt-transport-https ca-certificates
```

### Bước 2: Cài đặt .NET 9.0 Runtime

```bash
# Thêm Microsoft package repository
wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb

# Cài đặt .NET 9.0 SDK và Runtime
sudo apt update
sudo apt install -y dotnet-sdk-9.0 aspnetcore-runtime-9.0

# Kiểm tra cài đặt
dotnet --version
```

### Bước 3: Cài đặt SQL Server 2022

```bash
# Import GPG key
curl https://packages.microsoft.com/keys/microsoft.asc | sudo tee /etc/apt/trusted.gpg.d/microsoft.asc

# Thêm SQL Server repository
sudo add-apt-repository "$(wget -qO- https://packages.microsoft.com/config/ubuntu/22.04/mssql-server-2022.list)"

# Cài đặt SQL Server
sudo apt update
sudo apt install -y mssql-server

# Cấu hình SQL Server
sudo /opt/mssql/bin/mssql-conf setup
# Chọn: 3 (Express - Free)
# Đặt password cho SA account (ví dụ: PASSWORD)
# Lưu ý: Password phải có ít nhất 8 ký tự, bao gồm chữ hoa, chữ thường, số và ký tự đặc biệt

# Kiểm tra SQL Server đang chạy
systemctl status mssql-server

# Cho phép SQL Server khởi động cùng hệ thống
sudo systemctl enable mssql-server
```

### Bước 4: Cài đặt SQL Server Command Line Tools

```bash
# Thêm repository cho mssql-tools
curl https://packages.microsoft.com/keys/microsoft.asc | sudo tee /etc/apt/trusted.gpg.d/microsoft.asc
curl https://packages.microsoft.com/config/ubuntu/22.04/prod.list | sudo tee /etc/apt/sources.list.d/mssql-release.list

# Cài đặt mssql-tools và unixodbc
sudo apt update
sudo ACCEPT_EULA=Y apt install -y mssql-tools18 unixodbc-dev

# Thêm vào PATH
echo 'export PATH="$PATH:/opt/mssql-tools18/bin"' >> ~/.bashrc
source ~/.bashrc

# Kiểm tra kết nối
sqlcmd -S localhost -U SA -P 'PASSWORD' -C -Q "SELECT @@VERSION"
```

### Bước 5: Tạo Database

```bash
# Kết nối SQL Server
sqlcmd -S localhost -U SA -P 'PASSWORD' -C

# Trong sqlcmd, chạy các lệnh sau:
CREATE DATABASE managerCMN;
GO

# Tạo user cho ứng dụng (optional, khuyến nghị)
CREATE LOGIN cmnapp WITH PASSWORD = 'PASSWORD
GO
USE managerCMN;
GO
CREATE USER cmnapp FOR LOGIN cmnapp;
GO
ALTER ROLE db_owner ADD MEMBER cmnapp;
GO
EXIT
```

### Bước 6: Cài đặt Nginx (Reverse Proxy)

```bash
# Cài đặt Nginx
sudo apt install -y nginx

# Khởi động và enable Nginx
sudo systemctl start nginx
sudo systemctl enable nginx

# Tạo file cấu hình cho ứng dụng
sudo nano /etc/nginx/sites-available/cmnmanager
```

**Nội dung file `/etc/nginx/sites-available/cmnmanager`:**

```nginx
server {
    listen 80;
    server_name hyhon.io.vn;  # Hoặc IP server

    location / {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }

    # Tăng giới hạn upload file
    client_max_body_size 50M;
}
```

```bash
# Kích hoạt site
sudo ln -s /etc/nginx/sites-available/cmnmanager /etc/nginx/sites-enabled/

# Xóa default site
sudo rm /etc/nginx/sites-enabled/default

# Kiểm tra cấu hình
sudo nginx -t

# Reload Nginx
sudo systemctl reload nginx
```

### Bước 7: Deploy ứng dụng

**Trên máy Windows (Development):**

```bash
# Build ứng dụng cho Linux
cd managerCMN/managerCMN
dotnet publish -c Release -r linux-x64 --self-contained false -o ./publish
```

**Upload lên server:**

```bash
# Sử dụng SCP từ Windows (PowerShell hoặc Git Bash)
scp -r ./publish/* username@your-server-ip:/var/www/cmnmanager/
```

**Hoặc trên server, clone trực tiếp từ Git:**

```bash
# Tạo thư mục ứng dụng
sudo mkdir -p /var/www/cmnmanager
sudo chown -R $USER:$USER /var/www/cmnmanager

# Clone repository
cd /var/www
git clone <repository-url> cmnmanager-src
cd cmnmanager-src/managerCMN

# Build
dotnet publish -c Release -o /var/www/cmnmanager
```

### Bước 8: Cấu hình appsettings.Production.json

```bash
# Tạo file cấu hình production
sudo nano /var/www/cmnmanager/appsettings.Production.json
```

**Nội dung:**

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=managerCMN;User Id=SA;Password=YourStrong@Passw0rd;TrustServerCertificate=True;MultipleActiveResultSets=true"
  },
  "Authentication": {
    "Google": {
      "ClientId": "YOUR_GOOGLE_CLIENT_ID",
      "ClientSecret": "YOUR_GOOGLE_CLIENT_SECRET"
    }
  },
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

### Bước 9: Chạy Migration

```bash
cd /var/www/cmnmanager

# Nếu có dotnet-ef tool
dotnet tool install --global dotnet-ef

# Hoặc chạy migration từ source code
cd /var/www/cmnmanager-src/managerCMN
ASPNETCORE_ENVIRONMENT=Production dotnet ef database update
```

### Bước 10: Tạo Systemd Service

```bash
# Tạo service file
sudo nano /etc/systemd/system/cmnmanager.service
```

**Nội dung:**

```ini
[Unit]
Description=CMN Manager ASP.NET Core Application
After=network.target mssql-server.service

[Service]
WorkingDirectory=/var/www/cmnmanager
ExecStart=/usr/bin/dotnet /var/www/cmnmanager/managerCMN.dll
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=cmnmanager
User=www-data
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://localhost:5000
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false

[Install]
WantedBy=multi-user.target
```

```bash
# Đặt quyền cho thư mục
sudo chown -R www-data:www-data /var/www/cmnmanager
sudo chmod -R 755 /var/www/cmnmanager

# Tạo thư mục uploads nếu chưa có
sudo mkdir -p /var/www/cmnmanager/wwwroot/uploads
sudo chown -R www-data:www-data /var/www/cmnmanager/wwwroot/uploads

# Reload systemd
sudo systemctl daemon-reload

# Khởi động service
sudo systemctl start cmnmanager
sudo systemctl enable cmnmanager

# Kiểm tra trạng thái
sudo systemctl status cmnmanager

# Xem logs
sudo journalctl -u cmnmanager -f
```

### Bước 11: Cấu hình Firewall

```bash
# Mở các port cần thiết
sudo ufw allow 22/tcp     # SSH
sudo ufw allow 80/tcp     # HTTP
sudo ufw allow 443/tcp    # HTTPS (nếu có)
sudo ufw allow 1433/tcp   # SQL Server (nếu cần truy cập từ xa)

# Bật firewall
sudo ufw enable
sudo ufw status
```

### Bước 12: (Optional) Cài đặt SSL với Let's Encrypt

```bash
# Cài đặt Certbot
sudo apt install -y certbot python3-certbot-nginx

# Lấy SSL certificate (thay your-domain.com bằng domain thực)
sudo certbot --nginx -d hyhon.io.vn

# Tự động renew
sudo systemctl enable certbot.timer
```

---

## Quản lý SQL Server

### Cách 1: Sử dụng sqlcmd (Command Line)

```bash
# Kết nối SQL Server
sqlcmd -S localhost -U SA -P 'password' -C

# Các lệnh hữu ích trong sqlcmd:

# Xem danh sách databases
SELECT name FROM sys.databases;
GO

# Chọn database
USE managerCMN;
GO

# Xem danh sách tables
SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE';
GO

# Query data
SELECT * FROM Employees;
GO

# Insert data
INSERT INTO Departments (DepartmentName, DepartmentCode, IsActive, CreatedAt)
VALUES (N'Phòng IT', 'IT', 1, GETDATE());
GO

# Thoát
EXIT
```

### Cách 2: Cài đặt Azure Data Studio (GUI - Khuyến nghị)

**Azure Data Studio** là công cụ GUI miễn phí của Microsoft, chạy được trên Windows/Mac/Linux.

**Trên máy Windows/Mac (Local):**

1. Tải về từ: https://docs.microsoft.com/en-us/sql/azure-data-studio/download
2. Cài đặt và mở Azure Data Studio
3. Tạo connection:
   - Server: `your-server-ip,1433`
   - Authentication Type: `SQL Login`
   - User name: `SA`
   - Password: `YourStrong@Passw0rd`
   - Trust server certificate: `True`

**Lưu ý:** Cần mở port 1433 trên server để kết nối từ xa:

```bash
# Trên server
sudo ufw allow 1433/tcp

# Cấu hình SQL Server cho phép kết nối từ xa
sudo /opt/mssql/bin/mssql-conf set network.tcpport 1433
sudo systemctl restart mssql-server
```

### Cách 3: Cài đặt Adminer (Web-based GUI)

```bash
# Cài đặt PHP và Adminer
sudo apt install -y php php-fpm php-pdo php-sqlsrv

# Tải Adminer
sudo mkdir -p /var/www/adminer
cd /var/www/adminer
sudo wget https://github.com/vrana/adminer/releases/download/v4.8.1/adminer-4.8.1.php -O index.php
sudo chown -R www-data:www-data /var/www/adminer
```

**Tạo Nginx config cho Adminer:**

```bash
sudo nano /etc/nginx/sites-available/adminer
```

```nginx
server {
    listen 8080;
    server_name 103.68.253.22;

    root /var/www/adminer;
    index index.php;

    location ~ \.php$ {
        include snippets/fastcgi-php.conf;
        fastcgi_pass unix:/var/run/php/php-fpm.sock;
    }
}
```

```bash
sudo ln -s /etc/nginx/sites-available/adminer /etc/nginx/sites-enabled/
sudo nginx -t
sudo systemctl reload nginx

# Truy cập: http://your-server-ip:8080
# System: MS SQL (beta)
# Server: localhost
# Username: SA
# Password: YourStrong@Passw0rd
# Database: managerCMN
```

### Cách 4: Sử dụng DBeaver (GUI - Cross-platform)

1. Tải về từ: https://dbeaver.io/download/
2. Cài đặt và mở DBeaver
3. New Connection > SQL Server
4. Nhập thông tin kết nối tương tự Azure Data Studio

---

## Các lệnh SQL hữu ích

### Quản lý Users

```sql
-- Xem tất cả users
SELECT u.UserId, u.Email, u.FullName, u.IsActive,
       STRING_AGG(r.RoleName, ', ') as Roles
FROM Users u
LEFT JOIN UserRoles ur ON u.UserId = ur.UserId
LEFT JOIN Roles r ON ur.RoleId = r.RoleId
GROUP BY u.UserId, u.Email, u.FullName, u.IsActive;
GO

-- Tạo user mới và gán role Admin


DECLARE @NewUserId INT = SCOPE_IDENTITY();
INSERT INTO UserRoles (UserId, RoleId, AssignedDate)
VALUES (@NewUserId, 1, GETDATE());  -- RoleId 1 = Admin
GO
```

### Quản lý Employees

```sql
-- Xem tất cả nhân viên
SELECT e.EmployeeId, e.EmployeeCode, e.FullName, e.Email,
       d.DepartmentName, j.JobTitleName, p.PositionName
FROM Employees e
LEFT JOIN Departments d ON e.DepartmentId = d.DepartmentId
LEFT JOIN JobTitles j ON e.JobTitleId = j.JobTitleId
LEFT JOIN Positions p ON e.PositionId = p.PositionId
ORDER BY e.EmployeeCode;
GO

-- Thêm nhân viên mới
INSERT INTO Employees (
    EmployeeCode, FullName, Email, Gender, DepartmentId,
    JobTitleId, PositionId, Status, CreatedAt
)
VALUES (
    'NV001', N'Nguyễn Văn A', 'nguyenvana@company.com', 0,
    1, 4, 1, 0, GETDATE()
);
GO
```

### Quản lý Departments

```sql
-- Xem departments
SELECT * FROM Departments ORDER BY DepartmentName;
GO

-- Thêm department
INSERT INTO Departments (DepartmentName, DepartmentCode, IsActive, CreatedAt)
VALUES (N'Phòng Kỹ Thuật', 'KT', 1, GETDATE());
GO
```



INSERT [dbo].[Employees] ([EmployeeId], [EmployeeCode], [FullName], [DateOfBirth], [Gender], [Email], [Phone], [PermanentAddress], [TemporaryAddress], [TaxCode], [BankAccount], [BankName], [DepartmentId], [Qualifications], [StartWorkingDate], [Status], [CreatedAt], [AttendanceCode], [PositionId], [AttendanceName], [Ethnicity], [IdCardIssueDate], [IdCardIssuePlace], [IdCardNumber], [JobTitleId], [Nationality], [ResignationDate], [ResignationReason], [VehiclePlate], [FacebookUrl], [InsuranceCode], [IsApprover]) VALUES (1, N'A00000', N'HR', CAST(N'1977-12-03T00:00:00.0000000' AS DateTime2), 0, N'hr@cmn.com.vn', N'903974848', N'A1411 CC HAGL 1, P. Tân Quy, Q7', N'Số 7 đường số 9, KDC Phước Kiển A, Nhà Bè', N'8049806155', N'0071001014424', N'Vietcombank CN Sài Gòn', 1, N'Đại học', CAST(N'2012-05-15T00:00:00.0000000' AS DateTime2), 0, CAST(N'2026-03-13T08:07:15.9286699' AS DateTime2), N'4', 4, N'KhoiMX', N'Kinh', CAST(N'2021-10-07T00:00:00.0000000' AS DateTime2), N'CTCCS QLHC về TTXH', N'83077014783', 1, N'Việt Nam', NULL, NULL, N'51G-340.86', NULL, N'200133112', 0)


### Seed Data ban đầu

```sql
-- Script seed data cơ bản
-- Chạy sau khi migrate database

-- Tạo admin user nếu chưa có
IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = 'hr@cmn.com.vn')
BEGIN
    INSERT INTO Users (Email, FullName, IsActive, CreatedAt)
    VALUES ('hr@cmn.com.vn', 'System Administrator', 1, GETDATE());

    DECLARE @AdminUserId INT = SCOPE_IDENTITY();
    INSERT INTO UserRoles (UserId, RoleId, AssignedDate)
    VALUES (@AdminUserId, 1, GETDATE());
END
GO
```

---

## Cấu hình hệ thống

### Google OAuth Setup

1. Truy cập [Google Cloud Console](https://console.cloud.google.com/)
2. Tạo Project mới hoặc chọn project có sẵn
3. Vào **APIs & Services** > **Credentials**
4. Tạo **OAuth 2.0 Client ID**:
   - Application type: Web application
   - Authorized redirect URIs:
     - `http://localhost:5257/signin-google` (Development)
     - `https://your-domain.com/signin-google` (Production)
5. Copy **Client ID** và **Client Secret** vào `appsettings.json`

### File Upload Configuration

Mặc định, files được upload vào `/wwwroot/uploads/`. Đảm bảo thư mục có quyền ghi:

```bash
sudo chown -R www-data:www-data /var/www/cmnmanager/wwwroot/uploads
sudo chmod -R 775 /var/www/cmnmanager/wwwroot/uploads
```

---

## Hướng dẫn sử dụng

### Đăng nhập

1. Truy cập trang Login
2. Click **"Đăng nhập với Google"**
3. Chọn tài khoản Google (email phải khớp với email trong bảng Employees)
4. Nếu email hợp lệ, hệ thống tự động tạo User và đăng nhập

### Phân quyền

| Role | Mô tả |
|------|-------|
| Admin | Toàn quyền hệ thống |
| Manager | Quản lý phòng ban, duyệt đơn |
| User | Nhân viên thường |

### Các chức năng chính

- **Dashboard**: Tổng quan hệ thống
- **Nhân sự > Nhân viên**: Quản lý nhân viên
- **Nhân sự > Hợp đồng**: Quản lý hợp đồng
- **Chấm công**: Xem và quản lý chấm công
- **Yêu cầu & Ticket**: Tạo và quản lý đơn từ, ticket
- **Tài sản**: Quản lý tài sản công ty
- **Hệ thống > Danh mục**: Cài đặt danh mục, phân quyền

---

## Troubleshooting

### Lỗi kết nối SQL Server

```bash
# Kiểm tra SQL Server đang chạy
sudo systemctl status mssql-server

# Restart SQL Server
sudo systemctl restart mssql-server

# Kiểm tra logs
sudo cat /var/opt/mssql/log/errorlog | tail -50
```

### Lỗi ứng dụng không khởi động

```bash
# Xem logs ứng dụng
sudo journalctl -u cmnmanager -n 100

# Kiểm tra file permissions
ls -la /var/www/cmnmanager/

# Chạy manual để debug
cd /var/www/cmnmanager
sudo -u www-data ASPNETCORE_ENVIRONMENT=Production dotnet managerCMN.dll
```

### Lỗi Nginx 502 Bad Gateway

```bash
# Kiểm tra ứng dụng đang chạy trên port 5000
curl http://localhost:5000

# Kiểm tra Nginx logs
sudo tail -f /var/log/nginx/error.log

# Kiểm tra nginx config
sudo nginx -t
```

### Lỗi Migration

```bash
# Rollback tất cả migrations
dotnet ef database update 0

# Xóa và tạo lại database
sqlcmd -S localhost -U SA -P 'YourStrong@Passw0rd' -C -Q "DROP DATABASE managerCMN; CREATE DATABASE managerCMN;"

# Chạy lại migration
dotnet ef database update
```

### Lỗi Permission Denied

```bash
# Fix permissions cho ứng dụng
sudo chown -R www-data:www-data /var/www/cmnmanager
sudo chmod -R 755 /var/www/cmnmanager
sudo chmod -R 775 /var/www/cmnmanager/wwwroot/uploads
```

### Kiểm tra Port đang sử dụng

```bash
# Xem các port đang listen
sudo netstat -tlnp

# Hoặc
sudo ss -tlnp
```

---

## Cập nhật ứng dụng

```bash
# Dừng service
sudo systemctl stop cmnmanager

# Backup (optional)
sudo cp -r /var/www/cmnmanager /var/www/cmnmanager.backup

# Upload files mới hoặc pull từ git
cd /var/www/cmnmanager-src
git pull
dotnet publish -c Release -o /var/www/cmnmanager

# Chạy migration nếu có
cd /var/www/cmnmanager-src/managerCMN
ASPNETCORE_ENVIRONMENT=Production dotnet ef database update

# Khởi động lại service
sudo systemctl start cmnmanager
sudo systemctl status cmnmanager
```

---

## Backup Database

```bash
# Backup database
sqlcmd -S localhost -U SA -P 'YourStrong@Passw0rd' -C -Q "BACKUP DATABASE managerCMN TO DISK = '/var/opt/mssql/backup/managerCMN_$(date +%Y%m%d).bak'"

# Tạo script backup tự động
sudo nano /etc/cron.daily/backup-sqlserver
```

```bash
#!/bin/bash
BACKUP_DIR="/var/opt/mssql/backup"
DATE=$(date +%Y%m%d)
/opt/mssql-tools18/bin/sqlcmd -S localhost -U SA -P 'YourStrong@Passw0rd' -C -Q "BACKUP DATABASE managerCMN TO DISK = '$BACKUP_DIR/managerCMN_$DATE.bak'"
# Xóa backup cũ hơn 7 ngày
find $BACKUP_DIR -name "*.bak" -mtime +7 -delete
```

```bash
sudo chmod +x /etc/cron.daily/backup-sqlserver
```

---

## Liên hệ & Hỗ trợ

- **Developer**: hyhoncute
- **Version**: 1.0.0
- **License**: Private

---

*CMN Management System - Phát triển bởi hyhoncute*