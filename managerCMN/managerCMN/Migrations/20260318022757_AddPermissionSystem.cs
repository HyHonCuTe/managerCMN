using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace managerCMN.Migrations
{
    /// <inheritdoc />
    public partial class AddPermissionSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Notifications_UserId",
                table: "Notifications");

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
                name: "IX_RolePermissions_PermissionId",
                table: "RolePermissions",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_RoleId_PermissionId",
                table: "RolePermissions",
                columns: new[] { "RoleId", "PermissionId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RolePermissions");

            migrationBuilder.DropTable(
                name: "Permissions");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_UserId_CreatedDate",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_UserId_IsRead_CreatedDate",
                table: "Notifications");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId",
                table: "Notifications",
                column: "UserId");
        }
    }
}
