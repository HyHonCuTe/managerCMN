# 🚀 QUICKSTART - Deploy lên Server Production

## ✅ Code đã sẵn sàng trên GitHub
- Repo: https://github.com/HyHonCuTe/managerCMN.git
- Tất cả tính năng mới đã được commit và push
- Deployment scripts và docs đã ready

## 🎯 Commands để chạy trên server

### **Bước 1: Kết nối server**
```bash
ssh root@103.68.253.22
```

### **Bước 2: Download deployment scripts**
```bash
cd /var/www/cmnmanager-src
git pull origin main

# Copy scripts to system directory
mkdir -p /root/scripts
cp *.sh /root/scripts/
chmod +x /root/scripts/*.sh
```

### **Bước 3: Deploy tự động (RECOMMENDED)**
```bash
cd /root/scripts
./deploy.sh
```

**Script sẽ tự động:**
- ✅ Backup database và code hiện tại
- ✅ Pull code mới từ GitHub
- ✅ Build và publish application
- ✅ Update database migrations
- ✅ Restart service và verify

### **Bước 4: Verify deployment**
```bash
# Check service status
systemctl status cmnmanager

# Test website response
curl -I http://localhost:5000

# View logs
journalctl -u cmnmanager -f
```

---

## 🔄 Nếu có vấn đề - Rollback nhanh
```bash
# Xem available backups
cd /root/scripts
./rollback.sh

# Rollback về phiên bản trước
./rollback.sh [timestamp_from_backup]
```

---

## ⚡ Manual Steps (nếu muốn làm thủ công)

Trong trường hợp muốn deploy manual thay vì dùng script tự động, đọc file **DEPLOYMENT_GUIDE.md** để có hướng dẫn step-by-step chi tiết.

---

## 🎉 Tính năng mới sau khi deploy

1. **Holiday Management**: Settings → Ngày nghỉ lễ
2. **Calendar**: Attendance hiển thị "Nghỉ lễ" cho ngày đã add
3. **Excel Export**: Format mới với P/K/L values và màu sắc

All done! 🚀