using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgroMesh.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStockMovementSectionAndPerformedBy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BlockId",
                table: "StockMovements",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PerformedByUserId",
                table: "StockMovements",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SectionId",
                table: "StockMovements",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_SectionId",
                table: "StockMovements",
                column: "SectionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StockMovements_SectionId",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "BlockId",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "PerformedByUserId",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "SectionId",
                table: "StockMovements");
        }
    }
}
