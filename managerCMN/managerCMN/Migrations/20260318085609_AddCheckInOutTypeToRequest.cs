using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace managerCMN.Migrations
{
    /// <inheritdoc />
    public partial class AddCheckInOutTypeToRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CheckInOutType",
                table: "Requests",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CheckInOutType",
                table: "Requests");
        }
    }
}
