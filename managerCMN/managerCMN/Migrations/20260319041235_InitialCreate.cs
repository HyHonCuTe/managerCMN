using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace managerCMN.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AssetCategories",
                columns: table => new
                {
                    AssetCategoryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetCategories", x => x.AssetCategoryId);
                });

            migrationBuilder.CreateTable(
                name: "Brands",
                columns: table => new
                {
                    BrandId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BrandName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Brands", x => x.BrandId);
                });

            migrationBuilder.CreateTable(
                name: "JobTitles",
                columns: table => new
                {
                    JobTitleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JobTitleName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobTitles", x => x.JobTitleId);
                });

            migrationBuilder.CreateTable(
                name: "Permissions",
                columns: table => new
                {
                    PermissionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PermissionKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PermissionName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.PermissionId);
                });

            migrationBuilder.CreateTable(
                name: "Positions",
                columns: table => new
                {
                    PositionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PositionName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Positions", x => x.PositionId);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    RoleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.RoleId);
                });

            migrationBuilder.CreateTable(
                name: "Suppliers",
                columns: table => new
                {
                    SupplierId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SupplierName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Suppliers", x => x.SupplierId);
                });

            migrationBuilder.CreateTable(
                name: "SystemLogs",
                columns: table => new
                {
                    LogId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    Action = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Module = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DataBefore = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DataAfter = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IPAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemLogs", x => x.LogId);
                });

            migrationBuilder.CreateTable(
                name: "RolePermissions",
                columns: table => new
                {
                    RolePermissionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    PermissionId = table.Column<int>(type: "int", nullable: false),
                    AssignedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermissions", x => x.RolePermissionId);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "Permissions",
                        principalColumn: "PermissionId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "RoleId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Assets",
                columns: table => new
                {
                    AssetId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssetCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AssetName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AssetCategoryId = table.Column<int>(type: "int", nullable: true),
                    BrandId = table.Column<int>(type: "int", nullable: true),
                    SupplierId = table.Column<int>(type: "int", nullable: true),
                    PurchaseDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PurchasePrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assets", x => x.AssetId);
                    table.ForeignKey(
                        name: "FK_Assets_AssetCategories_AssetCategoryId",
                        column: x => x.AssetCategoryId,
                        principalTable: "AssetCategories",
                        principalColumn: "AssetCategoryId");
                    table.ForeignKey(
                        name: "FK_Assets_Brands_BrandId",
                        column: x => x.BrandId,
                        principalTable: "Brands",
                        principalColumn: "BrandId");
                    table.ForeignKey(
                        name: "FK_Assets_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "SupplierId");
                });

            migrationBuilder.CreateTable(
                name: "AssetConfigurations",
                columns: table => new
                {
                    AssetId = table.Column<int>(type: "int", nullable: false),
                    CPU = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Mainboard = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RAM = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SSD = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    HDD = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    VGA = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OS = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetConfigurations", x => x.AssetId);
                    table.ForeignKey(
                        name: "FK_AssetConfigurations_Assets_AssetId",
                        column: x => x.AssetId,
                        principalTable: "Assets",
                        principalColumn: "AssetId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AssetAssignments",
                columns: table => new
                {
                    AssignmentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssetId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    AssignedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReturnDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    AssignmentReason = table.Column<int>(type: "int", nullable: false),
                    ReturnReason = table.Column<int>(type: "int", nullable: true),
                    AssignmentCondition = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ReturnCondition = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Condition = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ApprovedById = table.Column<int>(type: "int", nullable: true),
                    ApprovedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetAssignments", x => x.AssignmentId);
                    table.ForeignKey(
                        name: "FK_AssetAssignments_Assets_AssetId",
                        column: x => x.AssetId,
                        principalTable: "Assets",
                        principalColumn: "AssetId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AssetLifecycleHistories",
                columns: table => new
                {
                    HistoryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssetId = table.Column<int>(type: "int", nullable: false),
                    EventType = table.Column<int>(type: "int", nullable: false),
                    EventDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: true),
                    PerformedById = table.Column<int>(type: "int", nullable: true),
                    EventDescription = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    PreviousValue = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    NewValue = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetLifecycleHistories", x => x.HistoryId);
                    table.ForeignKey(
                        name: "FK_AssetLifecycleHistories_Assets_AssetId",
                        column: x => x.AssetId,
                        principalTable: "Assets",
                        principalColumn: "AssetId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Attendances",
                columns: table => new
                {
                    AttendanceId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    CheckIn = table.Column<TimeOnly>(type: "time", nullable: true),
                    CheckOut = table.Column<TimeOnly>(type: "time", nullable: true),
                    WorkingHours = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    OvertimeHours = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    IsLate = table.Column<bool>(type: "bit", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Attendances", x => x.AttendanceId);
                });

            migrationBuilder.CreateTable(
                name: "Contracts",
                columns: table => new
                {
                    ContractId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    ContractType = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Salary = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contracts", x => x.ContractId);
                });

            migrationBuilder.CreateTable(
                name: "Departments",
                columns: table => new
                {
                    DepartmentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DepartmentName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ManagerId = table.Column<int>(type: "int", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departments", x => x.DepartmentId);
                });

            migrationBuilder.CreateTable(
                name: "Employees",
                columns: table => new
                {
                    EmployeeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DateOfBirth = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Gender = table.Column<int>(type: "int", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    PermanentAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TemporaryAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AttendanceCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    AttendanceName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TaxCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    BankAccount = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    BankName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DepartmentId = table.Column<int>(type: "int", nullable: true),
                    JobTitleId = table.Column<int>(type: "int", nullable: true),
                    PositionId = table.Column<int>(type: "int", nullable: true),
                    Ethnicity = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Nationality = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IdCardNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    IdCardIssueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IdCardIssuePlace = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Qualifications = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    StartWorkingDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    InsuranceCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ResignationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResignationReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    VehiclePlate = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FacebookUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    IsApprover = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Employees", x => x.EmployeeId);
                    table.ForeignKey(
                        name: "FK_Employees_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "DepartmentId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Employees_JobTitles_JobTitleId",
                        column: x => x.JobTitleId,
                        principalTable: "JobTitles",
                        principalColumn: "JobTitleId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Employees_Positions_PositionId",
                        column: x => x.PositionId,
                        principalTable: "Positions",
                        principalColumn: "PositionId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeContacts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Relationship = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeContacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeContacts_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "EmployeeId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LeaveBalances",
                columns: table => new
                {
                    LeaveId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    TotalLeave = table.Column<decimal>(type: "decimal(5,1)", nullable: false),
                    UsedLeave = table.Column<decimal>(type: "decimal(5,1)", nullable: false),
                    RemainingLeave = table.Column<decimal>(type: "decimal(5,1)", nullable: false),
                    CarryForward = table.Column<decimal>(type: "decimal(5,1)", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveBalances", x => x.LeaveId);
                    table.ForeignKey(
                        name: "FK_LeaveBalances_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "EmployeeId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LeaveRequests",
                columns: table => new
                {
                    RequestId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PayType = table.Column<int>(type: "int", nullable: false),
                    TotalDays = table.Column<decimal>(type: "decimal(5,1)", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ApprovedBy = table.Column<int>(type: "int", nullable: true),
                    ApprovedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsLeaveDeducted = table.Column<bool>(type: "bit", nullable: false),
                    DeductedFromCurrentYear = table.Column<decimal>(type: "decimal(5,1)", nullable: false),
                    DeductedFromCarryForward = table.Column<decimal>(type: "decimal(5,1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveRequests", x => x.RequestId);
                    table.ForeignKey(
                        name: "FK_LeaveRequests_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "EmployeeId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Requests",
                columns: table => new
                {
                    RequestId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    RequestType = table.Column<int>(type: "int", nullable: false),
                    CheckInOutType = table.Column<int>(type: "int", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LeaveReason = table.Column<int>(type: "int", nullable: true),
                    IsHalfDayStart = table.Column<bool>(type: "bit", nullable: false),
                    IsHalfDayEnd = table.Column<bool>(type: "bit", nullable: false),
                    IsHalfDayStartMorning = table.Column<bool>(type: "bit", nullable: false),
                    IsHalfDayEndMorning = table.Column<bool>(type: "bit", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CountsAsWork = table.Column<bool>(type: "bit", nullable: false),
                    TotalDays = table.Column<decimal>(type: "decimal(5,1)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ApproverId = table.Column<int>(type: "int", nullable: true),
                    ApprovedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Requests", x => x.RequestId);
                    table.ForeignKey(
                        name: "FK_Requests_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "EmployeeId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Tickets",
                columns: table => new
                {
                    TicketId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    Urgency = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Deadline = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    AssignedTo = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ResolvedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Resolution = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tickets", x => x.TicketId);
                    table.ForeignKey(
                        name: "FK_Tickets_Employees_AssignedTo",
                        column: x => x.AssignedTo,
                        principalTable: "Employees",
                        principalColumn: "EmployeeId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Tickets_Employees_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Employees",
                        principalColumn: "EmployeeId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AvatarUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    GoogleId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastLogin = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EmployeeId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_Users_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "EmployeeId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "RequestApprovals",
                columns: table => new
                {
                    RequestApprovalId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestId = table.Column<int>(type: "int", nullable: false),
                    ApproverId = table.Column<int>(type: "int", nullable: false),
                    ApproverOrder = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ApprovedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Comment = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestApprovals", x => x.RequestApprovalId);
                    table.ForeignKey(
                        name: "FK_RequestApprovals_Employees_ApproverId",
                        column: x => x.ApproverId,
                        principalTable: "Employees",
                        principalColumn: "EmployeeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RequestApprovals_Requests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "Requests",
                        principalColumn: "RequestId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RequestAttachments",
                columns: table => new
                {
                    AttachmentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestId = table.Column<int>(type: "int", nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestAttachments", x => x.AttachmentId);
                    table.ForeignKey(
                        name: "FK_RequestAttachments_Requests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "Requests",
                        principalColumn: "RequestId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TicketMessages",
                columns: table => new
                {
                    TicketMessageId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TicketId = table.Column<int>(type: "int", nullable: false),
                    SenderId = table.Column<int>(type: "int", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    MessageType = table.Column<int>(type: "int", nullable: false),
                    ForwardedToId = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketMessages", x => x.TicketMessageId);
                    table.ForeignKey(
                        name: "FK_TicketMessages_Employees_ForwardedToId",
                        column: x => x.ForwardedToId,
                        principalTable: "Employees",
                        principalColumn: "EmployeeId");
                    table.ForeignKey(
                        name: "FK_TicketMessages_Employees_SenderId",
                        column: x => x.SenderId,
                        principalTable: "Employees",
                        principalColumn: "EmployeeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TicketMessages_Tickets_TicketId",
                        column: x => x.TicketId,
                        principalTable: "Tickets",
                        principalColumn: "TicketId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TicketRecipients",
                columns: table => new
                {
                    TicketRecipientId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TicketId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    AddedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReadDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AddedById = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketRecipients", x => x.TicketRecipientId);
                    table.ForeignKey(
                        name: "FK_TicketRecipients_Employees_AddedById",
                        column: x => x.AddedById,
                        principalTable: "Employees",
                        principalColumn: "EmployeeId");
                    table.ForeignKey(
                        name: "FK_TicketRecipients_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "EmployeeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TicketRecipients_Tickets_TicketId",
                        column: x => x.TicketId,
                        principalTable: "Tickets",
                        principalColumn: "TicketId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    NotificationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.NotificationId);
                    table.ForeignKey(
                        name: "FK_Notifications_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    AssignedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserRoles_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "RoleId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRoles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TicketAttachments",
                columns: table => new
                {
                    TicketAttachmentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TicketId = table.Column<int>(type: "int", nullable: true),
                    TicketMessageId = table.Column<int>(type: "int", nullable: true),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UploadedById = table.Column<int>(type: "int", nullable: false),
                    UploadedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketAttachments", x => x.TicketAttachmentId);
                    table.ForeignKey(
                        name: "FK_TicketAttachments_Employees_UploadedById",
                        column: x => x.UploadedById,
                        principalTable: "Employees",
                        principalColumn: "EmployeeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TicketAttachments_TicketMessages_TicketMessageId",
                        column: x => x.TicketMessageId,
                        principalTable: "TicketMessages",
                        principalColumn: "TicketMessageId");
                    table.ForeignKey(
                        name: "FK_TicketAttachments_Tickets_TicketId",
                        column: x => x.TicketId,
                        principalTable: "Tickets",
                        principalColumn: "TicketId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "JobTitles",
                columns: new[] { "JobTitleId", "Description", "IsActive", "JobTitleName", "SortOrder" },
                values: new object[,]
                {
                    { 1, null, true, "Ban Giám Đốc", 1 },
                    { 2, null, true, "Trưởng phòng", 2 },
                    { 3, null, true, "Manager", 3 },
                    { 4, null, true, "Nhân viên", 4 },
                    { 5, null, true, "Thực tập", 5 }
                });

            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "PermissionId", "Category", "Description", "IsActive", "PermissionKey", "PermissionName", "SortOrder" },
                values: new object[,]
                {
                    { 1, "Employee", "Xem thông tin nhân viên", true, "Employee.View", "Xem danh sách nhân viên", 1 },
                    { 2, "Employee", "Thêm nhân viên mới vào hệ thống", true, "Employee.Create", "Tạo nhân viên mới", 2 },
                    { 3, "Employee", "Chỉnh sửa thông tin nhân viên", true, "Employee.Edit", "Sửa thông tin nhân viên", 3 },
                    { 4, "Employee", "Xóa nhân viên khỏi hệ thống", true, "Employee.Delete", "Xóa nhân viên", 4 },
                    { 5, "Employee", "Xem thông tin lương và hợp đồng nhân viên", true, "Employee.ViewSalary", "Xem lương nhân viên", 5 },
                    { 6, "Request", "Xem danh sách đơn từ", true, "Request.View", "Xem đơn từ", 1 },
                    { 7, "Request", "Tạo đơn từ mới", true, "Request.Create", "Tạo đơn từ", 2 },
                    { 8, "Request", "Duyệt hoặc từ chối đơn từ", true, "Request.Approve", "Duyệt đơn từ", 3 },
                    { 9, "Request", "Xóa đơn từ khỏi hệ thống", true, "Request.Delete", "Xóa đơn từ", 4 },
                    { 10, "Attendance", "Xem dữ liệu chấm công", true, "Attendance.View", "Xem chấm công", 1 },
                    { 11, "Attendance", "Chỉnh sửa dữ liệu chấm công", true, "Attendance.Edit", "Sửa chấm công", 2 },
                    { 12, "Attendance", "Xuất file báo cáo chấm công", true, "Attendance.Export", "Xuất báo cáo chấm công", 3 },
                    { 13, "Asset", "Xem danh sách tài sản", true, "Asset.View", "Xem tài sản", 1 },
                    { 14, "Asset", "Thêm tài sản mới", true, "Asset.Create", "Tạo tài sản", 2 },
                    { 15, "Asset", "Chỉnh sửa thông tin tài sản", true, "Asset.Edit", "Sửa tài sản", 3 },
                    { 16, "Asset", "Xóa tài sản khỏi hệ thống", true, "Asset.Delete", "Xóa tài sản", 4 },
                    { 17, "Asset", "Gán tài sản cho nhân viên", true, "Asset.Assign", "Gán tài sản", 5 },
                    { 18, "Settings", "Xem phòng ban, chức vụ, vị trí", true, "Settings.ViewDepartments", "Xem cài đặt danh mục", 1 },
                    { 19, "Settings", "Thêm, sửa, xóa phòng ban và danh mục", true, "Settings.ManageDepartments", "Quản lý danh mục", 2 },
                    { 20, "Settings", "Xem phân quyền hệ thống", true, "Settings.ViewPermissions", "Xem phân quyền", 3 },
                    { 21, "Settings", "Thêm, sửa, xóa quyền và phân quyền cho vai trò", true, "Settings.ManagePermissions", "Quản lý phân quyền", 4 },
                    { 22, "System", "Xem nhật ký hệ thống", true, "System.ViewLogs", "Xem system logs", 1 },
                    { 23, "System", "Quản lý tài khoản người dùng", true, "System.ManageUsers", "Quản lý người dùng", 2 },
                    { 24, "System", "Xem các báo cáo hệ thống", true, "System.ViewReports", "Xem báo cáo", 3 },
                    { 25, "System", "Quyền cao nhất - Được làm mọi thứ trong hệ thống", true, "System.ALL", "⭐ TOÀN QUYỀN HỆ THỐNG", 99 }
                });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "RoleId", "Description", "RoleName" },
                values: new object[,]
                {
                    { 1, "System administrator", "Admin" },
                    { 2, "Department manager", "Manager" },
                    { 3, "Regular employee", "User" }
                });

            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "RolePermissionId", "AssignedDate", "PermissionId", "RoleId" },
                values: new object[,]
                {
                    { 1, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, 1 },
                    { 2, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 2, 1 },
                    { 3, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 3, 1 },
                    { 4, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 4, 1 },
                    { 5, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 5, 1 },
                    { 6, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 6, 1 },
                    { 7, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 7, 1 },
                    { 8, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 8, 1 },
                    { 9, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 9, 1 },
                    { 10, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 10, 1 },
                    { 11, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 11, 1 },
                    { 12, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 12, 1 },
                    { 13, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 13, 1 },
                    { 14, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 14, 1 },
                    { 15, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 15, 1 },
                    { 16, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 16, 1 },
                    { 17, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 17, 1 },
                    { 18, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 18, 1 },
                    { 19, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 19, 1 },
                    { 20, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 20, 1 },
                    { 21, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 21, 1 },
                    { 22, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 22, 1 },
                    { 23, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 23, 1 },
                    { 24, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 24, 1 },
                    { 25, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, 2 },
                    { 26, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 2, 2 },
                    { 27, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 3, 2 },
                    { 28, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 6, 2 },
                    { 29, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 7, 2 },
                    { 30, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 8, 2 },
                    { 31, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 10, 2 },
                    { 32, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 11, 2 },
                    { 33, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 12, 2 },
                    { 34, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 13, 2 },
                    { 35, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 6, 3 },
                    { 36, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 7, 3 },
                    { 37, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 10, 3 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssetAssignments_ApprovedById",
                table: "AssetAssignments",
                column: "ApprovedById");

            migrationBuilder.CreateIndex(
                name: "IX_AssetAssignments_AssetId",
                table: "AssetAssignments",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetAssignments_EmployeeId",
                table: "AssetAssignments",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetLifecycleHistories_AssetId",
                table: "AssetLifecycleHistories",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetLifecycleHistories_EmployeeId",
                table: "AssetLifecycleHistories",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetLifecycleHistories_PerformedById",
                table: "AssetLifecycleHistories",
                column: "PerformedById");

            migrationBuilder.CreateIndex(
                name: "IX_Assets_AssetCategoryId",
                table: "Assets",
                column: "AssetCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Assets_AssetCode",
                table: "Assets",
                column: "AssetCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Assets_BrandId",
                table: "Assets",
                column: "BrandId");

            migrationBuilder.CreateIndex(
                name: "IX_Assets_SupplierId",
                table: "Assets",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_EmployeeId_Date",
                table: "Attendances",
                columns: new[] { "EmployeeId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_EmployeeId",
                table: "Contracts",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_ManagerId",
                table: "Departments",
                column: "ManagerId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeContacts_EmployeeId",
                table: "EmployeeContacts",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_AttendanceCode",
                table: "Employees",
                column: "AttendanceCode",
                unique: true,
                filter: "[AttendanceCode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_DepartmentId",
                table: "Employees",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_Email",
                table: "Employees",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Employees_EmployeeCode",
                table: "Employees",
                column: "EmployeeCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Employees_JobTitleId",
                table: "Employees",
                column: "JobTitleId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_PositionId",
                table: "Employees",
                column: "PositionId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveBalances_EmployeeId_Year",
                table: "LeaveBalances",
                columns: new[] { "EmployeeId", "Year" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_EmployeeId",
                table: "LeaveRequests",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_CreatedDate",
                table: "Notifications",
                columns: new[] { "UserId", "CreatedDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_IsRead_CreatedDate",
                table: "Notifications",
                columns: new[] { "UserId", "IsRead", "CreatedDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_PermissionKey",
                table: "Permissions",
                column: "PermissionKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RequestApprovals_ApproverId",
                table: "RequestApprovals",
                column: "ApproverId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestApprovals_RequestId_ApproverOrder",
                table: "RequestApprovals",
                columns: new[] { "RequestId", "ApproverOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RequestAttachments_RequestId",
                table: "RequestAttachments",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_Requests_EmployeeId",
                table: "Requests",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_PermissionId",
                table: "RolePermissions",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_RoleId_PermissionId",
                table: "RolePermissions",
                columns: new[] { "RoleId", "PermissionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SystemLogs_CreatedDate",
                table: "SystemLogs",
                column: "CreatedDate");

            migrationBuilder.CreateIndex(
                name: "IX_TicketAttachments_TicketId",
                table: "TicketAttachments",
                column: "TicketId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketAttachments_TicketMessageId",
                table: "TicketAttachments",
                column: "TicketMessageId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketAttachments_UploadedById",
                table: "TicketAttachments",
                column: "UploadedById");

            migrationBuilder.CreateIndex(
                name: "IX_TicketMessages_ForwardedToId",
                table: "TicketMessages",
                column: "ForwardedToId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketMessages_SenderId",
                table: "TicketMessages",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketMessages_TicketId",
                table: "TicketMessages",
                column: "TicketId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketRecipients_AddedById",
                table: "TicketRecipients",
                column: "AddedById");

            migrationBuilder.CreateIndex(
                name: "IX_TicketRecipients_EmployeeId",
                table: "TicketRecipients",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketRecipients_TicketId_EmployeeId",
                table: "TicketRecipients",
                columns: new[] { "TicketId", "EmployeeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_AssignedTo",
                table: "Tickets",
                column: "AssignedTo");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_CreatedBy",
                table: "Tickets",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_RoleId",
                table: "UserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_UserId_RoleId",
                table: "UserRoles",
                columns: new[] { "UserId", "RoleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_EmployeeId",
                table: "Users",
                column: "EmployeeId",
                unique: true,
                filter: "[EmployeeId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Users_GoogleId",
                table: "Users",
                column: "GoogleId",
                unique: true,
                filter: "[GoogleId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_AssetAssignments_Employees_ApprovedById",
                table: "AssetAssignments",
                column: "ApprovedById",
                principalTable: "Employees",
                principalColumn: "EmployeeId",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_AssetAssignments_Employees_EmployeeId",
                table: "AssetAssignments",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "EmployeeId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AssetLifecycleHistories_Employees_EmployeeId",
                table: "AssetLifecycleHistories",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "EmployeeId");

            migrationBuilder.AddForeignKey(
                name: "FK_AssetLifecycleHistories_Employees_PerformedById",
                table: "AssetLifecycleHistories",
                column: "PerformedById",
                principalTable: "Employees",
                principalColumn: "EmployeeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Attendances_Employees_EmployeeId",
                table: "Attendances",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "EmployeeId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Contracts_Employees_EmployeeId",
                table: "Contracts",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "EmployeeId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Departments_Employees_ManagerId",
                table: "Departments",
                column: "ManagerId",
                principalTable: "Employees",
                principalColumn: "EmployeeId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Departments_Employees_ManagerId",
                table: "Departments");

            migrationBuilder.DropTable(
                name: "AssetAssignments");

            migrationBuilder.DropTable(
                name: "AssetConfigurations");

            migrationBuilder.DropTable(
                name: "AssetLifecycleHistories");

            migrationBuilder.DropTable(
                name: "Attendances");

            migrationBuilder.DropTable(
                name: "Contracts");

            migrationBuilder.DropTable(
                name: "EmployeeContacts");

            migrationBuilder.DropTable(
                name: "LeaveBalances");

            migrationBuilder.DropTable(
                name: "LeaveRequests");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "RequestApprovals");

            migrationBuilder.DropTable(
                name: "RequestAttachments");

            migrationBuilder.DropTable(
                name: "RolePermissions");

            migrationBuilder.DropTable(
                name: "SystemLogs");

            migrationBuilder.DropTable(
                name: "TicketAttachments");

            migrationBuilder.DropTable(
                name: "TicketRecipients");

            migrationBuilder.DropTable(
                name: "UserRoles");

            migrationBuilder.DropTable(
                name: "Assets");

            migrationBuilder.DropTable(
                name: "Requests");

            migrationBuilder.DropTable(
                name: "Permissions");

            migrationBuilder.DropTable(
                name: "TicketMessages");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "AssetCategories");

            migrationBuilder.DropTable(
                name: "Brands");

            migrationBuilder.DropTable(
                name: "Suppliers");

            migrationBuilder.DropTable(
                name: "Tickets");

            migrationBuilder.DropTable(
                name: "Employees");

            migrationBuilder.DropTable(
                name: "Departments");

            migrationBuilder.DropTable(
                name: "JobTitles");

            migrationBuilder.DropTable(
                name: "Positions");
        }
    }
}
