using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace managerCMN.Migrations
{
    /// <inheritdoc />
    public partial class ConfigureFullAttendanceEmployee : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FullAttendanceEmployees_EmployeeId",
                table: "FullAttendanceEmployees");

            migrationBuilder.CreateIndex(
                name: "IX_FullAttendanceEmployees_EmployeeId",
                table: "FullAttendanceEmployees",
                column: "EmployeeId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FullAttendanceEmployees_EmployeeId",
                table: "FullAttendanceEmployees");

            migrationBuilder.CreateIndex(
                name: "IX_FullAttendanceEmployees_EmployeeId",
                table: "FullAttendanceEmployees",
                column: "EmployeeId");
        }
    }
}
