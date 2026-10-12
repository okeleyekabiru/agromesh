using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgroMesh.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSatelliteReadings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastSatelliteSyncAt",
                table: "Farms",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BlockSatelliteReadings",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    FarmId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    BlockId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Ndvi = table.Column<double>(type: "double precision", nullable: false),
                    Ndmi = table.Column<double>(type: "double precision", nullable: false),
                    ValidShare = table.Column<double>(type: "double precision", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlockSatelliteReadings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BlockSatelliteReadings_BlockId_Date",
                table: "BlockSatelliteReadings",
                columns: new[] { "BlockId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BlockSatelliteReadings_FarmId",
                table: "BlockSatelliteReadings",
                column: "FarmId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BlockSatelliteReadings");

            migrationBuilder.DropColumn(
                name: "LastSatelliteSyncAt",
                table: "Farms");
        }
    }
}
