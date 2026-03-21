# 🚀 Production Deployment Scripts

Bộ scripts tự động để deploy và quản lý CMN Manager trên production server.

## 📁 Files Overview

| File | Mô tả | Cách sử dụng |
|------|-------|--------------|
| `DEPLOYMENT_GUIDE.md` | Hướng dẫn deploy chi tiết manual | Đọc trước khi deploy |
| `deploy.sh` | Script deploy tự động | `./deploy.sh` |
| `rollback.sh` | Script rollback nhanh | `./rollback.sh [timestamp]` |
| `daily_backup.sh` | Script backup hàng ngày | Setup cron job |

## 🎯 Quick Start

### **Lần đầu setup trên server:**

1. **Upload scripts lên server:**
```bash
# Trên local machine
scp *.sh root@103.68.253.22:/root/scripts/
scp DEPLOYMENT_GUIDE.md root@103.68.253.22:/root/

# Trên server
ssh root@103.68.253.22
chmod +x /root/scripts/*.sh
```

2. **Chạy deployment:**
```bash
cd /root/scripts
./deploy.sh
```

### **Deploy code mới:**
```bash
ssh root@103.68.253.22
cd /root/scripts
./deploy.sh
```

### **Rollback nếu có vấn đề:**
```bash
# Xem backups available
./rollback.sh

# Rollback về timestamp cụ thể
./rollback.sh 20240321_143022
```

### **Setup backup tự động:**
```bash
# Copy script vào thư mục system
cp /root/scripts/daily_backup.sh /root/scripts/
chmod +x /root/scripts/daily_backup.sh

# Setup cron job (chạy lúc 2h sáng hàng ngày)
crontab -e
# Thêm dòng:
0 2 * * * /root/scripts/daily_backup.sh >> /var/log/cmnmanager_backup.log 2>&1
```

## 📋 Pre-deployment Checklist

- [ ] **Code**: Đã commit và push code lên GitHub
- [ ] **Database**: Migrations đã test thành công local
- [ ] **Features**: Tất cả tính năng mới đã test kỹ
- [ ] **Backup**: Server có đủ disk space cho backup
- [ ] **Notification**: Thông báo users về downtime (nếu cần)

## 🔄 Normal Workflow

1. **Develop & Test** trên local
2. **Commit & Push** lên GitHub
3. **Deploy** bằng `deploy.sh`
4. **Verify** functionality trên production
5. **Monitor** logs để đảm bảo stable

## 🆘 Emergency Procedures

### **Nếu deploy lỗi:**
```bash
# Rollback ngay lập tức
./rollback.sh [last_good_timestamp]

# Hoặc manual rollback
systemctl stop cmnmanager
mv /var/www/cmnmanager-src /var/www/cmnmanager-src-broken
mv /var/www/cmnmanager-src-backup-[timestamp] /var/www/cmnmanager-src
systemctl start cmnmanager
```

### **Nếu database corrupt:**
```bash
# Khôi phục từ backup
cp /root/backups/[date]/cmnmanager_*.db /var/www/cmnmanager-src/
systemctl restart cmnmanager
```

### **Nếu service không start:**
```bash
# Check logs
journalctl -u cmnmanager -n 50

# Check port conflicts
netstat -tlnp | grep :5000
lsof -i :5000

# Reset service
systemctl daemon-reload
systemctl restart cmnmanager
```

## 📊 Monitoring Commands

```bash
# Service status
systemctl status cmnmanager

# Live logs
journalctl -u cmnmanager -f

# Disk space
df -h

# Memory usage
free -h

# Application response test
curl -I http://localhost:5000

# Database size
du -h /var/www/cmnmanager-src/*.db
```

## 🔧 Configuration Files

### **Service file:** `/etc/systemd/system/cmnmanager.service`
### **Nginx config:** `/etc/nginx/sites-available/cmnmanager`
### **App config:** `/var/www/cmnmanager-src/appsettings.Production.json`

## 📞 Support

- **Logs location:** `/var/log/nginx/` và `journalctl -u cmnmanager`
- **Backup location:** `/root/backups/`
- **Emergency contact:** Xem DEPLOYMENT_GUIDE.md

---

**⚡ Với các scripts này, việc deploy và maintain production server trở nên đơn giản và an toàn!**