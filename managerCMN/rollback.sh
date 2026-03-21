#!/bin/bash

# =============================================================================
# QUICK ROLLBACK SCRIPT FOR CMN MANAGER
# =============================================================================
# Usage: ./rollback.sh [backup_timestamp]
# Example: ./rollback.sh 20240321_143022

set -e

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

# Configuration
PROJECT_DIR="/var/www/cmnmanager-src"
SERVICE_NAME="cmnmanager"
BACKUP_DIR="/root/backups"

log_info() { echo -e "${BLUE}[INFO]${NC} $1"; }
log_success() { echo -e "${GREEN}[SUCCESS]${NC} $1"; }
log_warning() { echo -e "${YELLOW}[WARNING]${NC} $1"; }
log_error() { echo -e "${RED}[ERROR]${NC} $1"; }

echo -e "${RED}=== CMN MANAGER EMERGENCY ROLLBACK ===${NC}"
echo

# Check if backup timestamp is provided
if [ $# -eq 0 ]; then
    echo -e "${YELLOW}Available backups:${NC}"
    echo "Code backups:"
    ls -la /var/www/cmnmanager-src-backup-* 2>/dev/null | tail -5 || echo "No code backups found"
    echo
    echo "Database backups:"
    find "$BACKUP_DIR" -name "*.db" -type f -printf "%T@ %Tc %p\n" 2>/dev/null | sort -n | tail -5 || echo "No database backups found"
    echo
    echo -e "${BLUE}Usage: $0 [backup_timestamp]${NC}"
    echo "Example: $0 20240321_143022"
    exit 1
fi

BACKUP_TIMESTAMP=$1

# Verify backups exist
CODE_BACKUP="/var/www/cmnmanager-src-backup-$BACKUP_TIMESTAMP"
DB_BACKUP_SEARCH=$(find "$BACKUP_DIR" -name "*${BACKUP_TIMESTAMP}*.db" -type f | head -1)

if [ ! -d "$CODE_BACKUP" ]; then
    log_error "Code backup not found: $CODE_BACKUP"
    exit 1
fi

if [ -z "$DB_BACKUP_SEARCH" ]; then
    log_warning "Database backup not found for timestamp: $BACKUP_TIMESTAMP"
    echo "Available database backups:"
    find "$BACKUP_DIR" -name "*.db" -type f | tail -5
    read -p "Do you want to continue without database rollback? (y/N): " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        exit 1
    fi
else
    DB_BACKUP="$DB_BACKUP_SEARCH"
    log_info "Found database backup: $DB_BACKUP"
fi

# Confirmation
echo -e "${YELLOW}ROLLBACK DETAILS:${NC}"
echo "Code backup: $CODE_BACKUP"
echo "Database backup: ${DB_BACKUP:-'None (will skip)'}"
echo "Current project will be REPLACED!"
echo
read -p "Are you sure you want to proceed with rollback? (y/N): " -n 1 -r
echo
if [[ ! $REPLY =~ ^[Yy]$ ]]; then
    echo "Rollback cancelled."
    exit 0
fi

# Create emergency backup of current state
EMERGENCY_TIMESTAMP=$(date +%Y%m%d_%H%M%S)
log_info "Creating emergency backup of current state..."
cp -r "$PROJECT_DIR" "/var/www/cmnmanager-src-emergency-$EMERGENCY_TIMESTAMP"

# Stop service
log_info "Stopping application service..."
if systemctl is-active --quiet $SERVICE_NAME; then
    systemctl stop $SERVICE_NAME
    log_success "Service stopped"
fi

# Rollback code
log_info "Rolling back code..."
rm -rf "$PROJECT_DIR"
cp -r "$CODE_BACKUP" "$PROJECT_DIR"
log_success "Code rolled back"

# Rollback database
if [ -n "$DB_BACKUP" ]; then
    log_info "Rolling back database..."

    # Find current database location
    if [ -f "$PROJECT_DIR/cmnmanager.db" ]; then
        cp "$DB_BACKUP" "$PROJECT_DIR/cmnmanager.db"
        log_success "Database rolled back to project directory"
    elif [ -f "$PROJECT_DIR/wwwroot/cmnmanager.db" ]; then
        cp "$DB_BACKUP" "$PROJECT_DIR/wwwroot/cmnmanager.db"
        log_success "Database rolled back to wwwroot directory"
    else
        log_warning "Could not determine database location. Manual restoration may be required."
    fi
fi

# Set permissions
log_info "Setting file permissions..."
chown -R root:root "$PROJECT_DIR"
chmod -R 755 "$PROJECT_DIR"

# Start service
log_info "Starting application service..."
systemctl start $SERVICE_NAME

# Wait and check service status
sleep 5
if systemctl is-active --quiet $SERVICE_NAME; then
    log_success "Service started successfully"
else
    log_error "Service failed to start. Check logs: journalctl -u $SERVICE_NAME -n 50"
    exit 1
fi

# Test application
log_info "Testing application response..."
if curl -f -s http://localhost:5000 > /dev/null; then
    log_success "Application is responding"
else
    log_warning "Application might not be responding on port 5000"
fi

# Reload Nginx if needed
if command -v nginx >/dev/null 2>&1 && systemctl is-active --quiet nginx; then
    log_info "Reloading Nginx..."
    systemctl reload nginx
    log_success "Nginx reloaded"
fi

echo
echo -e "${GREEN}=== ROLLBACK COMPLETED ===${NC}"
echo "Rolled back to: $BACKUP_TIMESTAMP"
echo "Emergency backup of previous state: cmnmanager-src-emergency-$EMERGENCY_TIMESTAMP"
echo "Service status: $(systemctl is-active $SERVICE_NAME)"
echo
echo -e "${YELLOW}Post-rollback checklist:${NC}"
echo "1. Test website functionality"
echo "2. Check application logs: journalctl -u $SERVICE_NAME -f"
echo "3. Verify all features are working"
echo "4. Clean up emergency backup if rollback is successful"
echo
log_success "Rollback completed!"