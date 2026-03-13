using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace managerCMN.Migrations
{
    /// <inheritdoc />
    public partial class RefineQuarterlyLeavePolicy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DeductedFromCurrentYear",
                table: "LeaveRequests",
                type: "decimal(5,1)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DeductedFromCarryForward",
                table: "LeaveRequests",
                type: "decimal(5,1)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeductedFromCurrentYear",
                table: "LeaveRequests");

            migrationBuilder.DropColumn(
                name: "DeductedFromCarryForward",
                table: "LeaveRequests");
        }
    }
}
