using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using managerCMN.Data;

#nullable disable

namespace managerCMN.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260422090000_EnsureTelegramMuteBroadcast")]
    public partial class EnsureTelegramMuteBroadcast : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM sys.columns
                    WHERE object_id = OBJECT_ID(N'Users')
                      AND name = N'TelegramMuteBroadcast'
                )
                BEGIN
                    ALTER TABLE Users ADD TelegramMuteBroadcast BIT NOT NULL DEFAULT 0;
                END
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1 FROM sys.columns
                    WHERE object_id = OBJECT_ID(N'Users')
                      AND name = N'TelegramMuteBroadcast'
                )
                BEGIN
                    ALTER TABLE Users DROP COLUMN TelegramMuteBroadcast;
                END
            ");
        }
    }
}
