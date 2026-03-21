# 🚀 Production Deployment Guide - Manager CMN

## 📋 Thông tin Server
- **IP**: 103.68.253.22
- **OS**: Ubuntu 22.04.5 LTS
- **Project Path**: `/var/www/cmnmanager-src`
- **GitHub Repo**: https://github.com/HyHonCuTe/managerCMN.git

## 🔧 Yêu cầu trước khi deploy
- [ ] Backup database hiện tại
- [ ] Đảm bảo có quyền truy cập SSH root
- [ ] Kiểm tra dung lượng disk còn trống
- [ ] Thông báo downtime cho users (nếu cần)

---

## 🎯 Quy trình Deploy Production

### **Bước 1: Kết nối và Backup Database**
```bash
# Kết nối SSH
ssh root@103.68.253.22

# Tạo thư mục backup
mkdir -p /root/backups/$(date +%Y%m%d_%H%M%S)
cd /root/backups/$(date +%Y%m%d_%H%M%S)

# Backup database (điều chỉnh connection string nếu cần)
# Nếu dùng MySQL:
mysqldump -u [username] -p [database_name] > cmnmanager_backup.sql

# Nếu dùng PostgreSQL:
pg_dump -U [username] -h localhost [database_name] > cmnmanager_backup.sql

# Nếu dùng SQLite (thường ở wwwroot hoặc App_Data):
cp /var/www/cmnmanager-src/[path_to_db_file] ./cmnmanager_backup.db

# Backup thư mục upload files (nếu có)
cp -r /var/www/cmnmanager-src/wwwroot/uploads ./uploads_backup/
```

### **Bước 2: Dừng Application (để tránh conflict)**
```bash
# Tìm và dừng process đang chạy
ps aux | grep -i cmnmanager
sudo killall -9 dotnet  # Hoặc process ID cụ thể

# Nếu dùng systemd service:
sudo systemctl stop cmnmanager

# Nếu dùng nginx reverse proxy:
sudo systemctl reload nginx
```

### **Bước 3: Pull Code Mới và Backup Code Cũ**
```bash
cd /var/www

# Backup code hiện tại
cp -r cmnmanager-src cmnmanager-src-backup-$(date +%Y%m%d_%H%M%S)

# Pull code mới
cd cmnmanager-src

# Kiểm tra git status
git status
git stash  # Lưu local changes nếu có

# Pull latest code
git fetch origin
git reset --hard origin/main  # CẢNH BÁO: Sẽ mất tất cả local changes
git pull origin main

# Hoặc nếu muốn an toàn hơn:
# git checkout main
# git pull origin main
```

### **Bước 4: Build Application**
```bash
# Khôi phục packages
dotnet restore

# Build project
dotnet build --configuration Release

# Publish (tạo files optimized cho production)
dotnet publish --configuration Release --output ./bin/Release/publish
```

### **Bước 5: Update Database & Migrations**
```bash
# Xem current migrations
dotnet ef migrations list

# Drop và recreate database (CHỈ dùng nếu muốn reset hoàn toàn)
# CẢNH BÁO: SẼ MẤT TẤT CẢ DATA
dotnet ef database drop --force
dotnet ef database update

# Hoặc nếu chỉ muốn update migrations mới:
dotnet ef database update

# Kiểm tra database schema
dotnet ef migrations script --output migration_script.sql
cat migration_script.sql  # Review changes
```

### **Bước 6: Cập nhật Configuration**
```bash
# Kiểm tra appsettings.Production.json
cat appsettings.Production.json

# Đảm bảo connection string đúng:
nano appsettings.Production.json
```

**Ví dụ appsettings.Production.json:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=cmnmanager.db"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "Authentication": {
    "Google": {
      "ClientId": "your-client-id",
      "ClientSecret": "your-client-secret"
    }
  },
  "AllowedHosts": "*"
}
```

### **Bước 7: Setup hoặc Update Service**
```bash
# Tạo systemd service file (nếu chưa có)
sudo nano /etc/systemd/system/cmnmanager.service
```

**Nội dung service file:**
```ini
[Unit]
Description=CMN Manager Web Application
After=network.target

[Service]
Type=notify
WorkingDirectory=/var/www/cmnmanager-src
ExecStart=/usr/bin/dotnet /var/www/cmnmanager-src/bin/Release/publish/managerCMN.dll
ExecReload=/bin/kill -HUP $MAINPID
KillMode=mixed
Restart=always
RestartSec=5
SyslogIdentifier=cmnmanager
User=root
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false

[Install]
WantedBy=multi-user.target
```

```bash
# Reload systemd và start service
sudo systemctl daemon-reload
sudo systemctl enable cmnmanager
sudo systemctl start cmnmanager

