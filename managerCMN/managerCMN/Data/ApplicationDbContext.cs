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

    // HR
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<EmployeeContact> EmployeeContacts => Set<EmployeeContact>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Position> Positions => Set<Position>();
    public DbSet<Contract> Contracts => Set<Contract>();

    // Leave
    public DbSet<LeaveBalance> LeaveBalances => Set<LeaveBalance>();
    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();

    // Requests
    public DbSet<Request> Requests => Set<Request>();
    public DbSet<RequestAttachment> RequestAttachments => Set<RequestAttachment>();

    // Attendance
    public DbSet<Attendance> Attendances => Set<Attendance>();

    // Assets
    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<AssetAssignment> AssetAssignments => Set<AssetAssignment>();
    public DbSet<AssetConfiguration> AssetConfigurations => Set<AssetConfiguration>();
    public DbSet<AssetCategory> AssetCategories => Set<AssetCategory>();
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();

    // Tickets
    public DbSet<Ticket> Tickets => Set<Ticket>();

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

        // Contract
        modelBuilder.Entity<Contract>()
            .Property(c => c.Salary)
            .HasColumnType("decimal(18,2)");

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

        // Attendance unique constraint
        modelBuilder.Entity<Attendance>()
            .HasIndex(a => new { a.EmployeeId, a.Date })
            .IsUnique();

        modelBuilder.Entity<Attendance>()
            .Property(a => a.WorkingHours).HasColumnType("decimal(5,2)");
        modelBuilder.Entity<Attendance>()
            .Property(a => a.OvertimeHours).HasColumnType("decimal(5,2)");

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

        // SystemLog index
        modelBuilder.Entity<SystemLog>()
            .HasIndex(sl => sl.CreatedDate);

        // Seed default roles
        modelBuilder.Entity<Role>().HasData(
            new Role { RoleId = 1, RoleName = "Admin", Description = "System administrator" },
            new Role { RoleId = 2, RoleName = "Manager", Description = "Department manager" },
            new Role { RoleId = 3, RoleName = "User", Description = "Regular employee" }
        );
    }
}
