# 🚀 Manager CMN - Production Deployment & Operations Guide

**Comprehensive guide for deploying and maintaining the Manager CMN application in production environment.**

---

## 📋 **Project Overview**

- **Application**: Employee Management System (CMN Manager)
- **Technology**: ASP.NET Core 9.0, Entity Framework Core, SQL Server
- **Server**: Ubuntu 22.04.5 LTS @ 103.68.253.22
- **Domain**: https://hyhon.io.vn
- **Database**: SQL Server (localhost)
- **Service**: systemd service `cmnmanager`

### **Latest Features (March 2026)**
- ✅ **Holiday Management**: Admin can manage company holidays
- ✅ **Enhanced Attendance Calendar**: Displays holidays with "Nghỉ lễ"
- ✅ **New Excel Export Format**: P/K/L display rules with color coding

---

## 🎯 **Quick Deployment Commands**

### **Standard Deployment (Recommended)**
```bash
# Connect to production server
ssh root@103.68.253.22

# Navigate to project directory
cd /var/www/cmnmanager-src

# Pull latest code
git pull origin main

# Run automated deployment script
cd managerCMN
chmod +x deploy.sh
./deploy.sh
```

### **Emergency Rollback**
```bash
# If deployment fails, rollback immediately
cd /var/www/cmnmanager-src/managerCMN
chmod +x rollback.sh
./rollback.sh [backup_timestamp]
```

---

## 🛠️ **Manual Deployment Steps**

If automated scripts fail, follow these manual steps:

### **1. Preparation**
```bash
# Connect to server
ssh root@103.68.253.22

# Backup current state
cp -r /var/www/cmnmanager-src /var/www/cmnmanager-src-backup-$(date +%Y%m%d_%H%M%S)

# Stop service
systemctl stop cmnmanager
```

### **2. Code Update**
```bash
cd /var/www/cmnmanager-src

# Pull latest code
git fetch origin
git reset --hard origin/main
git pull origin main
```

### **3. Build & Publish**
```bash
cd /var/www/cmnmanager-src/managerCMN/managerCMN

# Clean build
rm -rf bin obj
dotnet clean
dotnet restore
dotnet build --configuration Release

# Publish application
dotnet publish --configuration Release --output ../bin/Release/publish
```

### **4. Database Migration**
```bash
# Apply database migrations
export ASPNETCORE_ENVIRONMENT=Production
dotnet ef database update --verbose
```

### **5. Service Management**
```bash
# Start service
systemctl daemon-reload
systemctl start cmnmanager
systemctl status cmnmanager

# Verify application is running
curl -I http://localhost:5000
```

---

## 🧪 **Testing & Verification**

### **1. System Health Checks**
```bash
# Service status
systemctl status cmnmanager

# Application logs
journalctl -u cmnmanager -n 50

# Port check
netstat -tlnp | grep :5000

# Database connectivity
dotnet ef migrations list
```

### **2. Feature Testing**

#### **Holiday Management**
1. Login to https://hyhon.io.vn
2. Navigate to **Settings** → **Ngày nghỉ lễ**
3. Test: Add/Edit/Delete holidays
4. Verify: No errors, data persists

#### **Attendance Calendar**
1. Go to **Attendance** module
2. View calendar for current/test month
3. Verify: Holidays display "Nghỉ lễ" with cyan-green background
4. Check: Holiday names appear correctly

#### **Excel Export**
1. Export attendance Excel file
2. Open file and verify format:
   - **"1"** = Full attendance (Green)
   - **"P"/"P/2"** = Paid leave (Yellow)
   - **"K"/"K/2"** = Unpaid leave (Pink)
   - **"L"** = Holiday (Cyan)
3. Check: Summary columns calculate correctly

### **3. Browser Cache Issues**
If you see old interface after deployment:
- **Hard refresh**: Ctrl+Shift+R (Windows) or Cmd+Shift+R (Mac)
- **Incognito mode**: Test in private browsing
- **Clear cache**: Delete browser cache and cookies

---

## 📁 **Important Paths & Files**

### **Application Paths**
```
/var/www/cmnmanager-src/                    # Git repository root
├── managerCMN/                             # Solution directory
│   ├── managerCMN/                         # Main project
│   │   ├── appsettings.Production.json     # Production config
│   │   └── managerCMN.csproj               # Project file
│   └── bin/Release/publish/                # Published application
│       └── managerCMN.dll                  # Main executable
```

### **System Configuration**
```
/etc/systemd/system/cmnmanager.service      # Service definition
/etc/nginx/sites-enabled/cmnmanager         # Nginx configuration
/root/backups/                              # Automatic backups
/root/scripts/                              # Deployment scripts
```

### **Key Configuration Files**

