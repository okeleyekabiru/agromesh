using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgroMesh.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTransactionBlock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BlockId",
                table: "Transactions",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BlockId",
                table: "Transactions");
        }
    }
}
