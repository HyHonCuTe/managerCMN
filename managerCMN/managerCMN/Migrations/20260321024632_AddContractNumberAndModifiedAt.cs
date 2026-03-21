using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace managerCMN.Migrations
{
    /// <inheritdoc />
    public partial class AddContractNumberAndModifiedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Add ContractNumber as NULLABLE first
            migrationBuilder.AddColumn<string>(
                name: "ContractNumber",
                table: "Contracts",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            // Step 2: Backfill existing contracts with LEGACY-{ContractId}-{Year}
            migrationBuilder.Sql(@"
                UPDATE Contracts
                SET ContractNumber = CONCAT('LEGACY-', ContractId, '-', YEAR(StartDate))
                WHERE ContractNumber IS NULL OR ContractNumber = ''
            ");

            // Step 3: Make ContractNumber NOT NULL
            migrationBuilder.AlterColumn<string>(
                name: "ContractNumber",
                table: "Contracts",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            // Step 4: Add ModifiedAt column
            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedAt",
                table: "Contracts",
                type: "datetime2",
                nullable: true);

            // Step 5: Add unique index
            migrationBuilder.CreateIndex(
                name: "IX_Contracts_ContractNumber",
                table: "Contracts",
                column: "ContractNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Contracts_ContractNumber",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "ContractNumber",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "ModifiedAt",
                table: "Contracts");
        }
    }
}
