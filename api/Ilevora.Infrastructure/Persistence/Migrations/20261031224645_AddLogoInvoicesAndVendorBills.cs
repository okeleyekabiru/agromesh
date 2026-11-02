using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ilevora.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLogoInvoicesAndVendorBills : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "Farms",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BankAccountName",
                table: "Farms",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BankAccountNumber",
                table: "Farms",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BankName",
                table: "Farms",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Farms",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LogoFileId",
                table: "Farms",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LogoUpdatedAt",
                table: "Farms",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "Farms",
                type: "character varying(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Website",
                table: "Farms",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Invoices",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    FarmId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Number = table.Column<int>(type: "integer", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    BillToName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    BillToPhone = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    BillToEmail = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    BillToAddress = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    LineItems = table.Column<string>(type: "jsonb", nullable: false),
                    VatPercent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    PaymentAccountName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    PaymentAccountNumber = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    PaymentBankName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TermsAndNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    SignatoryName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    TemplateKey = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedByUserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Invoices_Farms_FarmId",
                        column: x => x.FarmId,
                        principalTable: "Farms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StoredFiles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    FarmId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FileName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Size = table.Column<long>(type: "bigint", nullable: false),
                    Content = table.Column<byte[]>(type: "bytea", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoredFiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VendorInvoices",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    FarmId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    OrderId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SupplierId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FileId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FileName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    VendorName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    InvoiceNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    InvoiceDate = table.Column<DateOnly>(type: "date", nullable: false),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    PaidAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    UploadedByUserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VendorInvoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VendorInvoices_Farms_FarmId",
                        column: x => x.FarmId,
                        principalTable: "Farms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VendorInvoices_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VendorInvoices_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_FarmId_Number",
                table: "Invoices",
                columns: new[] { "FarmId", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StoredFiles_FarmId",
                table: "StoredFiles",
                column: "FarmId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorInvoices_FarmId_Status",
                table: "VendorInvoices",
                columns: new[] { "FarmId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_VendorInvoices_FarmId_SupplierId_InvoiceNumber",
                table: "VendorInvoices",
                columns: new[] { "FarmId", "SupplierId", "InvoiceNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_VendorInvoices_OrderId",
                table: "VendorInvoices",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorInvoices_SupplierId",
                table: "VendorInvoices",
                column: "SupplierId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Invoices");

            migrationBuilder.DropTable(
                name: "StoredFiles");

            migrationBuilder.DropTable(
                name: "VendorInvoices");

            migrationBuilder.DropColumn(
                name: "Address",
                table: "Farms");

            migrationBuilder.DropColumn(
                name: "BankAccountName",
                table: "Farms");

            migrationBuilder.DropColumn(
                name: "BankAccountNumber",
                table: "Farms");

            migrationBuilder.DropColumn(
                name: "BankName",
                table: "Farms");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Farms");

            migrationBuilder.DropColumn(
                name: "LogoFileId",
                table: "Farms");

            migrationBuilder.DropColumn(
                name: "LogoUpdatedAt",
                table: "Farms");

            migrationBuilder.DropColumn(
                name: "Phone",
                table: "Farms");

            migrationBuilder.DropColumn(
                name: "Website",
                table: "Farms");
        }
    }
}
