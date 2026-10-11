using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgroMesh.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStockPricing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PackLabel",
                table: "InventoryItems",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "unit");

            migrationBuilder.AddColumn<decimal>(
                name: "PackSize",
                table: "InventoryItems",
                type: "numeric(14,3)",
                nullable: false,
                defaultValue: 1m);

            migrationBuilder.AddColumn<decimal>(
                name: "UnitCost",
                table: "InventoryItems",
                type: "numeric(14,4)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PackLabel",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "PackSize",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "UnitCost",
                table: "InventoryItems");
        }
    }
}
