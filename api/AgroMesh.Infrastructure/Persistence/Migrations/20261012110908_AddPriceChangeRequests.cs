using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgroMesh.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPriceChangeRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PriceChangeRequests",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    FarmId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    InventoryItemId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PackLabel = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    PackSize = table.Column<decimal>(type: "numeric(14,3)", nullable: false),
                    PricePerPack = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    BackfillActivities = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    RequestedByUserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ReviewedByUserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReviewNotes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceChangeRequests", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PriceChangeRequests_FarmId_Status",
                table: "PriceChangeRequests",
                columns: new[] { "FarmId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PriceChangeRequests_InventoryItemId",
                table: "PriceChangeRequests",
                column: "InventoryItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PriceChangeRequests");
        }
    }
}
