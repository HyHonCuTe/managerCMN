using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace managerCMN.Migrations
{
    /// <inheritdoc />
    public partial class AddIsManuallyAdjustedToLeaveBalance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsManuallyAdjusted",
                table: "LeaveBalances",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsManuallyAdjusted",
                table: "LeaveBalances");
        }
    }
}
