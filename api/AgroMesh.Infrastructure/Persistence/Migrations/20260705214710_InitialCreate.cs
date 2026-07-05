using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgroMesh.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ActivityLogs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    FarmId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SectionId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    BlockId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ActivityType = table.Column<int>(type: "integer", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    SupervisorId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LabourCost = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    InputCost = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    CreatedOffline = table.Column<bool>(type: "boolean", nullable: false),
                    SyncedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    WorkedAreaGeoJson = table.Column<string>(type: "text", nullable: true),
                    WorkedHectares = table.Column<double>(type: "double precision", nullable: true),
                    ChemicalProductId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ChemicalProductName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Workers = table.Column<string>(type: "jsonb", nullable: false),
                    Inputs = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BoundarySubmissions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    SectionId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    OperationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SubmissionType = table.Column<int>(type: "integer", nullable: false),
                    RawPointsJson = table.Column<string>(type: "text", nullable: true),
                    ComputedGeoJson = table.Column<string>(type: "text", nullable: false),
                    ComputedHectares = table.Column<double>(type: "double precision", nullable: false),
                    ComputedCentroidLat = table.Column<double>(type: "double precision", nullable: true),
                    ComputedCentroidLng = table.Column<double>(type: "double precision", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BoundarySubmissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ChemicalProducts",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Manufacturer = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ChemType = table.Column<int>(type: "integer", nullable: false),
                    TypicalIntervalDays = table.Column<int>(type: "integer", nullable: false),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChemicalProducts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Companies",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    OwnerId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsDefaultCompany = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Companies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SchedulingPredictions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    FarmId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SectionId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ActivityType = table.Column<int>(type: "integer", nullable: false),
                    LastActivityDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PredictedNextDue = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Reasoning = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    RecommendedChemical = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    RecommendedSprayDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Confidence = table.Column<int>(type: "integer", nullable: false),
                    ComputedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    WeatherDataUsed = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchedulingPredictions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "States",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_States", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Transactions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    FarmId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    MonoId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Narration = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    CropAllocation = table.Column<string>(type: "jsonb", nullable: false),
                    Confidence = table.Column<int>(type: "integer", nullable: false),
                    Source = table.Column<int>(type: "integer", nullable: false),
                    ReviewedByUser = table.Column<bool>(type: "boolean", nullable: false),
                    ReviewNotes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transactions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    FullName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    CompanyId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Farms",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Location = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    TotalHectares = table.Column<double>(type: "double precision", nullable: false),
                    OwnerId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CompanyId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CountryCode = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    StateId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    LgaFreetext = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    SetupStatus = table.Column<int>(type: "integer", nullable: false),
                    PreferredAreaUnit = table.Column<int>(type: "integer", nullable: false),
                    CentroidLat = table.Column<double>(type: "double precision", nullable: true),
                    CentroidLng = table.Column<double>(type: "double precision", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Farms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Farms_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CropAllocationRules",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    FarmId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    SectionId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    SeasonMonths = table.Column<string>(type: "jsonb", nullable: true),
                    Allocation = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CropAllocationRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CropAllocationRules_Farms_FarmId",
                        column: x => x.FarmId,
                        principalTable: "Farms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InventoryItems",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    FarmId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    CurrentStock = table.Column<decimal>(type: "numeric(14,3)", nullable: false),
                    ReorderThreshold = table.Column<decimal>(type: "numeric(14,3)", nullable: false),
                    ChemicalProductId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryItems_Farms_FarmId",
                        column: x => x.FarmId,
                        principalTable: "Farms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Sections",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    FarmId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Crops = table.Column<string>(type: "jsonb", nullable: false),
                    Hectares = table.Column<double>(type: "double precision", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    LastActivityDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    BoundaryGeoJson = table.Column<string>(type: "text", nullable: true),
                    BoundarySource = table.Column<int>(type: "integer", nullable: true),
                    CentroidLat = table.Column<double>(type: "double precision", nullable: true),
                    CentroidLng = table.Column<double>(type: "double precision", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sections_Farms_FarmId",
                        column: x => x.FarmId,
                        principalTable: "Farms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkerRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    FarmId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DefaultType = table.Column<int>(type: "integer", nullable: false),
                    DailyRate = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkerRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkerRoles_Farms_FarmId",
                        column: x => x.FarmId,
                        principalTable: "Farms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StockMovements",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    FarmId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    InventoryItemId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(14,3)", nullable: false),
                    ActivityLogId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    TransactionId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockMovements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockMovements_InventoryItems_InventoryItemId",
                        column: x => x.InventoryItemId,
                        principalTable: "InventoryItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

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

            migrationBuilder.CreateTable(
                name: "Workers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    FarmId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Initials = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    WorkerRoleId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Workers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Workers_Farms_FarmId",
                        column: x => x.FarmId,
                        principalTable: "Farms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Workers_WorkerRoles_WorkerRoleId",
                        column: x => x.WorkerRoleId,
                        principalTable: "WorkerRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActivityLogs_BlockId",
                table: "ActivityLogs",
                column: "BlockId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityLogs_FarmId_Date",
                table: "ActivityLogs",
                columns: new[] { "FarmId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_ActivityLogs_SectionId",
                table: "ActivityLogs",
                column: "SectionId");

            migrationBuilder.CreateIndex(
                name: "IX_Blocks_FarmId",
                table: "Blocks",
                column: "FarmId");

            migrationBuilder.CreateIndex(
                name: "IX_Blocks_SectionId",
                table: "Blocks",
                column: "SectionId");

            migrationBuilder.CreateIndex(
                name: "IX_BoundarySubmissions_OperationId",
                table: "BoundarySubmissions",
                column: "OperationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BoundarySubmissions_SectionId",
                table: "BoundarySubmissions",
                column: "SectionId");

            migrationBuilder.CreateIndex(
                name: "IX_ChemicalProducts_Active",
                table: "ChemicalProducts",
                column: "Active");

            migrationBuilder.CreateIndex(
                name: "IX_ChemicalProducts_ChemType",
                table: "ChemicalProducts",
                column: "ChemType");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_OwnerId",
                table: "Companies",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_CropAllocationRules_FarmId_Category",
                table: "CropAllocationRules",
                columns: new[] { "FarmId", "Category" });

            migrationBuilder.CreateIndex(
                name: "IX_Farms_CompanyId",
                table: "Farms",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Farms_OwnerId",
                table: "Farms",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Farms_StateId",
                table: "Farms",
                column: "StateId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_ChemicalProductId",
                table: "InventoryItems",
                column: "ChemicalProductId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_FarmId_Category",
                table: "InventoryItems",
                columns: new[] { "FarmId", "Category" });

            migrationBuilder.CreateIndex(
                name: "IX_SchedulingPredictions_FarmId_PredictedNextDue",
                table: "SchedulingPredictions",
                columns: new[] { "FarmId", "PredictedNextDue" });

            migrationBuilder.CreateIndex(
                name: "IX_SchedulingPredictions_SectionId_ActivityType",
                table: "SchedulingPredictions",
                columns: new[] { "SectionId", "ActivityType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sections_FarmId",
                table: "Sections",
                column: "FarmId");

            migrationBuilder.CreateIndex(
                name: "IX_Sections_FarmId_Status",
                table: "Sections",
                columns: new[] { "FarmId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_States_Name",
                table: "States",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_ActivityLogId",
                table: "StockMovements",
                column: "ActivityLogId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_FarmId_Date",
                table: "StockMovements",
                columns: new[] { "FarmId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_InventoryItemId",
                table: "StockMovements",
                column: "InventoryItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_FarmId_Date",
                table: "Transactions",
                columns: new[] { "FarmId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_FarmId_ReviewedByUser",
                table: "Transactions",
                columns: new[] { "FarmId", "ReviewedByUser" });

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_MonoId",
                table: "Transactions",
                column: "MonoId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkerRoles_FarmId",
                table: "WorkerRoles",
                column: "FarmId");

            migrationBuilder.CreateIndex(
                name: "IX_Workers_FarmId_Active",
                table: "Workers",
                columns: new[] { "FarmId", "Active" });

            migrationBuilder.CreateIndex(
                name: "IX_Workers_WorkerRoleId",
                table: "Workers",
                column: "WorkerRoleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActivityLogs");

            migrationBuilder.DropTable(
                name: "Blocks");

            migrationBuilder.DropTable(
                name: "BoundarySubmissions");

            migrationBuilder.DropTable(
                name: "ChemicalProducts");

            migrationBuilder.DropTable(
                name: "CropAllocationRules");

            migrationBuilder.DropTable(
                name: "SchedulingPredictions");

            migrationBuilder.DropTable(
                name: "States");

            migrationBuilder.DropTable(
                name: "StockMovements");

            migrationBuilder.DropTable(
                name: "Transactions");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Workers");

            migrationBuilder.DropTable(
                name: "Sections");

            migrationBuilder.DropTable(
                name: "InventoryItems");

            migrationBuilder.DropTable(
                name: "WorkerRoles");

            migrationBuilder.DropTable(
                name: "Farms");

            migrationBuilder.DropTable(
                name: "Companies");
        }
    }
}
