# 🎯 Quick Reference - Manager CMN Operations

**Essential commands for daily operations and emergencies**

---

## ⚡ **1-Minute Deployment**

```bash
ssh root@103.68.253.22
cd /var/www/cmnmanager-src/managerCMN
./deploy.sh
```

## 🆘 **Emergency Commands**

```bash
# Service Management
systemctl status cmnmanager       # Check status
systemctl restart cmnmanager      # Restart service
systemctl stop cmnmanager         # Stop service
systemctl start cmnmanager        # Start service

# Quick Health Check
curl -I http://localhost:5000      # Test response
journalctl -u cmnmanager -n 10     # Last 10 log entries
ps aux | grep dotnet               # Check processes

# Emergency Rollback
./rollback.sh [timestamp]          # Rollback to backup
./rollback.sh                      # Show available backups
```

## 🔍 **Troubleshooting Shortcuts**

```bash
# Service Issues
systemctl daemon-reload && systemctl restart cmnmanager

# Database Issues
cd /var/www/cmnmanager-src/managerCMN/managerCMN
dotnet ef database update

# Build Issues
rm -rf bin obj && dotnet publish -c Release -o ../bin/Release/publish

# Port Conflicts
sudo lsof -i :5000 && sudo kill -9 [PID]

# Clear Browser Cache
# Ctrl+Shift+R or use Incognito mode
```

## 📁 **Key Paths**

```
/var/www/cmnmanager-src/managerCMN/managerCMN/     # Project root
/etc/systemd/system/cmnmanager.service            # Service config
/var/log/nginx/                                   # Nginx logs
/root/backups/                                    # Backups
```

## 🧪 **Feature Testing URLs**

- **Main Site**: https://hyhon.io.vn
- **Login**: https://hyhon.io.vn/Account/Login
- **Settings**: https://hyhon.io.vn/Settings (Holiday Management)
- **Attendance**: https://hyhon.io.vn/Attendance (Calendar + Excel Export)

## 📊 **Log Monitoring**

```bash
# Live Logs
journalctl -u cmnmanager -f

# Error Logs Only
journalctl -u cmnmanager -p err -n 20

# Nginx Errors
tail -20 /var/log/nginx/error.log

# System Resources
free -h && df -h && top
```

## 💾 **Backup & Restore**

```bash
# Manual Backup
cp -r /var/www/cmnmanager-src /root/backups/manual-$(date +%Y%m%d_%H%M%S)

# List Backups
ls -la /root/backups/

# Quick Restore (if deployment fails)
systemctl stop cmnmanager
mv /var/www/cmnmanager-src /var/www/cmnmanager-src-broken
mv /var/www/cmnmanager-src-backup-[timestamp] /var/www/cmnmanager-src
systemctl start cmnmanager
```

---

**📚 For detailed information, see [README_PRODUCTION.md](./README_PRODUCTION.md)**