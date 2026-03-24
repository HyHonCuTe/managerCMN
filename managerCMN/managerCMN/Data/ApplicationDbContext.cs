using Microsoft.EntityFrameworkCore;
using managerCMN.Models.Entities;

namespace managerCMN.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    // Auth
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    // HR
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<EmployeeContact> EmployeeContacts => Set<EmployeeContact>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<JobTitle> JobTitles => Set<JobTitle>();
    public DbSet<Position> Positions => Set<Position>();
    public DbSet<Contract> Contracts => Set<Contract>();

    // Leave
    public DbSet<LeaveBalance> LeaveBalances => Set<LeaveBalance>();
    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();

    // Requests
    public DbSet<Request> Requests => Set<Request>();
    public DbSet<RequestAttachment> RequestAttachments => Set<RequestAttachment>();
    public DbSet<RequestApproval> RequestApprovals => Set<RequestApproval>();

    // Attendance
    public DbSet<Attendance> Attendances => Set<Attendance>();

    // Holidays
    public DbSet<Holiday> Holidays => Set<Holiday>();

    // Assets
    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<AssetAssignment> AssetAssignments => Set<AssetAssignment>();
    public DbSet<AssetConfiguration> AssetConfigurations => Set<AssetConfiguration>();
    public DbSet<AssetCategory> AssetCategories => Set<AssetCategory>();
    public DbSet<AssetLifecycleHistory> AssetLifecycleHistories => Set<AssetLifecycleHistory>();
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();

    // Tickets
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<TicketRecipient> TicketRecipients => Set<TicketRecipient>();
    public DbSet<TicketMessage> TicketMessages => Set<TicketMessage>();
    public DbSet<TicketAttachment> TicketAttachments => Set<TicketAttachment>();

    // System
    public DbSet<SystemLog> SystemLogs => Set<SystemLog>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User -> Employee (1:1 optional)
        modelBuilder.Entity<User>()
            .HasOne(u => u.Employee)
            .WithOne(e => e.User)
            .HasForeignKey<User>(u => u.EmployeeId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasIndex(u => u.GoogleId)
            .IsUnique()
            .HasFilter("[GoogleId] IS NOT NULL");

        // UserRole composite index
        modelBuilder.Entity<UserRole>()
            .HasIndex(ur => new { ur.UserId, ur.RoleId })
            .IsUnique();

        // Employee
        modelBuilder.Entity<Employee>()
            .HasIndex(e => e.EmployeeCode)
            .IsUnique();

        modelBuilder.Entity<Employee>()
            .HasIndex(e => e.Email)
            .IsUnique();

        modelBuilder.Entity<Employee>()
            .HasIndex(e => e.AttendanceCode)
            .IsUnique()
            .HasFilter("[AttendanceCode] IS NOT NULL");

        // Department -> Manager
        modelBuilder.Entity<Department>()
            .HasOne(d => d.Manager)
            .WithMany()
            .HasForeignKey(d => d.ManagerId)
            .OnDelete(DeleteBehavior.SetNull);

        // Employee -> Department
        modelBuilder.Entity<Employee>()
            .HasOne(e => e.Department)
            .WithMany(d => d.Employees)
            .HasForeignKey(e => e.DepartmentId)
            .OnDelete(DeleteBehavior.SetNull);

        // Employee -> Position
        modelBuilder.Entity<Employee>()
            .HasOne(e => e.Position)
            .WithMany(p => p.Employees)
            .HasForeignKey(e => e.PositionId)
            .OnDelete(DeleteBehavior.SetNull);

        // Employee -> JobTitle
        modelBuilder.Entity<Employee>()
            .HasOne(e => e.JobTitle)
            .WithMany(j => j.Employees)
            .HasForeignKey(e => e.JobTitleId)
            .OnDelete(DeleteBehavior.SetNull);

        // Contract
        modelBuilder.Entity<Contract>()
            .Property(c => c.Salary)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<Contract>()
            .HasIndex(c => c.ContractNumber)
            .IsUnique();

        // LeaveBalance
        modelBuilder.Entity<LeaveBalance>()
            .HasIndex(lb => new { lb.EmployeeId, lb.Year })
            .IsUnique();

        modelBuilder.Entity<LeaveBalance>()
            .Property(lb => lb.TotalLeave).HasColumnType("decimal(5,1)");
        modelBuilder.Entity<LeaveBalance>()
            .Property(lb => lb.UsedLeave).HasColumnType("decimal(5,1)");
        modelBuilder.Entity<LeaveBalance>()
            .Property(lb => lb.RemainingLeave).HasColumnType("decimal(5,1)");
        modelBuilder.Entity<LeaveBalance>()
            .Property(lb => lb.CarryForward).HasColumnType("decimal(5,1)");

        // LeaveRequest
        modelBuilder.Entity<LeaveRequest>()
            .Property(lr => lr.TotalDays).HasColumnType("decimal(5,1)");
        modelBuilder.Entity<LeaveRequest>()
            .Property(lr => lr.DeductedFromCurrentYear).HasColumnType("decimal(5,1)");
        modelBuilder.Entity<LeaveRequest>()
            .Property(lr => lr.DeductedFromCarryForward).HasColumnType("decimal(5,1)");

        // Request TotalDays precision
        modelBuilder.Entity<Request>()
            .Property(r => r.TotalDays)
            .HasColumnType("decimal(5,1)");

        // RequestApproval
        modelBuilder.Entity<RequestApproval>()
            .HasOne(ra => ra.Request)
            .WithMany(r => r.Approvals)
            .HasForeignKey(ra => ra.RequestId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RequestApproval>()
            .HasOne(ra => ra.Approver)
            .WithMany()
            .HasForeignKey(ra => ra.ApproverId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<RequestApproval>()
            .HasIndex(ra => new { ra.RequestId, ra.ApproverOrder })
            .IsUnique();

        // Attendance unique constraint
        modelBuilder.Entity<Attendance>()
            .HasIndex(a => new { a.EmployeeId, a.Date })
            .IsUnique();

        modelBuilder.Entity<Attendance>()
            .Property(a => a.WorkingHours).HasColumnType("decimal(5,2)");
        modelBuilder.Entity<Attendance>()
            .Property(a => a.OvertimeHours).HasColumnType("decimal(5,2)");
        modelBuilder.Entity<Attendance>()
            .Property(a => a.LateMinutes).HasDefaultValue(0);

        // Holiday unique constraint on Date
        modelBuilder.Entity<Holiday>()
            .HasIndex(h => h.Date)
            .IsUnique();

        // Asset
        modelBuilder.Entity<Asset>()
            .HasIndex(a => a.AssetCode)
            .IsUnique();

        modelBuilder.Entity<Asset>()
            .Property(a => a.PurchasePrice)
            .HasColumnType("decimal(18,2)");

        // AssetConfiguration 1:1
        modelBuilder.Entity<AssetConfiguration>()
            .HasOne(ac => ac.Asset)
            .WithOne(a => a.Configuration)
            .HasForeignKey<AssetConfiguration>(ac => ac.AssetId);

        // AssetAssignment -> Employee (main employee relationship)
        modelBuilder.Entity<AssetAssignment>()
            .HasOne(aa => aa.Employee)
            .WithMany(e => e.AssetAssignments)
            .HasForeignKey(aa => aa.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        // AssetAssignment -> Employee (ApprovedBy)
        modelBuilder.Entity<AssetAssignment>()
            .HasOne(aa => aa.ApprovedBy)
            .WithMany()
            .HasForeignKey(aa => aa.ApprovedById)
            .OnDelete(DeleteBehavior.SetNull);

        // AssetLifecycleHistory -> Employee (Employee)
        modelBuilder.Entity<AssetLifecycleHistory>()
            .HasOne(alh => alh.Employee)
            .WithMany()
            .HasForeignKey(alh => alh.EmployeeId)
            .OnDelete(DeleteBehavior.NoAction);

        // AssetLifecycleHistory -> Employee (PerformedBy)
        modelBuilder.Entity<AssetLifecycleHistory>()
            .HasOne(alh => alh.PerformedBy)
            .WithMany()
            .HasForeignKey(alh => alh.PerformedById)
            .OnDelete(DeleteBehavior.NoAction);

        // Ticket
        modelBuilder.Entity<Ticket>()
            .HasOne(t => t.Creator)
            .WithMany(e => e.CreatedTickets)
            .HasForeignKey(t => t.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Ticket>()
            .HasOne(t => t.Assignee)
            .WithMany(e => e.AssignedTickets)
            .HasForeignKey(t => t.AssignedTo)
            .OnDelete(DeleteBehavior.SetNull);

        // TicketRecipient configurations
        modelBuilder.Entity<TicketRecipient>()
            .HasOne(tr => tr.Ticket)
            .WithMany(t => t.Recipients)
            .HasForeignKey(tr => tr.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TicketRecipient>()
            .HasOne(tr => tr.Employee)
            .WithMany()
            .HasForeignKey(tr => tr.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TicketRecipient>()
            .HasOne(tr => tr.AddedBy)
            .WithMany()
            .HasForeignKey(tr => tr.AddedById)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<TicketRecipient>()
            .HasIndex(tr => new { tr.TicketId, tr.EmployeeId })
            .IsUnique();

        // TicketMessage configurations
        modelBuilder.Entity<TicketMessage>()
            .HasOne(tm => tm.Ticket)
            .WithMany(t => t.Messages)
            .HasForeignKey(tm => tm.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TicketMessage>()
            .HasOne(tm => tm.Sender)
            .WithMany()
            .HasForeignKey(tm => tm.SenderId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TicketMessage>()
            .HasOne(tm => tm.ForwardedTo)
            .WithMany()
            .HasForeignKey(tm => tm.ForwardedToId)
            .OnDelete(DeleteBehavior.NoAction);

        // TicketAttachment configurations
        modelBuilder.Entity<TicketAttachment>()
            .HasOne(ta => ta.Ticket)
            .WithMany(t => t.Attachments)
            .HasForeignKey(ta => ta.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TicketAttachment>()
            .HasOne(ta => ta.TicketMessage)
            .WithMany(tm => tm.Attachments)
            .HasForeignKey(ta => ta.TicketMessageId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<TicketAttachment>()
            .HasOne(ta => ta.UploadedBy)
            .WithMany()
            .HasForeignKey(ta => ta.UploadedById)
            .OnDelete(DeleteBehavior.Restrict);

        // SystemLog index
        modelBuilder.Entity<SystemLog>()
            .HasIndex(sl => sl.CreatedDate);

        // Notification indexes for query performance
        modelBuilder.Entity<Notification>()
            .HasIndex(n => new { n.UserId, n.CreatedDate });

        modelBuilder.Entity<Notification>()
            .HasIndex(n => new { n.UserId, n.IsRead, n.CreatedDate });

        // Permission
        modelBuilder.Entity<Permission>()
            .HasIndex(p => p.PermissionKey)
            .IsUnique();

        // RolePermission
        modelBuilder.Entity<RolePermission>()
            .HasIndex(rp => new { rp.RoleId, rp.PermissionId })
            .IsUnique();

        modelBuilder.Entity<RolePermission>()
            .HasOne(rp => rp.Role)
            .WithMany(r => r.RolePermissions)
            .HasForeignKey(rp => rp.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RolePermission>()
            .HasOne(rp => rp.Permission)
            .WithMany(p => p.RolePermissions)
            .HasForeignKey(rp => rp.PermissionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Seed default roles
        modelBuilder.Entity<Role>().HasData(
            new Role { RoleId = 1, RoleName = "Admin", Description = "System administrator" },
            new Role { RoleId = 2, RoleName = "Manager", Description = "Department manager" },
            new Role { RoleId = 3, RoleName = "User", Description = "Regular employee" }
        );

        // Seed permissions
        modelBuilder.Entity<Permission>().HasData(
            // Employee Management (1-5)
            new Permission { PermissionId = 1, PermissionKey = "Employee.View", PermissionName = "Xem danh sách nhân viên", Category = "Employee", Description = "Xem thông tin nhân viên", SortOrder = 1, IsActive = true },
            new Permission { PermissionId = 2, PermissionKey = "Employee.Create", PermissionName = "Tạo nhân viên mới", Category = "Employee", Description = "Thêm nhân viên mới vào hệ thống", SortOrder = 2, IsActive = true },
            new Permission { PermissionId = 3, PermissionKey = "Employee.Edit", PermissionName = "Sửa thông tin nhân viên", Category = "Employee", Description = "Chỉnh sửa thông tin nhân viên", SortOrder = 3, IsActive = true },
            new Permission { PermissionId = 4, PermissionKey = "Employee.Delete", PermissionName = "Xóa nhân viên", Category = "Employee", Description = "Xóa nhân viên khỏi hệ thống", SortOrder = 4, IsActive = true },
            new Permission { PermissionId = 5, PermissionKey = "Employee.ViewSalary", PermissionName = "Xem lương nhân viên", Category = "Employee", Description = "Xem thông tin lương và hợp đồng nhân viên", SortOrder = 5, IsActive = true },

            // Request Management (6-9)
            new Permission { PermissionId = 6, PermissionKey = "Request.View", PermissionName = "Xem đơn từ", Category = "Request", Description = "Xem danh sách đơn từ", SortOrder = 1, IsActive = true },
            new Permission { PermissionId = 7, PermissionKey = "Request.Create", PermissionName = "Tạo đơn từ", Category = "Request", Description = "Tạo đơn từ mới", SortOrder = 2, IsActive = true },
            new Permission { PermissionId = 8, PermissionKey = "Request.Approve", PermissionName = "Duyệt đơn từ", Category = "Request", Description = "Duyệt hoặc từ chối đơn từ", SortOrder = 3, IsActive = true },
            new Permission { PermissionId = 9, PermissionKey = "Request.Delete", PermissionName = "Xóa đơn từ", Category = "Request", Description = "Xóa đơn từ khỏi hệ thống", SortOrder = 4, IsActive = true },

            // Attendance Management (10-12)
            new Permission { PermissionId = 10, PermissionKey = "Attendance.View", PermissionName = "Xem chấm công", Category = "Attendance", Description = "Xem dữ liệu chấm công", SortOrder = 1, IsActive = true },
            new Permission { PermissionId = 11, PermissionKey = "Attendance.Edit", PermissionName = "Sửa chấm công", Category = "Attendance", Description = "Chỉnh sửa dữ liệu chấm công", SortOrder = 2, IsActive = true },
            new Permission { PermissionId = 12, PermissionKey = "Attendance.Export", PermissionName = "Xuất báo cáo chấm công", Category = "Attendance", Description = "Xuất file báo cáo chấm công", SortOrder = 3, IsActive = true },

            // Asset Management (13-17)
            new Permission { PermissionId = 13, PermissionKey = "Asset.View", PermissionName = "Xem tài sản", Category = "Asset", Description = "Xem danh sách tài sản", SortOrder = 1, IsActive = true },
            new Permission { PermissionId = 14, PermissionKey = "Asset.Create", PermissionName = "Tạo tài sản", Category = "Asset", Description = "Thêm tài sản mới", SortOrder = 2, IsActive = true },
            new Permission { PermissionId = 15, PermissionKey = "Asset.Edit", PermissionName = "Sửa tài sản", Category = "Asset", Description = "Chỉnh sửa thông tin tài sản", SortOrder = 3, IsActive = true },
            new Permission { PermissionId = 16, PermissionKey = "Asset.Delete", PermissionName = "Xóa tài sản", Category = "Asset", Description = "Xóa tài sản khỏi hệ thống", SortOrder = 4, IsActive = true },
            new Permission { PermissionId = 17, PermissionKey = "Asset.Assign", PermissionName = "Gán tài sản", Category = "Asset", Description = "Gán tài sản cho nhân viên", SortOrder = 5, IsActive = true },

            // Settings Management (18-21)
            new Permission { PermissionId = 18, PermissionKey = "Settings.ViewDepartments", PermissionName = "Xem cài đặt danh mục", Category = "Settings", Description = "Xem phòng ban, chức vụ, vị trí", SortOrder = 1, IsActive = true },
            new Permission { PermissionId = 19, PermissionKey = "Settings.ManageDepartments", PermissionName = "Quản lý danh mục", Category = "Settings", Description = "Thêm, sửa, xóa phòng ban và danh mục", SortOrder = 2, IsActive = true },
            new Permission { PermissionId = 20, PermissionKey = "Settings.ViewPermissions", PermissionName = "Xem phân quyền", Category = "Settings", Description = "Xem phân quyền hệ thống", SortOrder = 3, IsActive = true },
            new Permission { PermissionId = 21, PermissionKey = "Settings.ManagePermissions", PermissionName = "Quản lý phân quyền", Category = "Settings", Description = "Thêm, sửa, xóa quyền và phân quyền cho vai trò", SortOrder = 4, IsActive = true },

            // System Management (22-25)
            new Permission { PermissionId = 22, PermissionKey = "System.ViewLogs", PermissionName = "Xem system logs", Category = "System", Description = "Xem nhật ký hệ thống", SortOrder = 1, IsActive = true },
            new Permission { PermissionId = 23, PermissionKey = "System.ManageUsers", PermissionName = "Quản lý người dùng", Category = "System", Description = "Quản lý tài khoản người dùng", SortOrder = 2, IsActive = true },
            new Permission { PermissionId = 24, PermissionKey = "System.ViewReports", PermissionName = "Xem báo cáo", Category = "System", Description = "Xem các báo cáo hệ thống", SortOrder = 3, IsActive = true },
            new Permission { PermissionId = 25, PermissionKey = "System.ALL", PermissionName = "⭐ TOÀN QUYỀN HỆ THỐNG", Category = "System", Description = "Quyền cao nhất - Được làm mọi thứ trong hệ thống", SortOrder = 99, IsActive = true }
        );

        // Seed default role permissions
        modelBuilder.Entity<RolePermission>().HasData(
            // Admin role (RoleId = 1) - All permissions EXCEPT System.ALL
            new RolePermission { RolePermissionId = 1, RoleId = 1, PermissionId = 1, AssignedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new RolePermission { RolePermissionId = 2, RoleId = 1, PermissionId = 2, AssignedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new RolePermission { RolePermissionId = 3, RoleId = 1, PermissionId = 3, AssignedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new RolePermission { RolePermissionId = 4, RoleId = 1, PermissionId = 4, AssignedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new RolePermission { RolePermissionId = 5, RoleId = 1, PermissionId = 5, AssignedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new RolePermission { RolePermissionId = 6, RoleId = 1, PermissionId = 6, AssignedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new RolePermission { RolePermissionId = 7, RoleId = 1, PermissionId = 7, AssignedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new RolePermission { RolePermissionId = 8, RoleId = 1, PermissionId = 8, AssignedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new RolePermission { RolePermissionId = 9, RoleId = 1, PermissionId = 9, AssignedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new RolePermission { RolePermissionId = 10, RoleId = 1, PermissionId = 10, AssignedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new RolePermission { RolePermissionId = 11, RoleId = 1, PermissionId = 11, AssignedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new RolePermission { RolePermissionId = 12, RoleId = 1, PermissionId = 12, AssignedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new RolePermission { RolePermissionId = 13, RoleId = 1, PermissionId = 13, AssignedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new RolePermission { RolePermissionId = 14, RoleId = 1, PermissionId = 14, AssignedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new RolePermission { RolePermissionId = 15, RoleId = 1, PermissionId = 15, AssignedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new RolePermission { RolePermissionId = 16, RoleId = 1, PermissionId = 16, AssignedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new RolePermission { RolePermissionId = 17, RoleId = 1, PermissionId = 17, AssignedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new RolePermission { RolePermissionId = 18, RoleId = 1, PermissionId = 18, AssignedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new RolePermission { RolePermissionId = 19, RoleId = 1, PermissionId = 19, AssignedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new RolePermission { RolePermissionId = 20, RoleId = 1, PermissionId = 20, AssignedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new RolePermission { RolePermissionId = 21, RoleId = 1, PermissionId = 21, AssignedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new RolePermission { RolePermissionId = 22, RoleId = 1, PermissionId = 22, AssignedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new RolePermission { RolePermissionId = 23, RoleId = 1, PermissionId = 23, AssignedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new RolePermission { RolePermissionId = 24, RoleId = 1, PermissionId = 24, AssignedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },

            // Manager role (RoleId = 2) - Employee.*, Request.*, Attendance.*, Asset.View
            new RolePermission { RolePermissionId = 25, RoleId = 2, PermissionId = 1, AssignedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) }, // Employee.View
            new RolePermission { RolePermissionId = 26, RoleId = 2, PermissionId = 2, AssignedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) }, // Employee.Create
            new RolePermission { RolePermissionId = 27, RoleId = 2, PermissionId = 3, AssignedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) }, // Employee.Edit
            new RolePermission { RolePermissionId = 28, RoleId = 2, PermissionId = 6, AssignedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) }, // Request.View
            new RolePermission { RolePermissionId = 29, RoleId = 2, PermissionId = 7, AssignedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) }, // Request.Create
            new RolePermission { RolePermissionId = 30, RoleId = 2, PermissionId = 8, AssignedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) }, // Request.Approve
            new RolePermission { RolePermissionId = 31, RoleId = 2, PermissionId = 10, AssignedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) }, // Attendance.View
            new RolePermission { RolePermissionId = 32, RoleId = 2, PermissionId = 11, AssignedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) }, // Attendance.Edit
            new RolePermission { RolePermissionId = 33, RoleId = 2, PermissionId = 12, AssignedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) }, // Attendance.Export
            new RolePermission { RolePermissionId = 34, RoleId = 2, PermissionId = 13, AssignedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) }, // Asset.View

            // User role (RoleId = 3) - Basic permissions
            new RolePermission { RolePermissionId = 35, RoleId = 3, PermissionId = 6, AssignedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) }, // Request.View
            new RolePermission { RolePermissionId = 36, RoleId = 3, PermissionId = 7, AssignedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) }, // Request.Create
            new RolePermission { RolePermissionId = 37, RoleId = 3, PermissionId = 10, AssignedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) } // Attendance.View
        );

        // Seed job titles (Chức vụ)
        modelBuilder.Entity<JobTitle>().HasData(
            new JobTitle { JobTitleId = 1, JobTitleName = "Ban Giám Đốc", SortOrder = 1 },
            new JobTitle { JobTitleId = 2, JobTitleName = "Trưởng phòng", SortOrder = 2 },
            new JobTitle { JobTitleId = 3, JobTitleName = "Manager", SortOrder = 3 },
            new JobTitle { JobTitleId = 4, JobTitleName = "Nhân viên", SortOrder = 4 },
            new JobTitle { JobTitleId = 5, JobTitleName = "Thực tập", SortOrder = 5 }
        );
    }
}
