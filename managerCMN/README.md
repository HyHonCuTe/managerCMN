# 🏢 CMN Management System

**Complete enterprise HR management system with attendance tracking, holiday management, and advanced Excel reporting.**

[![.NET 9.0](https://img.shields.io/badge/.NET-9.0-blue.svg)](https://dotnet.microsoft.com/)
[![SQL Server](https://img.shields.io/badge/SQL%20Server-2019+-red.svg)](https://www.microsoft.com/en-us/sql-server)
[![License](https://img.shields.io/badge/License-Private-yellow.svg)]()

---

## 🚀 **Quick Start - Production Deployment**

```bash
# Connect to production server
ssh root@103.68.253.22

# Deploy latest version
cd /var/www/cmnmanager-src/managerCMN
./deploy.sh
```

📖 **Complete Guides:**
- 📋 **[Production Deployment & Operations Guide](./README_PRODUCTION.md)** - Comprehensive deployment and maintenance
- ⚡ **[Quick Reference](./QUICK_REFERENCE.md)** - Emergency commands and shortcuts
- 🛠️ **[Deployment Scripts Guide](./README_SCRIPTS.md)** - Automated deployment tools

---

## ✨ **Latest Features (March 2026)**

### 🎉 **NEW: Holiday Management System**
- ✅ **Admin Holiday Management**: Add/edit/delete company holidays via Settings
- ✅ **Calendar Integration**: Holidays display as "Nghỉ lễ" with cyan-green styling
- ✅ **Working Day Calculation**: Holidays automatically excluded from attendance calculations
- ✅ **Recurring Holidays**: Support for annual recurring holidays

### 📊 **NEW: Enhanced Excel Export**
- ✅ **Simplified Display Rules**: Clean P/K/L format instead of verbose text
- ✅ **Color Coding**: Visual indicators for different attendance types
- ✅ **Improved Calculations**: Accurate summary columns for P/K/L values
- ✅ **Export Format**:
  - **"1"** = Full attendance or approved work-counting requests (Green)
  - **"P"/"P/2"** = Paid leave full/half day (Yellow)
  - **"K"/"K/2"** = Unpaid leave/forgot check-in full/half day (Pink)
  - **"L"** = Company holiday (Cyan)

---

## 📋 **System Overview**

**CMN Management** is a comprehensive enterprise management system featuring:

- 👥 **Employee Management** - Complete HR records and lifecycle
- ⏰ **Attendance Tracking** - Clock in/out with overtime calculations
- 🏖️ **Holiday Management** - Company holidays and calendar integration
- 📝 **Request & Approval Workflow** - Multi-level approval process
- 💼 **Asset Management** - Company asset tracking and assignment
- 🎫 **Internal Ticketing** - Support ticket system
- 📊 **Excel Integration** - Advanced import/export with P/K/L formatting
- 🔐 **Role-Based Authorization** - Granular permission system

---

## 🛠️ **Technology Stack**

| Component | Version | Purpose |
|-----------|---------|---------|
| **.NET** | 9.0 | Backend Framework |
| **ASP.NET Core MVC** | 9.0 | Web Framework |
| **Entity Framework Core** | 9.0 | ORM & Database Access |
| **SQL Server** | 2019+ | Primary Database |
| **Google OAuth 2.0** | - | Authentication |
| **ClosedXML** | 0.104+ | Excel Import/Export |
| **Bootstrap** | 5.3 | UI Framework |

---

## 🏗️ **Project Structure**

```
managerCMN/
├── 📁 managerCMN/                    # Main ASP.NET Core project
│   ├── 🎮 Controllers/               # 17+ MVC Controllers
│   ├── 📊 Models/
│   │   ├── 🏗️ Entities/              # 31+ Entity classes
│   │   ├── 📋 Enums/                 # 21+ Enum definitions
│   │   └── 📝 ViewModels/            # 21+ ViewModels
│   ├── 🖼️ Views/                     # Razor Views & Components
│   ├── 💾 Data/                      # Database Context & Seeding
│   ├── 🧠 Services/                  # Business Logic Layer
│   ├── 📚 Repositories/              # Data Access Layer
│   ├── 🔐 Authorization/             # Custom Authorization
│   ├── 🛠️ Helpers/                   # Utility Classes
│   ├── 🌐 wwwroot/                   # Static Files
│   └── 📦 Migrations/                # EF Database Migrations
├── 📋 README_PRODUCTION.md           # Production deployment guide
├── ⚡ QUICK_REFERENCE.md              # Quick commands reference
├── 🚀 deploy.sh                      # Automated deployment script
├── 🔄 rollback.sh                    # Emergency rollback script
└── 💾 daily_backup.sh                # Automated backup script
```

---

## 🎯 **Key Features**

### 🔐 **Authentication & Authorization**
- **Google OAuth 2.0** integration
- **3 Role Levels**: Admin, Manager, User
- **25+ Granular Permissions** for fine-grained access control
- **DevLogin** support for development environments

### 👥 **Employee Management**
- Complete employee lifecycle management
- Excel import/export functionality
- Department, position, and job title organization
- Emergency contact information
- Employee status tracking and reporting

### 💼 **Contract Management**
- **5 Contract Types**: Trial, Fixed-term, Indefinite, Seasonal, Part-time
- Salary tracking and history
- Contract status monitoring
- Start/end date management

### ⏰ **Advanced Attendance System**
- Clock in/out with automatic time calculations
- Overtime and late arrival detection
- **Holiday Integration**: Automatic exclusion from working days
- **Enhanced Calendar View**: Visual holiday indicators
- **Excel Export**: P/K/L format with color coding
- Monthly and custom date range reporting

### 📝 **Request & Approval Workflow**
- **4 Request Types**: Leave, Check-in/out correction, Absence, Work from home
- Multi-level approval workflow
- File attachments support
- Real-time status tracking
- Email notifications

### 💻 **Asset Management**
- **7+ Asset Categories**: Laptops, Monitors, Phones, Printers, etc.
- Employee asset assignment tracking
- Asset lifecycle and history
- Supplier and brand management

### 🎫 **Internal Ticketing System**
- Multi-recipient ticket creation
- Threaded messaging system
- File attachment support
- Priority and urgency levels
- Department-specific routing

### 📊 **Dashboard & Reporting**
- Executive dashboard with key metrics
- System activity logs
- Real-time notifications
- Custom report generation

---

## 🚀 **Production Environment**

- **Server**: Ubuntu 22.04.5 LTS @ 103.68.253.22
- **Database**: SQL Server with connection pooling
- **Web Server**: Nginx reverse proxy with SSL
- **Domain**: https://hyhon.io.vn
- **Service**: systemd with auto-restart
- **Backup**: Automated daily backups with 30-day retention

---

## 👨‍💻 **Development Setup**

### Prerequisites
- .NET 9.0 SDK
- SQL Server 2019+ or SQL Server Express
- Visual Studio 2022 / VS Code
- Git

### Quick Setup
```bash
# Clone repository
git clone <repository-url>
cd managerCMN/managerCMN

# Restore packages
dotnet restore

# Configure connection string in appsettings.json
# Update ConnectionStrings:DefaultConnection

# Apply migrations
dotnet ef database update

# Run application
dotnet run

# Access: http://localhost:5257
```

---

## 🧪 **Testing Features**

### Holiday Management
1. Login as Admin → **Settings** → **Ngày nghỉ lễ**
2. Add holiday (e.g., 30/04/2026 - Giải phóng miền Nam)
3. View **Attendance** calendar → verify "Nghỉ lễ" display

### Excel Export
1. Navigate to **Attendance** → Export Excel
2. Verify new format:
   - **"1"** = Full work (Green background)
   - **"P"** = Paid leave (Yellow background)
   - **"K"** = Unpaid leave (Pink background)
   - **"L"** = Holiday (Cyan background)

---

## 📞 **Support & Maintenance**

### Emergency Commands
```bash
# Service management
systemctl status cmnmanager
systemctl restart cmnmanager

# View logs
journalctl -u cmnmanager -f

# Emergency rollback
./rollback.sh [timestamp]
```

### Health Checks
```bash
# Application health
curl -I http://localhost:5000

# Database connectivity
dotnet ef migrations list

# System resources
free -h && df -h
```

---

## 📚 **Documentation Links**

| Document | Purpose |
|----------|---------|
| [📋 README_PRODUCTION.md](./README_PRODUCTION.md) | Complete production deployment & operations guide |
| [⚡ QUICK_REFERENCE.md](./QUICK_REFERENCE.md) | Emergency commands and quick troubleshooting |
| [🛠️ README_SCRIPTS.md](./README_SCRIPTS.md) | Automated deployment tools documentation |
| [🚀 DEPLOY_NOW.md](./DEPLOY_NOW.md) | Quick deployment commands |

---

## 🔄 **Version History**

- **v2.1 (March 2026)**: Holiday Management + Enhanced Excel Export
- **v2.0**: Production deployment with advanced features
- **v1.0**: Initial release with core HR functionality

---

## 📄 **License & Contact**

- **Developer**: hyhoncute
- **Environment**: Production @ hyhon.io.vn
- **License**: Private Enterprise License
- **Last Updated**: March 21, 2026

---

**🎉 Ready for enterprise deployment with comprehensive automation and monitoring!**