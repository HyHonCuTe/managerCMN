using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace managerCMN.Migrations
{
    /// <inheritdoc />
    public partial class AddSubTaskToProjectTemplate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ParentTemplateTaskId",
                table: "ProjectTemplateTasks",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTemplateTasks_ParentTemplateTaskId",
                table: "ProjectTemplateTasks",
                column: "ParentTemplateTaskId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectTemplateTasks_ProjectTemplateTasks_ParentTemplateTaskId",
                table: "ProjectTemplateTasks",
                column: "ParentTemplateTaskId",
                principalTable: "ProjectTemplateTasks",
                principalColumn: "ProjectTemplateTaskId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectTemplateTasks_ProjectTemplateTasks_ParentTemplateTaskId",
                table: "ProjectTemplateTasks");

            migrationBuilder.DropIndex(
                name: "IX_ProjectTemplateTasks_ParentTemplateTaskId",
                table: "ProjectTemplateTasks");

            migrationBuilder.DropColumn(
                name: "ParentTemplateTaskId",
                table: "ProjectTemplateTasks");
        }
    }
}
