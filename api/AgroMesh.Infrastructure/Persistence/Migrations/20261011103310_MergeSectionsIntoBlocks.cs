using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgroMesh.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Collapses the Farm → Section → Block hierarchy into Farm → Block. The old sub-section
    /// Blocks table (and the BlockId columns pointing at it) is dropped; the Sections table is
    /// then renamed to Blocks — keeping every existing row, key and index — and every SectionId
    /// column becomes BlockId. Hand-written: EF's scaffold would have dropped Sections outright.
    /// </summary>
    public partial class MergeSectionsIntoBlocks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Remove the old sub-section level.
            migrationBuilder.DropTable(name: "Blocks");

            migrationBuilder.DropIndex(name: "IX_ActivityLogs_BlockId", table: "ActivityLogs");
            migrationBuilder.DropColumn(name: "BlockId", table: "ActivityLogs");
            migrationBuilder.DropColumn(name: "BlockId", table: "StockMovements");

            // 2. Sections → Blocks, preserving data.
            migrationBuilder.RenameTable(name: "Sections", newName: "Blocks");
            migrationBuilder.Sql(@"ALTER TABLE ""Blocks"" RENAME CONSTRAINT ""PK_Sections"" TO ""PK_Blocks"";");
            migrationBuilder.Sql(@"ALTER TABLE ""Blocks"" RENAME CONSTRAINT ""FK_Sections_Farms_FarmId"" TO ""FK_Blocks_Farms_FarmId"";");
            migrationBuilder.RenameIndex(name: "IX_Sections_FarmId", table: "Blocks", newName: "IX_Blocks_FarmId");
            migrationBuilder.RenameIndex(name: "IX_Sections_FarmId_Status", table: "Blocks", newName: "IX_Blocks_FarmId_Status");

            migrationBuilder.AddColumn<int>(
                name: "TreeCount",
                table: "Blocks",
                type: "integer",
                nullable: true);

            // 3. SectionId → BlockId everywhere it's referenced.
            migrationBuilder.RenameColumn(name: "SectionId", table: "ActivityLogs", newName: "BlockId");
            migrationBuilder.RenameIndex(name: "IX_ActivityLogs_SectionId", table: "ActivityLogs", newName: "IX_ActivityLogs_BlockId");

            migrationBuilder.RenameColumn(name: "SectionId", table: "StockMovements", newName: "BlockId");
            migrationBuilder.RenameIndex(name: "IX_StockMovements_SectionId", table: "StockMovements", newName: "IX_StockMovements_BlockId");

            migrationBuilder.RenameColumn(name: "SectionId", table: "SchedulingPredictions", newName: "BlockId");
            migrationBuilder.RenameIndex(
                name: "IX_SchedulingPredictions_SectionId_ActivityType",
                table: "SchedulingPredictions",
                newName: "IX_SchedulingPredictions_BlockId_ActivityType");

            migrationBuilder.RenameColumn(name: "SectionId", table: "CropAllocationRules", newName: "BlockId");

            migrationBuilder.RenameColumn(name: "SectionId", table: "BoundarySubmissions", newName: "BlockId");
            migrationBuilder.RenameIndex(name: "IX_BoundarySubmissions_SectionId", table: "BoundarySubmissions", newName: "IX_BoundarySubmissions_BlockId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 3. BlockId → SectionId.
            migrationBuilder.RenameIndex(name: "IX_BoundarySubmissions_BlockId", table: "BoundarySubmissions", newName: "IX_BoundarySubmissions_SectionId");
            migrationBuilder.RenameColumn(name: "BlockId", table: "BoundarySubmissions", newName: "SectionId");

            migrationBuilder.RenameColumn(name: "BlockId", table: "CropAllocationRules", newName: "SectionId");

            migrationBuilder.RenameIndex(
                name: "IX_SchedulingPredictions_BlockId_ActivityType",
                table: "SchedulingPredictions",
                newName: "IX_SchedulingPredictions_SectionId_ActivityType");
            migrationBuilder.RenameColumn(name: "BlockId", table: "SchedulingPredictions", newName: "SectionId");

            migrationBuilder.RenameIndex(name: "IX_StockMovements_BlockId", table: "StockMovements", newName: "IX_StockMovements_SectionId");
            migrationBuilder.RenameColumn(name: "BlockId", table: "StockMovements", newName: "SectionId");

            migrationBuilder.RenameIndex(name: "IX_ActivityLogs_BlockId", table: "ActivityLogs", newName: "IX_ActivityLogs_SectionId");
            migrationBuilder.RenameColumn(name: "BlockId", table: "ActivityLogs", newName: "SectionId");

            // 2. Blocks → Sections. TreeCount has no home on a section, so it's lost on rollback.
            migrationBuilder.DropColumn(name: "TreeCount", table: "Blocks");

            migrationBuilder.RenameIndex(name: "IX_Blocks_FarmId_Status", table: "Blocks", newName: "IX_Sections_FarmId_Status");
            migrationBuilder.RenameIndex(name: "IX_Blocks_FarmId", table: "Blocks", newName: "IX_Sections_FarmId");
            migrationBuilder.Sql(@"ALTER TABLE ""Blocks"" RENAME CONSTRAINT ""FK_Blocks_Farms_FarmId"" TO ""FK_Sections_Farms_FarmId"";");
            migrationBuilder.Sql(@"ALTER TABLE ""Blocks"" RENAME CONSTRAINT ""PK_Blocks"" TO ""PK_Sections"";");
            migrationBuilder.RenameTable(name: "Blocks", newName: "Sections");

            // 1. Recreate the (empty) sub-section level. Existing activity logs had no separate
            //    block, so the restored required BlockId falls back to their section id.
            migrationBuilder.AddColumn<string>(
                name: "BlockId",
                table: "StockMovements",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BlockId",
                table: "ActivityLogs",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
            migrationBuilder.Sql(@"UPDATE ""ActivityLogs"" SET ""BlockId"" = ""SectionId"";");
            migrationBuilder.CreateIndex(name: "IX_ActivityLogs_BlockId", table: "ActivityLogs", column: "BlockId");

            migrationBuilder.CreateTable(
                name: "Blocks",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    SectionId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FarmId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Hectares = table.Column<double>(type: "double precision", nullable: false),
                    TreeCount = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Blocks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Blocks_Sections_SectionId",
                        column: x => x.SectionId,
                        principalTable: "Sections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(name: "IX_Blocks_FarmId", table: "Blocks", column: "FarmId");
            migrationBuilder.CreateIndex(name: "IX_Blocks_SectionId", table: "Blocks", column: "SectionId");
        }
    }
}
