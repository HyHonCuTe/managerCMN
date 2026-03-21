#!/bin/bash

# =============================================================================
# DAILY BACKUP SCRIPT FOR CMN MANAGER
# =============================================================================
# Setup cron job: 0 2 * * * /root/scripts/daily_backup.sh
# This runs every day at 2:00 AM

# Configuration
PROJECT_DIR="/var/www/cmnmanager-src"
BACKUP_BASE_DIR="/root/backups"
DAILY_BACKUP_DIR="$BACKUP_BASE_DIR/daily"
TIMESTAMP=$(date +%Y%m%d_%H%M%S)
TODAY=$(date +%Y%m%d)
RETENTION_DAYS=30  # Keep backups for 30 days

# Colors
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
NC='\033[0m'

log_info() { echo -e "${BLUE}[$(date '+%Y-%m-%d %H:%M:%S')] [INFO]${NC} $1"; }
log_success() { echo -e "${GREEN}[$(date '+%Y-%m-%d %H:%M:%S')] [SUCCESS]${NC} $1"; }
log_warning() { echo -e "${YELLOW}[$(date '+%Y-%m-%d %H:%M:%S')] [WARNING]${NC} $1"; }

# Create backup directory
mkdir -p "$DAILY_BACKUP_DIR/$TODAY"
cd "$DAILY_BACKUP_DIR/$TODAY"

log_info "Starting daily backup for CMN Manager..."

# Backup database
log_info "Backing up database..."
if [ -f "$PROJECT_DIR/cmnmanager.db" ]; then
    cp "$PROJECT_DIR/cmnmanager.db" "./cmnmanager_${TIMESTAMP}.db"
    log_success "Database backed up"
elif [ -f "$PROJECT_DIR/wwwroot/cmnmanager.db" ]; then
    cp "$PROJECT_DIR/wwwroot/cmnmanager.db" "./cmnmanager_${TIMESTAMP}.db"
    log_success "Database backed up"
else
    log_warning "SQLite database not found"
fi

# Backup uploads directory
if [ -d "$PROJECT_DIR/wwwroot/uploads" ]; then
    log_info "Backing up uploads directory..."
    tar -czf "uploads_${TIMESTAMP}.tar.gz" -C "$PROJECT_DIR/wwwroot" uploads/
    log_success "Uploads directory backed up"
fi

# Backup configuration files
log_info "Backing up configuration files..."
mkdir -p config
cp "$PROJECT_DIR"/*.json config/ 2>/dev/null || true
cp "$PROJECT_DIR/Properties/launchSettings.json" config/ 2>/dev/null || true
log_success "Configuration files backed up"

# Create a summary file
cat > "backup_summary_${TIMESTAMP}.txt" << EOF
CMN Manager Daily Backup Summary
================================
Date: $(date)
Backup Location: $DAILY_BACKUP_DIR/$TODAY
Project Directory: $PROJECT_DIR

Files backed up:
$(ls -la)

Database Size: $(du -h cmnmanager_*.db 2>/dev/null || echo "Not found")
Uploads Size: $(du -h uploads_*.tar.gz 2>/dev/null || echo "Not found")
Total Backup Size: $(du -sh . | cut -f1)

Service Status: $(systemctl is-active cmnmanager 2>/dev/null || echo "Unknown")
Disk Space: $(df -h /root/backups | tail -1)
EOF

log_success "Backup summary created"

# Cleanup old backups (keep only last RETENTION_DAYS days)
log_info "Cleaning up old backups (keeping last $RETENTION_DAYS days)..."
find "$DAILY_BACKUP_DIR" -type d -name "????????_*" -mtime +$RETENTION_DAYS -exec rm -rf {} \; 2>/dev/null || true
find "$DAILY_BACKUP_DIR" -type d -name "????????" -mtime +$RETENTION_DAYS -exec rm -rf {} \; 2>/dev/null || true

# Log disk usage
DISK_USAGE=$(df /root/backups | tail -1 | awk '{print $5}' | sed 's/%//')
if [ "$DISK_USAGE" -gt 80 ]; then
    log_warning "Backup disk usage is high: ${DISK_USAGE}%"
fi

log_success "Daily backup completed successfully"
log_info "Backup location: $DAILY_BACKUP_DIR/$TODAY"
log_info "Total size: $(du -sh $DAILY_BACKUP_DIR/$TODAY | cut -f1)"

# Send notification (optional - uncomment and configure if needed)
# echo "CMN Manager daily backup completed: $TODAY" | mail -s "Backup Status" admin@example.com