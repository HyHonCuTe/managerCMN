using managerCMN.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace managerCMN.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260424041000_AddApprover1Settings")]
    public partial class AddApprover1Settings : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsApprover1",
                table: "Employees",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql(@"
                UPDATE Employees
                SET IsApprover1 = 1
                WHERE JobTitleId = 2
                  AND Status = 0;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsApprover1",
                table: "Employees");
        }
    }
}
