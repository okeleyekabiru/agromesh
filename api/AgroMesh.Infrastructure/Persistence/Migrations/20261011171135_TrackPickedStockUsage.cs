using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgroMesh.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class TrackPickedStockUsage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ReturnedQuantity",
                table: "StockMovements",
                type: "numeric(14,3)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "UsedQuantity",
                table: "StockMovements",
                type: "numeric(14,3)",
                nullable: false,
                defaultValue: 0m);

            // Picks made before usage tracking existed were treated as fully consumed at pick time.
            // Mark them used so they don't all show up as stock still "out on the farm".
            // StockMovementType.Out = 1; a pick is an Out movement with a picker.
            migrationBuilder.Sql(
                @"UPDATE ""StockMovements"" SET ""UsedQuantity"" = ""Quantity"" WHERE ""Type"" = 1 AND ""PerformedByUserId"" IS NOT NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReturnedQuantity",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "UsedQuantity",
                table: "StockMovements");
        }
    }
}
