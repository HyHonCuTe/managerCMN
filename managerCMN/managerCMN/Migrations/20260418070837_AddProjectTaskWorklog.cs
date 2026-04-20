using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace managerCMN.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectTaskWorklog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProjectTaskUpdates",
                columns: table => new
                {
                    ProjectTaskUpdateId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectTaskId = table.Column<int>(type: "int", nullable: false),
                    SenderEmployeeId = table.Column<int>(type: "int", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    StatusSnapshot = table.Column<int>(type: "int", nullable: true),
                    ProgressSnapshot = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectTaskUpdates", x => x.ProjectTaskUpdateId);
                    table.ForeignKey(
                        name: "FK_ProjectTaskUpdates_Employees_SenderEmployeeId",
                        column: x => x.SenderEmployeeId,
                        principalTable: "Employees",
                        principalColumn: "EmployeeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectTaskUpdates_ProjectTasks_ProjectTaskId",
                        column: x => x.ProjectTaskId,
                        principalTable: "ProjectTasks",
                        principalColumn: "ProjectTaskId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectTaskAttachments",
                columns: table => new
                {
                    ProjectTaskAttachmentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectTaskUpdateId = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UploadedByEmployeeId = table.Column<int>(type: "int", nullable: false),
                    UploadedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectTaskAttachments", x => x.ProjectTaskAttachmentId);
                    table.ForeignKey(
                        name: "FK_ProjectTaskAttachments_Employees_UploadedByEmployeeId",
                        column: x => x.UploadedByEmployeeId,
                        principalTable: "Employees",
                        principalColumn: "EmployeeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectTaskAttachments_ProjectTaskUpdates_ProjectTaskUpdateId",
                        column: x => x.ProjectTaskUpdateId,
                        principalTable: "ProjectTaskUpdates",
                        principalColumn: "ProjectTaskUpdateId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTaskAttachments_ProjectTaskUpdateId",
                table: "ProjectTaskAttachments",
                column: "ProjectTaskUpdateId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTaskAttachments_UploadedByEmployeeId",
                table: "ProjectTaskAttachments",
                column: "UploadedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTaskUpdates_ProjectTaskId_CreatedDate",
                table: "ProjectTaskUpdates",
                columns: new[] { "ProjectTaskId", "CreatedDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTaskUpdates_SenderEmployeeId",
                table: "ProjectTaskUpdates",
                column: "SenderEmployeeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProjectTaskAttachments");

            migrationBuilder.DropTable(
                name: "ProjectTaskUpdates");
        }
    }
}
