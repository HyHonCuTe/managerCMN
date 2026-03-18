using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace managerCMN.Migrations
{
    /// <inheritdoc />
    public partial class FinalAssetLifecycleImplementation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AssetLifecycleHistories_Employees_EmployeeId",
                table: "AssetLifecycleHistories");

            migrationBuilder.AddForeignKey(
                name: "FK_AssetLifecycleHistories_Employees_EmployeeId",
                table: "AssetLifecycleHistories",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "EmployeeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AssetLifecycleHistories_Employees_EmployeeId",
                table: "AssetLifecycleHistories");

            migrationBuilder.AddForeignKey(
                name: "FK_AssetLifecycleHistories_Employees_EmployeeId",
                table: "AssetLifecycleHistories",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "EmployeeId",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
