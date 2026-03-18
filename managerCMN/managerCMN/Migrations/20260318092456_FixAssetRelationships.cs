using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace managerCMN.Migrations
{
    /// <inheritdoc />
    public partial class FixAssetRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ApprovedById",
                table: "AssetAssignments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedDate",
                table: "AssetAssignments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AssignmentCondition",
                table: "AssetAssignments",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AssignmentReason",
                table: "AssetAssignments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ReturnCondition",
                table: "AssetAssignments",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReturnReason",
                table: "AssetAssignments",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AssetLifecycleHistories",
                columns: table => new
                {
                    HistoryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssetId = table.Column<int>(type: "int", nullable: false),
                    EventType = table.Column<int>(type: "int", nullable: false),
                    EventDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: true),
                    PerformedById = table.Column<int>(type: "int", nullable: true),
                    EventDescription = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    PreviousValue = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    NewValue = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetLifecycleHistories", x => x.HistoryId);
                    table.ForeignKey(
                        name: "FK_AssetLifecycleHistories_Assets_AssetId",
                        column: x => x.AssetId,
                        principalTable: "Assets",
                        principalColumn: "AssetId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AssetLifecycleHistories_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "EmployeeId",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_AssetLifecycleHistories_Employees_PerformedById",
                        column: x => x.PerformedById,
                        principalTable: "Employees",
                        principalColumn: "EmployeeId",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssetAssignments_ApprovedById",
                table: "AssetAssignments",
                column: "ApprovedById");

            migrationBuilder.CreateIndex(
                name: "IX_AssetLifecycleHistories_AssetId",
                table: "AssetLifecycleHistories",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetLifecycleHistories_EmployeeId",
                table: "AssetLifecycleHistories",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetLifecycleHistories_PerformedById",
                table: "AssetLifecycleHistories",
                column: "PerformedById");

            migrationBuilder.AddForeignKey(
                name: "FK_AssetAssignments_Employees_ApprovedById",
                table: "AssetAssignments",
                column: "ApprovedById",
                principalTable: "Employees",
                principalColumn: "EmployeeId",
                onDelete: ReferentialAction.NoAction);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AssetAssignments_Employees_ApprovedById",
                table: "AssetAssignments");

            migrationBuilder.DropTable(
                name: "AssetLifecycleHistories");

            migrationBuilder.DropIndex(
                name: "IX_AssetAssignments_ApprovedById",
                table: "AssetAssignments");

            migrationBuilder.DropColumn(
                name: "ApprovedById",
                table: "AssetAssignments");

            migrationBuilder.DropColumn(
                name: "ApprovedDate",
                table: "AssetAssignments");

            migrationBuilder.DropColumn(
                name: "AssignmentCondition",
                table: "AssetAssignments");

            migrationBuilder.DropColumn(
                name: "AssignmentReason",
                table: "AssetAssignments");

            migrationBuilder.DropColumn(
                name: "ReturnCondition",
                table: "AssetAssignments");

            migrationBuilder.DropColumn(
                name: "ReturnReason",
                table: "AssetAssignments");
        }
    }
}
