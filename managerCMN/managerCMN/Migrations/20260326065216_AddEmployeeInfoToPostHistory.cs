using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace managerCMN.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeeInfoToPostHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EmployeeInfo",
                table: "PostHistories",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmployeeInfo",
                table: "PostHistories");
        }
    }
}
