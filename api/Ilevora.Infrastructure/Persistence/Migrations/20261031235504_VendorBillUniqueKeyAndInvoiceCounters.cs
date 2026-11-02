using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ilevora.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class VendorBillUniqueKeyAndInvoiceCounters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VendorInvoices_FarmId_SupplierId_InvoiceNumber",
                table: "VendorInvoices");

            migrationBuilder.AddColumn<string>(
                name: "InvoiceNumberKey",
                table: "VendorInvoices",
                type: "character varying(140)",
                maxLength: 140,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "InvoiceCounters",
                columns: table => new
                {
                    FarmId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    LastNumber = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceCounters", x => x.FarmId);
                    table.ForeignKey(
                        name: "FK_InvoiceCounters_Farms_FarmId",
                        column: x => x.FarmId,
                        principalTable: "Farms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Backfill the duplicate-detection key for rows that already exist (trimmed, upper-cased: the same rule as
            // VendorInvoice.NormaliseNumber). Bills that were uploaded twice before this migration cannot both own the key, so
            // the oldest keeps it and each later copy gets "#DUP-<n>" appended to its internal key only. No row is deleted and
            // InvoiceNumber (what people see) is unchanged; new uploads of that number are refused as duplicates from now on.
            migrationBuilder.Sql("""
                UPDATE "VendorInvoices" SET "InvoiceNumberKey" = UPPER(BTRIM("InvoiceNumber"));
                """);
            migrationBuilder.Sql("""
                UPDATE "VendorInvoices" v
                SET "InvoiceNumberKey" = v."InvoiceNumberKey" || '#DUP-' || d.rn
                FROM (
                    SELECT "Id", ROW_NUMBER() OVER (PARTITION BY "FarmId", "SupplierId", "InvoiceNumberKey" ORDER BY "CreatedAt", "Id") AS rn
                    FROM "VendorInvoices"
                ) d
                WHERE v."Id" = d."Id" AND d.rn > 1;
                """);

            // Seed each farm's counter with its highest existing number so the next invoice continues after it.
            migrationBuilder.Sql("""
                INSERT INTO "InvoiceCounters" ("FarmId", "LastNumber")
                SELECT "FarmId", MAX("Number") FROM "Invoices" GROUP BY "FarmId";
                """);

            migrationBuilder.CreateIndex(
                name: "IX_VendorInvoices_FarmId_SupplierId_InvoiceNumberKey",
                table: "VendorInvoices",
                columns: new[] { "FarmId", "SupplierId", "InvoiceNumberKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InvoiceCounters");

            migrationBuilder.DropIndex(
                name: "IX_VendorInvoices_FarmId_SupplierId_InvoiceNumberKey",
                table: "VendorInvoices");

            migrationBuilder.DropColumn(
                name: "InvoiceNumberKey",
                table: "VendorInvoices");

            migrationBuilder.CreateIndex(
                name: "IX_VendorInvoices_FarmId_SupplierId_InvoiceNumber",
                table: "VendorInvoices",
                columns: new[] { "FarmId", "SupplierId", "InvoiceNumber" });
        }
    }
}