#### **appsettings.Production.json**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=managerCMN;User Id=SA;Password=CMN@2026;TrustServerCertificate=True;"
  },
  "Authentication": {
    "Google": {
      "ClientId": "750588360271-tdokmk5t659s2cbces01fvngjkli0rpl.apps.googleusercontent.com",
      "ClientSecret": "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"
    }
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

#### **systemd service: /etc/systemd/system/cmnmanager.service**
```ini
[Unit]
Description=CMN Manager ASP.NET Core Application
After=network.target

[Service]
Type=notify
WorkingDirectory=/var/www/cmnmanager-src/managerCMN
ExecStart=/usr/bin/dotnet /var/www/cmnmanager-src/managerCMN/bin/Release/publish/managerCMN.dll
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

---

## 🔍 **Troubleshooting**

### **Common Issues & Solutions**

#### **Service Won't Start**
```bash
# Check logs for specific error
journalctl -u cmnmanager -n 20

# Common fixes:
systemctl daemon-reload
systemctl restart cmnmanager

# If port conflict:
sudo lsof -i :5000
sudo kill -9 [PID]
```

#### **Database Connection Issues**
```bash
# Test connection string
cd /var/www/cmnmanager-src/managerCMN/managerCMN
dotnet ef database update

# Check SQL Server service
systemctl status mssql-server

# Verify connection string in appsettings.Production.json
cat appsettings.Production.json | grep -A 3 "ConnectionStrings"
```

#### **Build/Publish Failures**
```bash
# Clean everything and rebuild
cd /var/www/cmnmanager-src/managerCMN/managerCMN
rm -rf bin obj ../bin
dotnet clean
dotnet restore
dotnet build --configuration Release
dotnet publish --configuration Release --output ../bin/Release/publish
```

#### **Website Shows Old Code**
```bash
# Force rebuild with new timestamp
systemctl stop cmnmanager
rm -rf /var/www/cmnmanager-src/managerCMN/bin/Release/publish
cd /var/www/cmnmanager-src/managerCMN/managerCMN
dotnet publish --configuration Release --output ../bin/Release/publish
systemctl start cmnmanager

# Check file timestamps
ls -la /var/www/cmnmanager-src/managerCMN/bin/Release/publish/managerCMN.dll
```

#### **SSL/Certificate Issues**
```bash
# Check nginx configuration
nginx -t
systemctl reload nginx

# Check SSL certificates
certbot certificates
certbot renew --dry-run
```

---

## 🔧 **Maintenance**

### **Daily Operations**
```bash
# Check service status
systemctl status cmnmanager

# View recent logs
journalctl -u cmnmanager --since "1 hour ago"

# Check disk space
df -h

# Monitor memory usage
free -h
```

### **Weekly Tasks**
```bash
# Update system packages (with caution)
apt update && apt list --upgradable

# Check nginx logs for errors
tail -100 /var/log/nginx/error.log

# Verify backups are working
ls -la /root/backups/ | tail -10
```

### **Backup Management**
```bash
# Manual backup
cp -r /var/www/cmnmanager-src /root/backups/manual-$(date +%Y%m%d_%H%M%S)

# Automated daily backup (already configured)
# Check: crontab -l

# Cleanup old backups (keep last 30 days)
find /root/backups/ -type d -mtime +30 -exec rm -rf {} \;
```

---

## 📊 **Monitoring & Logs**

### **Application Logs**
```bash
# Real-time logs
journalctl -u cmnmanager -f

# Last 50 entries
journalctl -u cmnmanager -n 50

# Logs since specific time
journalctl -u cmnmanager --since "2026-03-21 00:00:00"

# Error logs only
journalctl -u cmnmanager -p err
```

### **Nginx Logs**
```bash
# Access logs
tail -f /var/log/nginx/access.log

# Error logs
tail -f /var/log/nginx/error.log

# Search for specific errors
grep "error" /var/log/nginx/error.log | tail -20
```

### **Performance Monitoring**
```bash
# CPU and memory usage
top
htop

# Detailed process info
ps aux | grep dotnet

# Network connections
netstat -tlnp | grep cmnmanager
```

---

## 🚀 **Development & Updates**

### **Making Code Changes**
1. **Local Development**
   - Make changes on local machine
   - Test thoroughly
   - Commit to GitHub

2. **Deployment Process**
   ```bash
   # On production server
   cd /var/www/cmnmanager-src/managerCMN
   ./deploy.sh
   ```

3. **Verification**
   - Test all features
   - Check logs for errors
   - Monitor performance

### **Database Schema Changes**
```bash
# Add new migration (on development)
dotnet ef migrations add MigrationName

# Apply on production
cd /var/www/cmnmanager-src/managerCMN/managerCMN
export ASPNETCORE_ENVIRONMENT=Production
dotnet ef database update
```

---

## 📞 **Emergency Contacts & Resources**

### **Quick Commands Reference**
| Task | Command |
|------|---------|
| Deploy | `./deploy.sh` |
| Rollback | `./rollback.sh [timestamp]` |
| Service Status | `systemctl status cmnmanager` |
| View Logs | `journalctl -u cmnmanager -f` |
| Restart Service | `systemctl restart cmnmanager` |
| Check Website | `curl -I http://localhost:5000` |

### **Emergency Procedures**
1. **Total System Failure**: Use rollback script with latest backup
2. **Database Issues**: Check SQL Server service and connection strings
3. **Performance Issues**: Check logs, restart service, monitor resources
4. **Security Issues**: Check nginx logs, update certificates if needed

---

## ✅ **Post-Deployment Checklist**

After every deployment, verify:

- [ ] **Service Status**: `systemctl status cmnmanager` shows active
- [ ] **Website Accessible**: https://hyhon.io.vn loads correctly
- [ ] **Login Works**: User authentication functions properly
- [ ] **Holiday Management**: Settings → Ngày nghỉ lễ accessible
- [ ] **Attendance Calendar**: Holidays display correctly
- [ ] **Excel Export**: Format shows P/K/L values correctly
- [ ] **Database Connected**: No connection errors in logs
- [ ] **SSL Certificate**: HTTPS working without warnings
- [ ] **Performance**: Response times normal (<2 seconds)

---

**🎉 Everything is documented! Follow this guide for smooth operations!**

---

*Last updated: March 21, 2026*
*Version: 2.1 (Holiday Management + Enhanced Excel Export)*