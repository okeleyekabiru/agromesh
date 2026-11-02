using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ilevora.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InvoiceBrandColours : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AccentColor",
                table: "Invoices",
                type: "character varying(7)",
                maxLength: 7,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PrimaryColor",
                table: "Invoices",
                type: "character varying(7)",
                maxLength: 7,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BrandAccentColor",
                table: "Farms",
                type: "character varying(7)",
                maxLength: 7,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BrandPrimaryColor",
                table: "Farms",
                type: "character varying(7)",
                maxLength: 7,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccentColor",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "PrimaryColor",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "BrandAccentColor",
                table: "Farms");

            migrationBuilder.DropColumn(
                name: "BrandPrimaryColor",
                table: "Farms");
        }
    }
}
