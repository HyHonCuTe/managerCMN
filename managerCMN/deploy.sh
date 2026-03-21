#!/bin/bash

# =============================================================================
# QUICK PRODUCTION DEPLOYMENT SCRIPT
# =============================================================================
# Sử dụng: ./deploy.sh
# Chạy script này trên server production để tự động deploy

set -e  # Exit on any error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
PROJECT_DIR="/var/www/cmnmanager-src"
SERVICE_NAME="cmnmanager"
BACKUP_DIR="/root/backups"
TIMESTAMP=$(date +%Y%m%d_%H%M%S)

echo -e "${BLUE}=== CMN Manager Production Deployment ===${NC}"
echo "Timestamp: $(date)"
echo "Project Directory: $PROJECT_DIR"
echo

# Function to print colored output
log_info() { echo -e "${BLUE}[INFO]${NC} $1"; }
log_success() { echo -e "${GREEN}[SUCCESS]${NC} $1"; }
log_warning() { echo -e "${YELLOW}[WARNING]${NC} $1"; }
log_error() { echo -e "${RED}[ERROR]${NC} $1"; }

# Function to check if command exists
command_exists() {
    command -v "$1" >/dev/null 2>&1
}

# Verify prerequisites
log_info "Checking prerequisites..."
if ! command_exists dotnet; then
    log_error ".NET runtime not found. Please install .NET 9.0"
    exit 1
fi

if ! command_exists git; then
    log_error "Git not found. Please install git"
    exit 1
fi

if [ ! -d "$PROJECT_DIR" ]; then
    log_error "Project directory $PROJECT_DIR not found"
    exit 1
fi

log_success "Prerequisites check passed"

# Create backup directory
log_info "Creating backup directory..."
mkdir -p "$BACKUP_DIR/$TIMESTAMP"
cd "$BACKUP_DIR/$TIMESTAMP"

# Backup database
log_info "Backing up database..."
if [ -f "$PROJECT_DIR/cmnmanager.db" ]; then
    cp "$PROJECT_DIR/cmnmanager.db" "./cmnmanager_backup.db"
    log_success "SQLite database backed up"
elif [ -f "$PROJECT_DIR/wwwroot/cmnmanager.db" ]; then
    cp "$PROJECT_DIR/wwwroot/cmnmanager.db" "./cmnmanager_backup.db"
    log_success "SQLite database backed up"
else
    log_warning "Database file not found. You may need to backup manually if using MySQL/PostgreSQL"
fi

# Backup uploads directory
if [ -d "$PROJECT_DIR/wwwroot/uploads" ]; then
    log_info "Backing up uploads..."
    cp -r "$PROJECT_DIR/wwwroot/uploads" "./uploads_backup/"
    log_success "Uploads directory backed up"
fi

# Stop the service
log_info "Stopping application service..."
if systemctl is-active --quiet $SERVICE_NAME; then
    systemctl stop $SERVICE_NAME
    log_success "Service stopped"
else
    log_warning "Service was not running"
fi

# Backup current code
log_info "Backing up current code..."
cd /var/www
cp -r cmnmanager-src "cmnmanager-src-backup-$TIMESTAMP"
log_success "Code backed up to cmnmanager-src-backup-$TIMESTAMP"

# Pull latest code
log_info "Pulling latest code from GitHub..."
cd $PROJECT_DIR

# Save any local config files
cp appsettings.Production.json "/tmp/appsettings.Production.json.backup" 2>/dev/null || true

# Stash local changes and pull
git stash push -m "Auto stash before deployment $TIMESTAMP"
git fetch origin
git reset --hard origin/main
git pull origin main

# Restore production config if it was backed up
if [ -f "/tmp/appsettings.Production.json.backup" ]; then
    cp "/tmp/appsettings.Production.json.backup" appsettings.Production.json
    log_success "Production configuration restored"
fi

log_success "Code updated successfully"

# Restore and build
log_info "Restoring NuGet packages..."
dotnet restore

log_info "Building application..."
dotnet build --configuration Release --no-restore

log_info "Publishing application..."
dotnet publish --configuration Release --output ./bin/Release/publish --no-build

log_success "Application built successfully"

# Update database
log_info "Updating database migrations..."
export ASPNETCORE_ENVIRONMENT=Production
dotnet ef database update --verbose

log_success "Database updated successfully"

# Set proper permissions
log_info "Setting file permissions..."
chown -R root:root $PROJECT_DIR
chmod -R 755 $PROJECT_DIR
chmod 644 $PROJECT_DIR/*.json $PROJECT_DIR/*.db 2>/dev/null || true

# Start the service
log_info "Starting application service..."
systemctl daemon-reload
systemctl start $SERVICE_NAME
systemctl enable $SERVICE_NAME

# Wait a moment for service to start
sleep 5

# Check service status
if systemctl is-active --quiet $SERVICE_NAME; then
    log_success "Service started successfully"
else
    log_error "Service failed to start. Check logs with: journalctl -u $SERVICE_NAME -n 50"
    exit 1
fi

# Test application response
log_info "Testing application response..."
if curl -f -s http://localhost:5000 > /dev/null; then
    log_success "Application is responding"
else
    log_warning "Application might not be responding on port 5000"
fi

# Reload Nginx if it exists
if command_exists nginx && systemctl is-active --quiet nginx; then
    log_info "Reloading Nginx..."
    nginx -t && systemctl reload nginx
    log_success "Nginx reloaded"
fi

# Display final status
echo
echo -e "${GREEN}=== DEPLOYMENT COMPLETED SUCCESSFULLY ===${NC}"
echo "Timestamp: $(date)"
echo "Backup location: $BACKUP_DIR/$TIMESTAMP"
echo "Service status: $(systemctl is-active $SERVICE_NAME)"
echo
echo -e "${YELLOW}Post-deployment checklist:${NC}"
echo "1. Test website functionality"
echo "2. Check application logs: journalctl -u $SERVICE_NAME -f"
echo "3. Monitor for any errors"
echo "4. Test new features (holiday management, Excel export)"
echo
echo -e "${BLUE}Rollback command (if needed):${NC}"
echo "sudo systemctl stop $SERVICE_NAME && sudo mv /var/www/cmnmanager-src-backup-$TIMESTAMP /var/www/cmnmanager-src && sudo systemctl start $SERVICE_NAME"
echo
log_success "Deployment script completed!"