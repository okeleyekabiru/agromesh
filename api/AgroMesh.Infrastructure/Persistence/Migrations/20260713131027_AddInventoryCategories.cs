using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgroMesh.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Create the new InventoryCategories table first so we can seed + backfill before
            //    touching InventoryItems at all.
            migrationBuilder.CreateTable(
                name: "InventoryCategories",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    FarmId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryCategories_Farms_FarmId",
                        column: x => x.FarmId,
                        principalTable: "Farms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCategories_FarmId",
                table: "InventoryCategories",
                column: "FarmId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCategories_FarmId_Name",
                table: "InventoryCategories",
                columns: new[] { "FarmId", "Name" });

            // 2. Seed the 8 default categories (see spec.md Requirement 3) for every farm that
            //    exists today. Ids are deterministic (farmId + slug) rather than DB-generated
            //    UUIDs, since the Id column is plain `text` (app-generated Guid strings) and this
            //    avoids depending on a Postgres UUID extension being installed.
            migrationBuilder.Sql(@"
                INSERT INTO ""InventoryCategories"" (""Id"", ""FarmId"", ""Name"", ""IsDefault"", ""CreatedAt"", ""UpdatedAt"")
                SELECT f.""Id"" || '-cat-' || cat.slug, f.""Id"", cat.name, true, now(), now()
                FROM ""Farms"" f
                CROSS JOIN (VALUES
                    ('Chemical', 'chemical'),
                    ('Fertilizer', 'fertilizer'),
                    ('Farm Tools', 'farm-tools'),
                    ('Seed', 'seed'),
                    ('Fuel', 'fuel'),
                    ('Equipment', 'equipment'),
                    ('Spare Parts', 'spare-parts'),
                    ('Other', 'other')
                ) AS cat(name, slug);
            ");

            // 3. Add the new FK column nullable first — existing InventoryItems rows need a
            //    backfilled value before it can become NOT NULL.
            migrationBuilder.AddColumn<string>(
                name: "CategoryId",
                table: "InventoryItems",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            // 4. Backfill CategoryId from the old Category enum ordinal, matched to the category
            //    just seeded for that item's own farm. See spec.md Requirement 4 for the mapping
            //    table — SpareParts/Other (ordinals 5/6) map onto the extra default categories
            //    seeded above so no existing item loses its category.
            migrationBuilder.Sql(@"
                UPDATE ""InventoryItems"" ii
                SET ""CategoryId"" = ii.""FarmId"" || '-cat-' || CASE ii.""Category""
                    WHEN 0 THEN 'chemical'
                    WHEN 1 THEN 'fertilizer'
                    WHEN 2 THEN 'seed'
                    WHEN 3 THEN 'fuel'
                    WHEN 4 THEN 'equipment'
                    WHEN 5 THEN 'spare-parts'
                    WHEN 6 THEN 'other'
                END;
            ");

            // 5. Now safe to require it.
            migrationBuilder.AlterColumn<string>(
                name: "CategoryId",
                table: "InventoryItems",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            // 6. Drop the old enum column + its index.
            migrationBuilder.DropIndex(
                name: "IX_InventoryItems_FarmId_Category",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "InventoryItems");

            // 7. New indexes + FK.
            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_CategoryId",
                table: "InventoryItems",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_FarmId_CategoryId",
                table: "InventoryItems",
                columns: new[] { "FarmId", "CategoryId" });

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryItems_InventoryCategories_CategoryId",
                table: "InventoryItems",
                column: "CategoryId",
                principalTable: "InventoryCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InventoryItems_InventoryCategories_CategoryId",
                table: "InventoryItems");

            migrationBuilder.DropIndex(
                name: "IX_InventoryItems_CategoryId",
                table: "InventoryItems");

            migrationBuilder.DropIndex(
                name: "IX_InventoryItems_FarmId_CategoryId",
                table: "InventoryItems");

            // Re-add the old enum column nullable first so we can reverse-map before requiring it.
            migrationBuilder.AddColumn<int>(
                name: "Category",
                table: "InventoryItems",
                type: "integer",
                nullable: true);

            // Best-effort reverse mapping via the deterministic seed-id suffix. Any category with
            // no enum counterpart (Farm Tools, or a farm-created custom category) has no historical
            // ordinal to go back to, so it falls back to Other (6) — acceptable for a down-migration
            // edge case that's not expected to run in production.
            migrationBuilder.Sql(@"
                UPDATE ""InventoryItems"" ii
                SET ""Category"" = CASE
                    WHEN ii.""CategoryId"" LIKE '%-cat-chemical' THEN 0
                    WHEN ii.""CategoryId"" LIKE '%-cat-fertilizer' THEN 1
                    WHEN ii.""CategoryId"" LIKE '%-cat-seed' THEN 2
                    WHEN ii.""CategoryId"" LIKE '%-cat-fuel' THEN 3
                    WHEN ii.""CategoryId"" LIKE '%-cat-equipment' THEN 4
                    WHEN ii.""CategoryId"" LIKE '%-cat-spare-parts' THEN 5
                    ELSE 6
                END;
            ");

            migrationBuilder.AlterColumn<int>(
                name: "Category",
                table: "InventoryItems",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "InventoryItems");

            migrationBuilder.DropTable(
                name: "InventoryCategories");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_FarmId_Category",
                table: "InventoryItems",
                columns: new[] { "FarmId", "Category" });
        }
    }
}
