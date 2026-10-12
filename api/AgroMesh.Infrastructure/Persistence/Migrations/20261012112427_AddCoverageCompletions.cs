using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgroMesh.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCoverageCompletions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CoverageCompletions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    FarmId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    BlockId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ActivityType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CompletedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CompletedByUserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoverageCompletions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CoverageCompletions_BlockId_ActivityType",
                table: "CoverageCompletions",
                columns: new[] { "BlockId", "ActivityType" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CoverageCompletions");
        }
    }
}
