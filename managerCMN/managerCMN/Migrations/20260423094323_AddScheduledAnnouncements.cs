using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace managerCMN.Migrations
{
    /// <inheritdoc />
    public partial class AddScheduledAnnouncements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ScheduledAnnouncements",
                columns: table => new
                {
                    AnnouncementId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ScheduledAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByEmployeeId = table.Column<int>(type: "int", nullable: false),
                    FilterDepartmentId = table.Column<int>(type: "int", nullable: true),
                    FilterGender = table.Column<int>(type: "int", nullable: true),
                    FilterEmployeeIds = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduledAnnouncements", x => x.AnnouncementId);
                    table.ForeignKey(
                        name: "FK_ScheduledAnnouncements_Departments_FilterDepartmentId",
                        column: x => x.FilterDepartmentId,
                        principalTable: "Departments",
                        principalColumn: "DepartmentId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledAnnouncements_FilterDepartmentId",
                table: "ScheduledAnnouncements",
                column: "FilterDepartmentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ScheduledAnnouncements");
        }
    }
}
