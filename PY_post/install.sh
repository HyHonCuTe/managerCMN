#!/bin/bash
set -e

# Attendance Sync System Installation Script
# This script sets up the automated ZK device to server synchronization

INSTALL_DIR="/opt/attendance-sync"
SERVICE_USER="attendance-sync"
PYTHON_CMD="python3"

echo "=========================================="
echo "Attendance Sync System Installer"
echo "=========================================="

# Check if running as root
if [[ $EUID -eq 0 ]]; then
   echo "This script should not be run as root for security reasons."
   echo "Please run as a regular user with sudo privileges."
   exit 1
fi

# Check prerequisites
echo "Checking prerequisites..."

# Check Python 3
if ! command -v python3 &> /dev/null; then
    echo "Error: Python 3 is required but not installed."
    echo "Install with: sudo apt update && sudo apt install python3 python3-pip"
    exit 1
fi

# Check pip
if ! command -v pip3 &> /dev/null; then
    echo "Installing pip3..."
    sudo apt update
    sudo apt install -y python3-pip
fi

# Check if config.json exists
if [[ ! -f "config.json" ]]; then
    echo "Error: config.json not found in current directory."
    echo "Please ensure config.json is present with proper device and API settings."
    exit 1
fi

# Validate config.json
echo "Validating configuration..."
python3 -c "
import json
try:
    with open('config.json') as f:
        config = json.load(f)
    required_keys = ['device', 'api', 'sync']
    for key in required_keys:
        if key not in config:
            raise ValueError(f'Missing required key: {key}')
    if 'ip' not in config['device']:
        raise ValueError('Missing device.ip in config')
    if 'endpoint' not in config['api'] or 'key' not in config['api']:
        raise ValueError('Missing api.endpoint or api.key in config')
    print('Configuration validation passed')
except Exception as e:
    print(f'Configuration error: {e}')
    exit(1)
"

echo "Creating installation directory..."
sudo mkdir -p "$INSTALL_DIR"
sudo mkdir -p "$INSTALL_DIR/logs"

echo "Copying files..."
sudo cp post.py "$INSTALL_DIR/"
sudo cp config.json "$INSTALL_DIR/"
sudo cp requirements.txt "$INSTALL_DIR/"

# Make post.py executable
sudo chmod +x "$INSTALL_DIR/post.py"

echo "Installing Python dependencies..."
cd "$INSTALL_DIR"
sudo pip3 install -r requirements.txt

echo "Setting up permissions..."
# Get current user for file ownership
CURRENT_USER=$(whoami)
sudo chown -R "$CURRENT_USER:$CURRENT_USER" "$INSTALL_DIR"

# Set secure permissions on config file (contains API key)
chmod 600 "$INSTALL_DIR/config.json"

echo "Testing device connection..."
cd "$INSTALL_DIR"
timeout 30 python3 -c "
from zk import ZK
import json

with open('config.json') as f:
    config = json.load(f)

try:
    zk = ZK(config['device']['ip'], port=config['device'].get('port', 4370), force_udp=True, timeout=10)
    conn = zk.connect()
    print('✓ Successfully connected to ZK device')
    conn.disconnect()
except Exception as e:
    print(f'✗ Device connection failed: {e}')
    print('Warning: Device connection test failed. Please check:')
    print('1. Device IP address in config.json')
    print('2. Network connectivity to device')
    print('3. Device is powered on and accessible')
" || echo "Warning: Device connection test timed out or failed"

echo "Configuring cron job..."
# Remove any existing cron job
crontab -l 2>/dev/null | grep -v "attendance_sync" | crontab - || true

# Add new cron job (every 5 minutes)
(crontab -l 2>/dev/null; echo "*/5 * * * * cd $INSTALL_DIR && python3 post.py >> logs/cron.log 2>&1") | crontab -

echo "Testing manual sync..."
cd "$INSTALL_DIR"
echo "Running test sync (this may take a moment)..."
python3 post.py && echo "✓ Manual sync test completed" || echo "✗ Manual sync test failed"

echo ""
echo "=========================================="
echo "Installation completed successfully!"
echo "=========================================="
echo ""
echo "Installation Summary:"
echo "- Sync script installed to: $INSTALL_DIR"
echo "- Cron job configured: every 5 minutes"
echo "- Night hours (8 PM - 7 AM Vietnam time): skipped"
echo "- Logs location: $INSTALL_DIR/logs/"
echo ""
echo "Monitoring Commands:"
echo "- View sync logs: tail -f $INSTALL_DIR/logs/attendance_sync.log"
echo "- View cron logs: tail -f $INSTALL_DIR/logs/cron.log"
echo "- Manual sync test: cd $INSTALL_DIR && python3 post.py"
echo "- Check cron status: systemctl status cron"
echo "- View cron jobs: crontab -l"
echo ""
echo "Configuration:"
echo "- Edit settings: nano $INSTALL_DIR/config.json"
echo "- After config changes, test with: cd $INSTALL_DIR && python3 post.py"
echo ""
echo "Troubleshooting:"
echo "- If device connection fails, check IP and network connectivity"
echo "- If API fails, verify endpoint URL and API key"
echo "- Check logs for detailed error information"
echo ""
echo "The system will start syncing automatically every 5 minutes."
echo "First sync may take longer as it processes all available records."