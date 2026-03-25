# Hướng dẫn Deploy CMN Manager

Hướng dẫn chi tiết triển khai ứng dụng CMN Manager lên server production.

## Thông tin Server

| Thông số | Giá trị |
|----------|---------|
| Server | Ubuntu 22.04.5 LTS |
| IP | 103.68.253.22 |
| Domain | https://hyhon.io.vn |
| .NET Runtime | 9.0 |
| Database | SQL Server |
| Web Server | Nginx |
| Service | systemd (cmnmanager) |

## Cấu trúc thư mục trên Server

```
/var/www/
├── cmnmanager-src/                    # Source code
│   └── managerCMN/
│       └── managerCMN/
│           └── managerCMN/
│               ├── bin/Release/publish/   # Ứng dụng đã build
│               │   └── wwwroot/uploads/   # Thư mục upload files
│               └── *.csproj
└── cmnmanager-src-backup-*/           # Các bản backup

/root/backups/                         # Database backups
/etc/systemd/system/cmnmanager.service # Service configuration
/etc/nginx/sites-available/            # Nginx configuration
```

---

## Deploy nhanh (Dùng script)

```bash
# SSH vào server
ssh root@103.68.253.22

# Vào thư mục project
cd /var/www/cmnmanager-src/managerCMN/managerCMN

# Pull code mới và deploy
git fetch origin
git reset --hard origin/main
chmod +x deploy.sh
./deploy.sh
```

---

## Deploy thủ công (Từng bước)

### 1. SSH vào server

```bash
ssh root@103.68.253.22
```

### 2. Stop service

```bash
sudo systemctl stop cmnmanager
```

### 3. Backup (tùy chọn)

```bash
TIMESTAMP=$(date +%Y%m%d_%H%M%S)
cp -r /var/www/cmnmanager-src /var/www/cmnmanager-src-backup-$TIMESTAMP
```

### 4. Pull code mới

```bash
cd /var/www/cmnmanager-src/managerCMN/managerCMN
git fetch origin
git reset --hard origin/main
```

### 5. Build và Publish

```bash
cd managerCMN
dotnet restore
dotnet publish -c Release -o ./bin/Release/publish
```

### 6. Chạy Database Migrations

```bash
export ASPNETCORE_ENVIRONMENT=Production
dotnet ef database update
```

### 7. Set quyền thư mục uploads

```bash
UPLOADS_DIR="./bin/Release/publish/wwwroot/uploads"
mkdir -p "$UPLOADS_DIR"/{avatars,contracts,documents,tickets,temp}
chown -R www-data:www-data "$UPLOADS_DIR"
chmod -R 777 "$UPLOADS_DIR"
```

### 8. Start service

```bash
sudo systemctl daemon-reload
sudo systemctl start cmnmanager
sudo systemctl status cmnmanager
```

### 9. Kiểm tra

```bash
# Kiểm tra service
systemctl status cmnmanager

# Kiểm tra logs
journalctl -u cmnmanager -n 50 --no-pager

# Test response
curl -I http://localhost:5000
```

---

## Cấu hình Service (systemd)

File: `/etc/systemd/system/cmnmanager.service`

```ini
[Unit]
Description=CMN Manager ASP.NET Core Application
After=network.target mssql-server.service

[Service]
WorkingDirectory=/var/www/cmnmanager-src/managerCMN/managerCMN/managerCMN/bin/Release/publish
ExecStart=/usr/bin/dotnet /var/www/cmnmanager-src/managerCMN/managerCMN/managerCMN/bin/Release/publish/managerCMN.dll
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

**Lưu ý quan trọng:**
- `WorkingDirectory` và `ExecStart` phải trỏ đến thư mục `bin/Release/publish`
- `User=www-data` để có quyền ghi file uploads

---

## Cấu hình Nginx

File: `/etc/nginx/sites-available/cmnmanager`

```nginx
server {
    listen 80;
    server_name hyhon.io.vn;
    return 301 https://$server_name$request_uri;
}

