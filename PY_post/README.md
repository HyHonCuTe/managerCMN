# Attendance Sync System

Automated synchronization system between ZK Biometric Device and managerCMN attendance management system.

## Overview

This system automatically fetches attendance records from a ZK biometric device and synchronizes them with the managerCMN web application every 5 minutes. It features incremental sync (only new records), authentication security, error handling, and Vietnam timezone support.

## Features

- ✅ **Automated Sync**: Runs every 5 minutes via cron job
- ✅ **Incremental Updates**: Only syncs new records since last successful sync
- ✅ **Night Hours Skip**: Automatically skips sync during 8 PM - 7 AM Vietnam time
- ✅ **Security**: API key authentication prevents unauthorized access
- ✅ **Error Recovery**: Retry logic with exponential backoff
- ✅ **Comprehensive Logging**: Detailed logs for monitoring and troubleshooting
- ✅ **Vietnam Timezone**: Proper timezone handling for local time requirements

## Prerequisites

### System Requirements
- **OS**: Ubuntu 18.04+ or similar Linux distribution
- **Python**: 3.8 or higher
- **Network**: Access to ZK device (default: 192.168.100.13:4370)
- **Internet**: HTTPS access to managerCMN server (https://hyhon.io.vn)

### Dependencies
- `pyzk==0.9.0` - ZK biometric device communication
- `requests==2.31.0` - HTTP client for API calls
- `pytz==2023.3` - Timezone handling

## Quick Installation

1. **Download files** to your intermediate machine:
   ```bash
   # Copy these files to your server:
   # - post.py (main sync script)
   # - config.json (configuration)
   # - requirements.txt (Python dependencies)
   # - install.sh (installation script)
   ```

2. **Configure settings**:
   ```bash
   nano config.json
   # Update device IP, API endpoint, and API key as needed
   ```

3. **Run installation**:
   ```bash
   chmod +x install.sh
   ./install.sh
   ```

4. **Verify setup**:
   ```bash
   # Check logs
   tail -f /opt/attendance-sync/logs/attendance_sync.log

   # Manual test
   cd /opt/attendance-sync && python3 post.py
   ```

## Configuration

### config.json Structure

```json
{
  "device": {
    "ip": "192.168.100.13",        // ZK device IP address
    "port": 4370,                  // ZK device port (usually 4370)
    "timeout": 10                  // Connection timeout in seconds
  },
  "api": {
    "endpoint": "https://hyhon.io.vn/api/attendance/punch",  // Server API URL
    "key": "att_sync_key_2026_secure_hyhon",                // API authentication key
    "timeout": 30                  // HTTP request timeout in seconds
  },
  "sync": {
    "interval_minutes": 5,         // Cron interval (for reference only)
    "skip_night_hours": true,      // Skip sync during 8 PM - 7 AM Vietnam time
    "max_retries": 3,              // Number of retry attempts on failure
    "retry_delay_seconds": 30      // Delay between retry attempts
  }
}
```

### Important Settings

**Device Configuration:**
- `ip`: Must match your ZK biometric device IP address
- `port`: Usually 4370 for ZK devices (UDP)
- `timeout`: Increase if device connection is slow

**API Configuration:**
- `endpoint`: Full URL to the attendance API endpoint
- `key`: Must match the API key configured in managerCMN appsettings.json
- `timeout`: Increase for slow network connections

**Sync Configuration:**
- `skip_night_hours`: Set to `false` if you need 24/7 sync
- `max_retries`: Increase for unreliable networks
- `retry_delay_seconds`: Adjust based on typical recovery time

## Installation Details

### Automatic Installation (Recommended)

The `install.sh` script handles everything automatically:

```bash
chmod +x install.sh
./install.sh
```

**What it does:**
1. Checks Python 3 and pip prerequisites
2. Validates configuration file
3. Creates `/opt/attendance-sync/` directory
4. Installs Python dependencies
5. Sets up proper file permissions
6. Tests device connection
7. Configures cron job (every 5 minutes)
8. Runs initial sync test

### Manual Installation

If you prefer manual setup:

```bash
# 1. Create directory
sudo mkdir -p /opt/attendance-sync/logs

# 2. Copy files
sudo cp post.py config.json requirements.txt /opt/attendance-sync/

# 3. Install dependencies
cd /opt/attendance-sync
sudo pip3 install -r requirements.txt

# 4. Set permissions
sudo chown -R $USER:$USER /opt/attendance-sync
chmod 600 /opt/attendance-sync/config.json
chmod +x /opt/attendance-sync/post.py

# 5. Setup cron job
crontab -e
# Add: */5 * * * * cd /opt/attendance-sync && python3 post.py >> logs/cron.log 2>&1
```

## Monitoring & Maintenance

### Log Files

**Main Application Log:**
```bash
tail -f /opt/attendance-sync/logs/attendance_sync.log
```

**Cron Execution Log:**
```bash
tail -f /opt/attendance-sync/logs/cron.log
```

**Log Rotation:**
Logs rotate automatically when they become too large.

### Status Commands

**Check Cron Service:**
```bash
sudo systemctl status cron
```

**View Cron Jobs:**
```bash
crontab -l
```

**Manual Sync Test:**
```bash
cd /opt/attendance-sync
python3 post.py
```

**Check Last Sync Time:**
```bash
cat /opt/attendance-sync/last_sync.json
```

## Troubleshooting

### Common Issues

#### 1. Device Connection Failed
**Error:** `Failed to connect to ZK device`

**Solutions:**
- Check device IP: `ping 192.168.100.13`
- Verify device is powered on
- Check firewall rules: `sudo ufw status`
- Test device port: `nmap -p 4370 192.168.100.13`

**Debug:**
```bash
cd /opt/attendance-sync
python3 -c "
from zk import ZK
zk = ZK('192.168.100.13', port=4370, force_udp=True, timeout=10)
conn = zk.connect()
print('Connection successful')
conn.disconnect()
"
```

#### 2. API Authentication Error
**Error:** `Invalid API Key` or `401 Unauthorized`

**Solutions:**
- Verify API key in config.json matches server configuration
- Check appsettings.json on server has correct `ApiSettings:AttendanceApiKey`
- Ensure no extra spaces or characters in API key

**Test:**
```bash
curl -X POST https://hyhon.io.vn/api/attendance/punch \
  -H "Content-Type: application/json" \
  -H "X-API-Key: att_sync_key_2026_secure_hyhon" \
  -d '[{"UserId":"123","Time":"2026-03-24T10:30:00"}]'
```

#### 3. Network Connection Issues
**Error:** `Connection error` or `Request timeout`

**Solutions:**
- Check internet connectivity: `ping hyhon.io.vn`
- Verify HTTPS access: `curl -I https://hyhon.io.vn`
- Check DNS resolution: `nslookup hyhon.io.vn`
- Increase timeout in config.json

#### 4. Cron Not Running
**Error:** No automatic sync happening

**Solutions:**
- Check cron service: `sudo systemctl status cron`
- Start cron if stopped: `sudo systemctl start cron`
- Verify crontab entry: `crontab -l`
- Check cron logs: `grep CRON /var/log/syslog`

#### 5. Permission Errors
**Error:** `Permission denied` accessing files

**Solutions:**
```bash
# Fix ownership
sudo chown -R $USER:$USER /opt/attendance-sync

# Fix permissions
chmod 600 /opt/attendance-sync/config.json
chmod +x /opt/attendance-sync/post.py
chmod 755 /opt/attendance-sync/logs
```

### Advanced Debugging

**Enable Verbose Logging:**
Modify config.json:
```json
{
  "logging": {
    "level": "DEBUG"
  }
}
```

**Test Individual Components:**

1. **Configuration Loading:**
   ```bash
   cd /opt/attendance-sync
   python3 -c "
   from post import AttendanceSync
   sync = AttendanceSync()
   print('Config loaded successfully:', sync.config)
   "
   ```

2. **Device Connection:**
   ```bash
   python3 -c "
   from post import AttendanceSync
   sync = AttendanceSync()
   conn = sync.connect_to_device()
   if conn:
       print('Device connection successful')
       conn.disconnect()
   "
   ```

3. **API Connection:**
   ```bash
   python3 -c "
   from post import AttendanceSync
   sync = AttendanceSync()
   result = sync.send_to_server([{'UserId': 'test', 'Time': '2026-03-24T10:30:00'}])
   print('API test result:', result)
   "
   ```

## Security Considerations

### API Key Management
- **Storage**: API key is stored in config.json with 600 permissions (owner read/write only)
- **Transmission**: Always sent via HTTPS with X-API-Key header
- **Rotation**: Change API key by updating both config.json and server appsettings.json

### Network Security
- **HTTPS**: All API communication uses HTTPS
- **Firewall**: Consider restricting outbound connections to specific endpoints
- **VPN**: Use VPN if intermediate machine is remote

### File Permissions
```bash
# Secure configuration
chmod 600 /opt/attendance-sync/config.json    # API key protection
chmod 755 /opt/attendance-sync/               # Directory access
chmod +x /opt/attendance-sync/post.py         # Execution permission
chmod 755 /opt/attendance-sync/logs/          # Log directory
```

## Maintenance

### Regular Tasks

**Weekly:**
- Review logs for errors or unusual patterns
- Check disk space in `/opt/attendance-sync/logs/`
- Verify sync is running on schedule

**Monthly:**
- Update Python packages if needed: `pip3 install --upgrade -r requirements.txt`
- Archive old log files if they consume too much space
- Test manual sync to ensure system health

**As Needed:**
- Update API key if rotated on server
- Update device IP if network changes
- Adjust sync schedule if requirements change

### Log Management

Logs automatically rotate, but you can manually clean old logs:

```bash
# Remove logs older than 30 days
find /opt/attendance-sync/logs/ -name "*.log" -mtime +30 -delete

# Archive logs (optional)
tar -czf attendance-logs-$(date +%Y%m%d).tar.gz /opt/attendance-sync/logs/*.log
```

## System Architecture

```
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│   ZK Device     │    │  Intermediate    │    │  managerCMN     │
│ 192.168.100.13  │◄──►│    Machine       │◄──►│    Server       │
│    Port 4370    │    │                  │    │  hyhon.io.vn    │
│                 │    │  post.py         │    │                 │
│  Attendance     │    │  config.json     │    │  API Endpoint   │
│  Records        │    │  Cron Job        │    │  /api/attendance│
│                 │    │  (Every 5 min)   │    │  /punch         │
└─────────────────┘    └──────────────────┘    └─────────────────┘
```

**Data Flow:**
1. **ZK Device** stores punch records with UserId and timestamp
2. **post.py** connects via pyzk library (UDP port 4370)
3. **Incremental Sync** fetches only records newer than last sync
4. **API Request** sends JSON array via HTTPS with API key authentication
5. **Server Processing** validates, maps employees, calculates attendance
6. **Database** stores attendance records with late detection and working hours

## API Reference

### Endpoint
```
POST https://hyhon.io.vn/api/attendance/punch
```

### Headers
```
Content-Type: application/json
X-API-Key: att_sync_key_2026_secure_hyhon
```

### Request Body
```json
[
  {
    "UserId": "employee_attendance_code",
    "Time": "2026-03-24T10:30:00+07:00"
  }
]
```

### Response
**Success (200):**
```json
{
  "message": "Import thành công.",
  "count": 1
}
```

**Error (400/401):**
```json
{
  "error": "Error message in Vietnamese"
}
```

## Support

### Getting Help

1. **Check Logs**: Most issues are logged with detailed error messages
2. **Review Configuration**: Verify all settings in config.json
3. **Test Components**: Use manual testing commands to isolate issues
4. **Network Debugging**: Use ping, curl, nmap to verify connectivity

### Reporting Issues

When reporting issues, include:
- Error messages from logs
- Configuration (remove sensitive API key)
- Network topology (device IP, server URL)
- Steps to reproduce the problem

---

**Version**: 1.0
**Last Updated**: March 2026
**Compatibility**: managerCMN API v1, ZK devices with pyzk support