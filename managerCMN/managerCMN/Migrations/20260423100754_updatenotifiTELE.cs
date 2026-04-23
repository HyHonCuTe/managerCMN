using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace managerCMN.Migrations
{
    /// <inheritdoc />
    public partial class updatenotifiTELE : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ScheduledAnnouncements_Departments_FilterDepartmentId",
                table: "ScheduledAnnouncements");

            migrationBuilder.AddForeignKey(
                name: "FK_ScheduledAnnouncements_Departments_FilterDepartmentId",
                table: "ScheduledAnnouncements",
                column: "FilterDepartmentId",
                principalTable: "Departments",
                principalColumn: "DepartmentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ScheduledAnnouncements_Departments_FilterDepartmentId",
                table: "ScheduledAnnouncements");

            migrationBuilder.AddForeignKey(
                name: "FK_ScheduledAnnouncements_Departments_FilterDepartmentId",
                table: "ScheduledAnnouncements",
                column: "FilterDepartmentId",
                principalTable: "Departments",
                principalColumn: "DepartmentId",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
