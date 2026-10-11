using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgroMesh.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderPaymentAndTransactionKind : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Kind",
                table: "Transactions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "OrderId",
                table: "Transactions",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Reconciliation",
                table: "Transactions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ReconciliationNote",
                table: "Transactions",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Number",
                table: "Orders",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "PaidAt",
                table: "Orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentTransactionId",
                table: "Orders",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            // Orders that already exist get their numbers (ORD-1001, ORD-1002, ...) in the order they were created.
            migrationBuilder.Sql(@"
                UPDATE ""Orders"" o SET ""Number"" = n.rn + 1000
                FROM (SELECT ""Id"", ROW_NUMBER() OVER (PARTITION BY ""FarmId"" ORDER BY ""CreatedAt"", ""Id"") AS rn FROM ""Orders"") n
                WHERE o.""Id"" = n.""Id"";");

            // Money already in the books: credits are income, unless the narration says capital or investment.
            migrationBuilder.Sql(@"
                UPDATE ""Transactions""
                SET ""Kind"" = CASE WHEN ""Narration"" ~* '\m(capital|investment)\M' THEN 2 ELSE 1 END
                WHERE ""Amount"" >= 0;");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_FarmId_Reconciliation",
                table: "Transactions",
                columns: new[] { "FarmId", "Reconciliation" });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_FarmId_Number",
                table: "Orders",
                columns: new[] { "FarmId", "Number" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Transactions_FarmId_Reconciliation",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Orders_FarmId_Number",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "Kind",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "OrderId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "Reconciliation",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "ReconciliationNote",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "Number",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PaidAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PaymentTransactionId",
                table: "Orders");
        }
    }
}
