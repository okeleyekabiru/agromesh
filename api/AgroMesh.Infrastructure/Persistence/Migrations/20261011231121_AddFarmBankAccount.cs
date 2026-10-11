using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgroMesh.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFarmBankAccount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BankAccountId",
                table: "Farms",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "BankLinkedAt",
                table: "Farms",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastBankSyncAt",
                table: "Farms",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Farms_BankAccountId",
                table: "Farms",
                column: "BankAccountId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Farms_BankAccountId",
                table: "Farms");

            migrationBuilder.DropColumn(
                name: "BankAccountId",
                table: "Farms");

            migrationBuilder.DropColumn(
                name: "BankLinkedAt",
                table: "Farms");

            migrationBuilder.DropColumn(
                name: "LastBankSyncAt",
                table: "Farms");
        }
    }
}