# Kiểm tra status
sudo systemctl status cmnmanager
```

### **Bước 8: Cấu hình Nginx Reverse Proxy**
```bash
# Tạo hoặc update nginx config
sudo nano /etc/nginx/sites-available/cmnmanager
```

**Nginx configuration:**
```nginx
server {
    listen 80;
    server_name your-domain.com;  # Thay bằng domain thực tế

    location / {
        proxy_pass http://localhost:5000;  # Port của .NET app
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_cache_bypass $http_upgrade;
    }

    # Static files
    location ~* \.(css|js|png|jpg|jpeg|gif|ico|svg)$ {
        expires 1y;
        add_header Cache-Control "public, immutable";
    }
}
```

```bash
# Enable site và restart nginx
sudo ln -sf /etc/nginx/sites-available/cmnmanager /etc/nginx/sites-enabled/
sudo nginx -t  # Test config
sudo systemctl reload nginx
```

### **Bước 9: Verification & Testing**
```bash
# Kiểm tra service đang chạy
sudo systemctl status cmnmanager
sudo journalctl -u cmnmanager -f  # Xem logs realtime

# Kiểm tra port listening
sudo netstat -tlnp | grep :5000

# Test HTTP response
curl -I http://localhost:5000
curl -I http://your-domain.com

# Kiểm tra logs
tail -f /var/log/nginx/access.log
tail -f /var/log/nginx/error.log
```

### **Bước 10: Functional Testing**
- [ ] Truy cập website qua browser
- [ ] Test login functionality
- [ ] Test holiday management (Settings → Ngày nghỉ lễ)
- [ ] Test attendance calendar với holidays
- [ ] Test Excel export với new display rules
- [ ] Test các chức năng CRUD cơ bản
- [ ] Kiểm tra responsive trên mobile

---

## 🆘 Rollback Plan (nếu có vấn đề)

### **Nhanh chóng rollback:**
```bash
# Dừng service hiện tại
sudo systemctl stop cmnmanager

# Khôi phục code backup
cd /var/www
rm -rf cmnmanager-src
mv cmnmanager-src-backup-[timestamp] cmnmanager-src

# Khôi phục database
mysql -u [username] -p [database_name] < /root/backups/[timestamp]/cmnmanager_backup.sql
# Hoặc: cp /root/backups/[timestamp]/cmnmanager_backup.db [path_to_db_file]

# Start service
sudo systemctl start cmnmanager
```

---

## 🔍 Monitoring & Maintenance

### **Log Files quan trọng:**
```bash
# Application logs
sudo journalctl -u cmnmanager -f

# Nginx logs
tail -f /var/log/nginx/access.log
tail -f /var/log/nginx/error.log

# System logs
tail -f /var/log/syslog
```

### **Health Checks định kỳ:**
```bash
# Kiểm tra disk space
df -h

# Kiểm tra memory usage
free -h

# Kiểm tra CPU usage
top

# Kiểm tra service status
sudo systemctl is-active cmnmanager
```

### **Backup định kỳ (setup cron job):**
```bash
# Tạo script backup hằng ngày
sudo nano /root/scripts/daily_backup.sh

# Thêm vào crontab
sudo crontab -e
# Thêm dòng: 0 2 * * * /root/scripts/daily_backup.sh
```

---

## 📞 Support & Troubleshooting

### **Các lỗi thường gặp:**

**1. "Port already in use"**
```bash
sudo lsof -i :5000
sudo kill -9 [PID]
```

**2. "Database connection failed"**
```bash
# Kiểm tra connection string trong appsettings.Production.json
# Đảm bảo database service đang chạy
sudo systemctl status mysql  # hoặc postgresql
```

**3. "Permission denied"**
```bash
# Cấp quyền cho thư mục
sudo chown -R root:root /var/www/cmnmanager-src
sudo chmod -R 755 /var/www/cmnmanager-src
```

**4. "Migration failed"**
```bash
# Xem chi tiết lỗi
dotnet ef migrations list --verbose
dotnet ef database update --verbose
```

---

## ✅ Checklist sau khi deploy

- [ ] Website accessible qua browser
- [ ] All major features working
- [ ] No errors trong application logs
- [ ] Database migrations applied successfully
- [ ] Static files (CSS/JS) loading correctly
- [ ] SSL certificate working (nếu có HTTPS)
- [ ] Backup được tạo và verified
- [ ] Monitoring alerts configured
- [ ] Documentation updated

---

**🎉 Deployment hoàn tất! Tất cả tính năng mới đã được deploy lên production.**