server {
    listen 443 ssl http2;
    server_name hyhon.io.vn;

    ssl_certificate /etc/letsencrypt/live/hyhon.io.vn/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/hyhon.io.vn/privkey.pem;

    location / {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_cache_bypass $http_upgrade;

        # Upload limit
        client_max_body_size 50M;
    }
}
```

---

## Xử lý lỗi thường gặp

### Service không start

```bash
# Xem log chi tiết
journalctl -u cmnmanager -n 100 --no-pager

# Kiểm tra đường dẫn trong service file
cat /etc/systemd/system/cmnmanager.service

# Kiểm tra file dll tồn tại
ls -la /var/www/cmnmanager-src/managerCMN/managerCMN/managerCMN/bin/Release/publish/managerCMN.dll
```

### Lỗi "libhostpolicy.so not found"

Đường dẫn trong service file sai. Đảm bảo trỏ đến thư mục `publish`:

```bash
sudo nano /etc/systemd/system/cmnmanager.service
# Sửa WorkingDirectory và ExecStart
sudo systemctl daemon-reload
sudo systemctl restart cmnmanager
```

### Lỗi upload file (Permission denied)

```bash
UPLOADS_DIR="/var/www/cmnmanager-src/managerCMN/managerCMN/managerCMN/bin/Release/publish/wwwroot/uploads"
sudo chown -R www-data:www-data "$UPLOADS_DIR"
sudo chmod -R 777 "$UPLOADS_DIR"
```

### Database migration failed

```bash
cd /var/www/cmnmanager-src/managerCMN/managerCMN/managerCMN
export ASPNETCORE_ENVIRONMENT=Production
dotnet ef database update --verbose
```

### Nginx 502 Bad Gateway

```bash
# Kiểm tra service đang chạy
systemctl status cmnmanager

# Kiểm tra port 5000
netstat -tlnp | grep 5000

# Restart tất cả
sudo systemctl restart cmnmanager
sudo systemctl restart nginx
```

---

## Lệnh quản lý

### Service

```bash
# Status
systemctl status cmnmanager

# Start/Stop/Restart
systemctl start cmnmanager
systemctl stop cmnmanager
systemctl restart cmnmanager

# Enable auto-start
systemctl enable cmnmanager
```

### Logs

```bash
# Logs realtime
journalctl -u cmnmanager -f

# 100 dòng gần nhất
journalctl -u cmnmanager -n 100 --no-pager

# Logs hôm nay
journalctl -u cmnmanager --since today
```

### Nginx

```bash
# Test config
nginx -t

# Reload
systemctl reload nginx

# Restart
systemctl restart nginx
```

### Database

```bash
cd /var/www/cmnmanager-src/managerCMN/managerCMN/managerCMN

# Xem migrations
dotnet ef migrations list

# Update database
export ASPNETCORE_ENVIRONMENT=Production
dotnet ef database update

# Tạo migration mới (trên máy local)
dotnet ef migrations add intial_migration
```

---

## Rollback

Nếu deploy có vấn đề, rollback về bản backup:

```bash
# Stop service
sudo systemctl stop cmnmanager

# Xem các bản backup
ls -la /var/www/cmnmanager-src-backup-*

# Rollback (thay TIMESTAMP bằng timestamp backup)
sudo rm -rf /var/www/cmnmanager-src
sudo cp -r /var/www/cmnmanager-src-backup-TIMESTAMP /var/www/cmnmanager-src

# Start service
sudo systemctl start cmnmanager
```

---

## Backup Database

### Manual backup

```bash
# Backup SQL Server database
sqlcmd -S localhost -U sa -P 'YourPassword' -Q "BACKUP DATABASE cmnmanager TO DISK='/root/backups/cmnmanager_$(date +%Y%m%d).bak'"
```

### Setup cron job (tự động backup hàng ngày)

```bash
# Mở crontab
crontab -e

# Thêm dòng (backup lúc 2h sáng mỗi ngày)
0 2 * * * /root/scripts/backup.sh
```

---

## Checklist sau deploy

- [ ] Service đang chạy: `systemctl status cmnmanager`
- [ ] Website truy cập được: https://hyhon.io.vn
- [ ] Đăng nhập Google OAuth hoạt động
- [ ] Upload file hoạt động (thử tạo hợp đồng với file đính kèm)
- [ ] Xóa nhân viên hoạt động
- [ ] Điều chỉnh phép thủ công hoạt động
- [ ] Không có lỗi trong logs
