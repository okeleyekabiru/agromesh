using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Ilevora.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCrops : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Crops",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    FarmId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsBuiltIn = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Crops", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Crops",
                columns: new[] { "Id", "CreatedAt", "FarmId", "IsBuiltIn", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { "Banana", new DateTime(2026, 11, 9, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "Banana", new DateTime(2026, 11, 9, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { "Cassava", new DateTime(2026, 11, 9, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "Cassava", new DateTime(2026, 11, 9, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { "Cocoa", new DateTime(2026, 11, 9, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "Cocoa", new DateTime(2026, 11, 9, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { "Maize", new DateTime(2026, 11, 9, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "Maize", new DateTime(2026, 11, 9, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { "OilPalm", new DateTime(2026, 11, 9, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "Oil palm", new DateTime(2026, 11, 9, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { "Plantain", new DateTime(2026, 11, 9, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "Plantain", new DateTime(2026, 11, 9, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Crops_FarmId",
                table: "Crops",
                column: "FarmId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Crops");
        }
    }
}
