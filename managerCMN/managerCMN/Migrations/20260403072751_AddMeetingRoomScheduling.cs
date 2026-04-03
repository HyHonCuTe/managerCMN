using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace managerCMN.Migrations
{
    /// <inheritdoc />
    public partial class AddMeetingRoomScheduling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MeetingRooms",
                columns: table => new
                {
                    MeetingRoomId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Location = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Capacity = table.Column<int>(type: "int", nullable: true),
                    ColorHex = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeetingRooms", x => x.MeetingRoomId);
                });

            migrationBuilder.CreateTable(
                name: "MeetingRoomBookings",
                columns: table => new
                {
                    MeetingRoomBookingId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MeetingRoomId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeetingRoomBookings", x => x.MeetingRoomBookingId);
                    table.ForeignKey(
                        name: "FK_MeetingRoomBookings_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "EmployeeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MeetingRoomBookings_MeetingRooms_MeetingRoomId",
                        column: x => x.MeetingRoomId,
                        principalTable: "MeetingRooms",
                        principalColumn: "MeetingRoomId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MeetingRoomBookings_EmployeeId_StartTime",
                table: "MeetingRoomBookings",
                columns: new[] { "EmployeeId", "StartTime" });

            migrationBuilder.CreateIndex(
                name: "IX_MeetingRoomBookings_MeetingRoomId_StartTime_EndTime",
                table: "MeetingRoomBookings",
                columns: new[] { "MeetingRoomId", "StartTime", "EndTime" });

            migrationBuilder.CreateIndex(
                name: "IX_MeetingRooms_Name",
                table: "MeetingRooms",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MeetingRoomBookings");

            migrationBuilder.DropTable(
                name: "MeetingRooms");
        }
    }
}
