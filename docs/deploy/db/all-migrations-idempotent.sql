CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260705214710_InitialCreate') THEN
    CREATE TABLE "ActivityLogs" (
        "Id" text NOT NULL,
        "FarmId" character varying(50) NOT NULL,
        "SectionId" character varying(50) NOT NULL,
        "BlockId" character varying(50) NOT NULL,
        "ActivityType" integer NOT NULL,
        "Date" timestamp with time zone NOT NULL,
        "Notes" character varying(2000),
        "SupervisorId" character varying(100) NOT NULL,
        "LabourCost" numeric(14,2) NOT NULL,
        "InputCost" numeric(14,2) NOT NULL,
        "CreatedOffline" boolean NOT NULL,
        "SyncedAt" timestamp with time zone,
        "WorkedAreaGeoJson" text,
        "WorkedHectares" double precision,
        "ChemicalProductId" character varying(50),
        "ChemicalProductName" character varying(200),
        "Workers" jsonb NOT NULL,
        "Inputs" jsonb NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_ActivityLogs" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260705214710_InitialCreate') THEN
    CREATE TABLE "BoundarySubmissions" (
        "Id" text NOT NULL,
        "SectionId" character varying(50) NOT NULL,
        "OperationId" character varying(100) NOT NULL,
        "SubmissionType" integer NOT NULL,
        "RawPointsJson" text,
        "ComputedGeoJson" text NOT NULL,
        "ComputedHectares" double precision NOT NULL,
        "ComputedCentroidLat" double precision,
        "ComputedCentroidLng" double precision,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_BoundarySubmissions" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260705214710_InitialCreate') THEN
    CREATE TABLE "ChemicalProducts" (
        "Id" text NOT NULL,
        "Name" character varying(200) NOT NULL,
        "Manufacturer" character varying(200),
        "ChemType" integer NOT NULL,
        "TypicalIntervalDays" integer NOT NULL,
        "Active" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_ChemicalProducts" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260705214710_InitialCreate') THEN
    CREATE TABLE "Companies" (
        "Id" text NOT NULL,
        "Name" character varying(200) NOT NULL,
        "OwnerId" character varying(100) NOT NULL,
        "IsDefaultCompany" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Companies" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260705214710_InitialCreate') THEN
    CREATE TABLE "SchedulingPredictions" (
        "Id" text NOT NULL,
        "FarmId" character varying(50) NOT NULL,
        "SectionId" character varying(50) NOT NULL,
        "ActivityType" integer NOT NULL,
        "LastActivityDate" timestamp with time zone,
        "PredictedNextDue" timestamp with time zone NOT NULL,
        "Reasoning" character varying(500) NOT NULL,
        "RecommendedChemical" character varying(200),
        "RecommendedSprayDate" timestamp with time zone,
        "Confidence" integer NOT NULL,
        "ComputedAt" timestamp with time zone NOT NULL,
        "WeatherDataUsed" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_SchedulingPredictions" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260705214710_InitialCreate') THEN
    CREATE TABLE "States" (
        "Id" text NOT NULL,
        "Name" character varying(100) NOT NULL,
        "Code" character varying(10) NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_States" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260705214710_InitialCreate') THEN
    CREATE TABLE "Transactions" (
        "Id" text NOT NULL,
        "FarmId" character varying(50) NOT NULL,
        "MonoId" character varying(100) NOT NULL,
        "Narration" character varying(500) NOT NULL,
        "Amount" numeric(14,2) NOT NULL,
        "Date" timestamp with time zone NOT NULL,
        "Category" integer NOT NULL,
        "CropAllocation" jsonb NOT NULL,
        "Confidence" integer NOT NULL,
        "Source" integer NOT NULL,
        "ReviewedByUser" boolean NOT NULL,
        "ReviewNotes" character varying(500),
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Transactions" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260705214710_InitialCreate') THEN
    CREATE TABLE "Users" (
        "Id" text NOT NULL,
        "FullName" character varying(200) NOT NULL,
        "Email" character varying(320) NOT NULL,
        "PasswordHash" text NOT NULL,
        "CompanyId" character varying(50),
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Users" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260705214710_InitialCreate') THEN
    CREATE TABLE "Farms" (
        "Id" text NOT NULL,
        "Name" character varying(200) NOT NULL,
        "Location" character varying(300) NOT NULL,
        "TotalHectares" double precision NOT NULL,
        "OwnerId" character varying(100) NOT NULL,
        "CompanyId" character varying(50) NOT NULL,
        "CountryCode" character varying(2) NOT NULL,
        "StateId" character varying(50),
        "LgaFreetext" character varying(200),
        "SetupStatus" integer NOT NULL,
        "PreferredAreaUnit" integer NOT NULL,
        "CentroidLat" double precision,
        "CentroidLng" double precision,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Farms" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_Farms_Companies_CompanyId" FOREIGN KEY ("CompanyId") REFERENCES "Companies" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260705214710_InitialCreate') THEN
    CREATE TABLE "CropAllocationRules" (
        "Id" text NOT NULL,
        "FarmId" character varying(50) NOT NULL,
        "Category" integer NOT NULL,
        "SectionId" character varying(50),
        "SeasonMonths" jsonb,
        "Allocation" jsonb NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_CropAllocationRules" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_CropAllocationRules_Farms_FarmId" FOREIGN KEY ("FarmId") REFERENCES "Farms" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260705214710_InitialCreate') THEN
    CREATE TABLE "InventoryItems" (
        "Id" text NOT NULL,
        "FarmId" character varying(50) NOT NULL,
        "Name" character varying(200) NOT NULL,
        "Unit" character varying(50) NOT NULL,
        "Category" integer NOT NULL,
        "CurrentStock" numeric(14,3) NOT NULL,
        "ReorderThreshold" numeric(14,3) NOT NULL,
        "ChemicalProductId" character varying(50),
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_InventoryItems" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_InventoryItems_Farms_FarmId" FOREIGN KEY ("FarmId") REFERENCES "Farms" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260705214710_InitialCreate') THEN
    CREATE TABLE "Sections" (
        "Id" text NOT NULL,
        "FarmId" character varying(50) NOT NULL,
        "Name" character varying(200) NOT NULL,
        "Crops" jsonb NOT NULL,
        "Hectares" double precision NOT NULL,
        "Status" integer NOT NULL,
        "LastActivityDate" timestamp with time zone,
        "BoundaryGeoJson" text,
        "BoundarySource" integer,
        "CentroidLat" double precision,
        "CentroidLng" double precision,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Sections" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_Sections_Farms_FarmId" FOREIGN KEY ("FarmId") REFERENCES "Farms" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260705214710_InitialCreate') THEN
    CREATE TABLE "WorkerRoles" (
        "Id" text NOT NULL,
        "FarmId" character varying(50) NOT NULL,
        "Title" character varying(100) NOT NULL,
        "DefaultType" integer NOT NULL,
        "DailyRate" numeric(12,2) NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_WorkerRoles" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_WorkerRoles_Farms_FarmId" FOREIGN KEY ("FarmId") REFERENCES "Farms" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260705214710_InitialCreate') THEN
    CREATE TABLE "StockMovements" (
        "Id" text NOT NULL,
        "FarmId" character varying(50) NOT NULL,
        "InventoryItemId" character varying(50) NOT NULL,
        "Type" integer NOT NULL,
        "Quantity" numeric(14,3) NOT NULL,
        "ActivityLogId" character varying(50),
        "TransactionId" character varying(50),
        "Notes" character varying(500),
        "Date" timestamp with time zone NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_StockMovements" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_StockMovements_InventoryItems_InventoryItemId" FOREIGN KEY ("InventoryItemId") REFERENCES "InventoryItems" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260705214710_InitialCreate') THEN
    CREATE TABLE "Blocks" (
        "Id" text NOT NULL,
        "SectionId" character varying(50) NOT NULL,
        "FarmId" character varying(50) NOT NULL,
        "Name" character varying(200) NOT NULL,
        "Hectares" double precision NOT NULL,
        "TreeCount" integer,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Blocks" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_Blocks_Sections_SectionId" FOREIGN KEY ("SectionId") REFERENCES "Sections" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260705214710_InitialCreate') THEN
    CREATE TABLE "Workers" (
        "Id" text NOT NULL,
        "FarmId" character varying(50) NOT NULL,
        "Name" character varying(200) NOT NULL,
        "Initials" character varying(10) NOT NULL,
        "WorkerRoleId" character varying(50) NOT NULL,
        "Type" integer NOT NULL,
        "Active" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Workers" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_Workers_Farms_FarmId" FOREIGN KEY ("FarmId") REFERENCES "Farms" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_Workers_WorkerRoles_WorkerRoleId" FOREIGN KEY ("WorkerRoleId") REFERENCES "WorkerRoles" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260705214710_InitialCreate') THEN
    CREATE INDEX "IX_ActivityLogs_BlockId" ON "ActivityLogs" ("BlockId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260705214710_InitialCreate') THEN
    CREATE INDEX "IX_ActivityLogs_FarmId_Date" ON "ActivityLogs" ("FarmId", "Date");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260705214710_InitialCreate') THEN
    CREATE INDEX "IX_ActivityLogs_SectionId" ON "ActivityLogs" ("SectionId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260705214710_InitialCreate') THEN
    CREATE INDEX "IX_Blocks_FarmId" ON "Blocks" ("FarmId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260705214710_InitialCreate') THEN
    CREATE INDEX "IX_Blocks_SectionId" ON "Blocks" ("SectionId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260705214710_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_BoundarySubmissions_OperationId" ON "BoundarySubmissions" ("OperationId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260705214710_InitialCreate') THEN
    CREATE INDEX "IX_BoundarySubmissions_SectionId" ON "BoundarySubmissions" ("SectionId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260705214710_InitialCreate') THEN
    CREATE INDEX "IX_ChemicalProducts_Active" ON "ChemicalProducts" ("Active");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260705214710_InitialCreate') THEN
    CREATE INDEX "IX_ChemicalProducts_ChemType" ON "ChemicalProducts" ("ChemType");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260705214710_InitialCreate') THEN
    CREATE INDEX "IX_Companies_OwnerId" ON "Companies" ("OwnerId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260705214710_InitialCreate') THEN
    CREATE INDEX "IX_CropAllocationRules_FarmId_Category" ON "CropAllocationRules" ("FarmId", "Category");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260705214710_InitialCreate') THEN
    CREATE INDEX "IX_Farms_CompanyId" ON "Farms" ("CompanyId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260705214710_InitialCreate') THEN
    CREATE INDEX "IX_Farms_OwnerId" ON "Farms" ("OwnerId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260705214710_InitialCreate') THEN
    CREATE INDEX "IX_Farms_StateId" ON "Farms" ("StateId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260705214710_InitialCreate') THEN
    CREATE INDEX "IX_InventoryItems_ChemicalProductId" ON "InventoryItems" ("ChemicalProductId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260705214710_InitialCreate') THEN
    CREATE INDEX "IX_InventoryItems_FarmId_Category" ON "InventoryItems" ("FarmId", "Category");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260705214710_InitialCreate') THEN
    CREATE INDEX "IX_SchedulingPredictions_FarmId_PredictedNextDue" ON "SchedulingPredictions" ("FarmId", "PredictedNextDue");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260705214710_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_SchedulingPredictions_SectionId_ActivityType" ON "SchedulingPredictions" ("SectionId", "ActivityType");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260705214710_InitialCreate') THEN
    CREATE INDEX "IX_Sections_FarmId" ON "Sections" ("FarmId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260705214710_InitialCreate') THEN
    CREATE INDEX "IX_Sections_FarmId_Status" ON "Sections" ("FarmId", "Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260705214710_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_States_Name" ON "States" ("Name");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260705214710_InitialCreate') THEN
    CREATE INDEX "IX_StockMovements_ActivityLogId" ON "StockMovements" ("ActivityLogId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260705214710_InitialCreate') THEN
    CREATE INDEX "IX_StockMovements_FarmId_Date" ON "StockMovements" ("FarmId", "Date");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260705214710_InitialCreate') THEN
    CREATE INDEX "IX_StockMovements_InventoryItemId" ON "StockMovements" ("InventoryItemId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260705214710_InitialCreate') THEN
    CREATE INDEX "IX_Transactions_FarmId_Date" ON "Transactions" ("FarmId", "Date");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260705214710_InitialCreate') THEN
    CREATE INDEX "IX_Transactions_FarmId_ReviewedByUser" ON "Transactions" ("FarmId", "ReviewedByUser");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260705214710_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_Transactions_MonoId" ON "Transactions" ("MonoId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260705214710_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_Users_Email" ON "Users" ("Email");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260705214710_InitialCreate') THEN
    CREATE INDEX "IX_WorkerRoles_FarmId" ON "WorkerRoles" ("FarmId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260705214710_InitialCreate') THEN
    CREATE INDEX "IX_Workers_FarmId_Active" ON "Workers" ("FarmId", "Active");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260705214710_InitialCreate') THEN
    CREATE INDEX "IX_Workers_WorkerRoleId" ON "Workers" ("WorkerRoleId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260705214710_InitialCreate') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260705214710_InitialCreate', '8.0.11');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260712190057_AddWorkerPayFrequency') THEN
    ALTER TABLE "WorkerRoles" RENAME COLUMN "DailyRate" TO "PayRate";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260712190057_AddWorkerPayFrequency') THEN
    ALTER TABLE "WorkerRoles" ADD "PayFrequency" integer NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260712190057_AddWorkerPayFrequency') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260712190057_AddWorkerPayFrequency', '8.0.11');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260713131027_AddInventoryCategories') THEN
    CREATE TABLE "InventoryCategories" (
        "Id" text NOT NULL,
        "FarmId" character varying(50) NOT NULL,
        "Name" character varying(100) NOT NULL,
        "IsDefault" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_InventoryCategories" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_InventoryCategories_Farms_FarmId" FOREIGN KEY ("FarmId") REFERENCES "Farms" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260713131027_AddInventoryCategories') THEN
    CREATE INDEX "IX_InventoryCategories_FarmId" ON "InventoryCategories" ("FarmId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260713131027_AddInventoryCategories') THEN
    CREATE INDEX "IX_InventoryCategories_FarmId_Name" ON "InventoryCategories" ("FarmId", "Name");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260713131027_AddInventoryCategories') THEN

                    INSERT INTO "InventoryCategories" ("Id", "FarmId", "Name", "IsDefault", "CreatedAt", "UpdatedAt")
                    SELECT f."Id" || '-cat-' || cat.slug, f."Id", cat.name, true, now(), now()
                    FROM "Farms" f
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
                
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260713131027_AddInventoryCategories') THEN
    ALTER TABLE "InventoryItems" ADD "CategoryId" character varying(50);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260713131027_AddInventoryCategories') THEN

                    UPDATE "InventoryItems" ii
                    SET "CategoryId" = ii."FarmId" || '-cat-' || CASE ii."Category"
                        WHEN 0 THEN 'chemical'
                        WHEN 1 THEN 'fertilizer'
                        WHEN 2 THEN 'seed'
                        WHEN 3 THEN 'fuel'
                        WHEN 4 THEN 'equipment'
                        WHEN 5 THEN 'spare-parts'
                        WHEN 6 THEN 'other'
                    END;
                
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260713131027_AddInventoryCategories') THEN
    ALTER TABLE "InventoryItems" ALTER COLUMN "CategoryId" SET NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260713131027_AddInventoryCategories') THEN
    DROP INDEX "IX_InventoryItems_FarmId_Category";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260713131027_AddInventoryCategories') THEN
    ALTER TABLE "InventoryItems" DROP COLUMN "Category";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260713131027_AddInventoryCategories') THEN
    CREATE INDEX "IX_InventoryItems_CategoryId" ON "InventoryItems" ("CategoryId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260713131027_AddInventoryCategories') THEN
    CREATE INDEX "IX_InventoryItems_FarmId_CategoryId" ON "InventoryItems" ("FarmId", "CategoryId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260713131027_AddInventoryCategories') THEN
    ALTER TABLE "InventoryItems" ADD CONSTRAINT "FK_InventoryItems_InventoryCategories_CategoryId" FOREIGN KEY ("CategoryId") REFERENCES "InventoryCategories" ("Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260713131027_AddInventoryCategories') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260713131027_AddInventoryCategories', '8.0.11');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260713142538_AddStockMovementWorkerId') THEN
    ALTER TABLE "StockMovements" ADD "WorkerId" character varying(50);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260713142538_AddStockMovementWorkerId') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260713142538_AddStockMovementWorkerId', '8.0.11');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260713152350_AddUserRole') THEN
    ALTER TABLE "Users" ADD "Role" character varying(20) NOT NULL DEFAULT 'Admin';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260713152350_AddUserRole') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260713152350_AddUserRole', '8.0.11');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260713153148_AddStockAdjustmentRequests') THEN
    CREATE TABLE "StockAdjustmentRequests" (
        "Id" text NOT NULL,
        "FarmId" character varying(50) NOT NULL,
        "InventoryItemId" character varying(50) NOT NULL,
        "RequestedQuantity" numeric(14,3) NOT NULL,
        "Status" character varying(20) NOT NULL,
        "Notes" character varying(500) NOT NULL,
        "RequestedByUserId" character varying(100) NOT NULL,
        "ReviewedByUserId" character varying(100),
        "ReviewedAt" timestamp with time zone,
        "ReviewNotes" character varying(500),
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_StockAdjustmentRequests" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260713153148_AddStockAdjustmentRequests') THEN
    CREATE INDEX "IX_StockAdjustmentRequests_FarmId" ON "StockAdjustmentRequests" ("FarmId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260713153148_AddStockAdjustmentRequests') THEN
    CREATE INDEX "IX_StockAdjustmentRequests_FarmId_Status" ON "StockAdjustmentRequests" ("FarmId", "Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260713153148_AddStockAdjustmentRequests') THEN
    CREATE INDEX "IX_StockAdjustmentRequests_InventoryItemId" ON "StockAdjustmentRequests" ("InventoryItemId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260713153148_AddStockAdjustmentRequests') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260713153148_AddStockAdjustmentRequests', '8.0.11');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260713171230_AddSuppliersAndOrders') THEN
    ALTER TABLE "StockMovements" ADD "OrderId" character varying(50);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260713171230_AddSuppliersAndOrders') THEN
    CREATE TABLE "Suppliers" (
        "Id" text NOT NULL,
        "FarmId" character varying(50) NOT NULL,
        "Name" character varying(200) NOT NULL,
        "Phone" character varying(30),
        "Email" character varying(200),
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Suppliers" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260713171230_AddSuppliersAndOrders') THEN
    CREATE TABLE "Orders" (
        "Id" text NOT NULL,
        "FarmId" character varying(50) NOT NULL,
        "SupplierId" character varying(50) NOT NULL,
        "Status" integer NOT NULL,
        "LineItems" jsonb NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Orders" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_Orders_Suppliers_SupplierId" FOREIGN KEY ("SupplierId") REFERENCES "Suppliers" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260713171230_AddSuppliersAndOrders') THEN
    CREATE INDEX "IX_Orders_FarmId" ON "Orders" ("FarmId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260713171230_AddSuppliersAndOrders') THEN
    CREATE INDEX "IX_Orders_FarmId_Status" ON "Orders" ("FarmId", "Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260713171230_AddSuppliersAndOrders') THEN
    CREATE INDEX "IX_Orders_SupplierId" ON "Orders" ("SupplierId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260713171230_AddSuppliersAndOrders') THEN
    CREATE INDEX "IX_Suppliers_FarmId" ON "Suppliers" ("FarmId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260713171230_AddSuppliersAndOrders') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260713171230_AddSuppliersAndOrders', '8.0.11');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260713211751_AddStockMovementSectionAndPerformedBy') THEN
    ALTER TABLE "StockMovements" ADD "BlockId" character varying(50);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260713211751_AddStockMovementSectionAndPerformedBy') THEN
    ALTER TABLE "StockMovements" ADD "PerformedByUserId" character varying(50);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260713211751_AddStockMovementSectionAndPerformedBy') THEN
    ALTER TABLE "StockMovements" ADD "SectionId" character varying(50);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260713211751_AddStockMovementSectionAndPerformedBy') THEN
    CREATE INDEX "IX_StockMovements_SectionId" ON "StockMovements" ("SectionId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260713211751_AddStockMovementSectionAndPerformedBy') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260713211751_AddStockMovementSectionAndPerformedBy', '8.0.11');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261011103310_MergeSectionsIntoBlocks') THEN
    DROP TABLE "Blocks";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261011103310_MergeSectionsIntoBlocks') THEN
    DROP INDEX "IX_ActivityLogs_BlockId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261011103310_MergeSectionsIntoBlocks') THEN
    ALTER TABLE "ActivityLogs" DROP COLUMN "BlockId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261011103310_MergeSectionsIntoBlocks') THEN
    ALTER TABLE "StockMovements" DROP COLUMN "BlockId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261011103310_MergeSectionsIntoBlocks') THEN
    ALTER TABLE "Sections" RENAME TO "Blocks";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261011103310_MergeSectionsIntoBlocks') THEN
    ALTER TABLE "Blocks" RENAME CONSTRAINT "PK_Sections" TO "PK_Blocks";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261011103310_MergeSectionsIntoBlocks') THEN
    ALTER TABLE "Blocks" RENAME CONSTRAINT "FK_Sections_Farms_FarmId" TO "FK_Blocks_Farms_FarmId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261011103310_MergeSectionsIntoBlocks') THEN
    ALTER INDEX "IX_Sections_FarmId" RENAME TO "IX_Blocks_FarmId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261011103310_MergeSectionsIntoBlocks') THEN
    ALTER INDEX "IX_Sections_FarmId_Status" RENAME TO "IX_Blocks_FarmId_Status";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261011103310_MergeSectionsIntoBlocks') THEN
    ALTER TABLE "Blocks" ADD "TreeCount" integer;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261011103310_MergeSectionsIntoBlocks') THEN
    ALTER TABLE "ActivityLogs" RENAME COLUMN "SectionId" TO "BlockId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261011103310_MergeSectionsIntoBlocks') THEN
    ALTER INDEX "IX_ActivityLogs_SectionId" RENAME TO "IX_ActivityLogs_BlockId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261011103310_MergeSectionsIntoBlocks') THEN
    ALTER TABLE "StockMovements" RENAME COLUMN "SectionId" TO "BlockId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261011103310_MergeSectionsIntoBlocks') THEN
    ALTER INDEX "IX_StockMovements_SectionId" RENAME TO "IX_StockMovements_BlockId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261011103310_MergeSectionsIntoBlocks') THEN
    ALTER TABLE "SchedulingPredictions" RENAME COLUMN "SectionId" TO "BlockId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261011103310_MergeSectionsIntoBlocks') THEN
    ALTER INDEX "IX_SchedulingPredictions_SectionId_ActivityType" RENAME TO "IX_SchedulingPredictions_BlockId_ActivityType";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261011103310_MergeSectionsIntoBlocks') THEN
    ALTER TABLE "CropAllocationRules" RENAME COLUMN "SectionId" TO "BlockId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261011103310_MergeSectionsIntoBlocks') THEN
    ALTER TABLE "BoundarySubmissions" RENAME COLUMN "SectionId" TO "BlockId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261011103310_MergeSectionsIntoBlocks') THEN
    ALTER INDEX "IX_BoundarySubmissions_SectionId" RENAME TO "IX_BoundarySubmissions_BlockId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261011103310_MergeSectionsIntoBlocks') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261011103310_MergeSectionsIntoBlocks', '8.0.11');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261011171135_TrackPickedStockUsage') THEN
    ALTER TABLE "StockMovements" ADD "ReturnedQuantity" numeric(14,3) NOT NULL DEFAULT 0.0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261011171135_TrackPickedStockUsage') THEN
    ALTER TABLE "StockMovements" ADD "UsedQuantity" numeric(14,3) NOT NULL DEFAULT 0.0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261011171135_TrackPickedStockUsage') THEN
    UPDATE "StockMovements" SET "UsedQuantity" = "Quantity" WHERE "Type" = 1 AND "PerformedByUserId" IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261011171135_TrackPickedStockUsage') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261011171135_TrackPickedStockUsage', '8.0.11');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261011183441_AddStockPricing') THEN
    ALTER TABLE "InventoryItems" ADD "PackLabel" character varying(30) NOT NULL DEFAULT 'unit';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261011183441_AddStockPricing') THEN
    ALTER TABLE "InventoryItems" ADD "PackSize" numeric(14,3) NOT NULL DEFAULT 1.0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261011183441_AddStockPricing') THEN
    ALTER TABLE "InventoryItems" ADD "UnitCost" numeric(14,4) NOT NULL DEFAULT 0.0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261011183441_AddStockPricing') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261011183441_AddStockPricing', '8.0.11');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261011202820_AddBlockPlantingYear') THEN
    ALTER TABLE "Blocks" ADD "PlantingYear" integer;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261011202820_AddBlockPlantingYear') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261011202820_AddBlockPlantingYear', '8.0.11');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261011224309_AddOrderPaymentAndTransactionKind') THEN
    ALTER TABLE "Transactions" ADD "Kind" integer NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261011224309_AddOrderPaymentAndTransactionKind') THEN
    ALTER TABLE "Transactions" ADD "OrderId" character varying(50);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261011224309_AddOrderPaymentAndTransactionKind') THEN
    ALTER TABLE "Transactions" ADD "Reconciliation" integer NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261011224309_AddOrderPaymentAndTransactionKind') THEN
    ALTER TABLE "Transactions" ADD "ReconciliationNote" character varying(500);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261011224309_AddOrderPaymentAndTransactionKind') THEN
    ALTER TABLE "Orders" ADD "Number" integer NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261011224309_AddOrderPaymentAndTransactionKind') THEN
    ALTER TABLE "Orders" ADD "PaidAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261011224309_AddOrderPaymentAndTransactionKind') THEN
    ALTER TABLE "Orders" ADD "PaymentTransactionId" character varying(50);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261011224309_AddOrderPaymentAndTransactionKind') THEN

                    UPDATE "Orders" o SET "Number" = n.rn + 1000
                    FROM (SELECT "Id", ROW_NUMBER() OVER (PARTITION BY "FarmId" ORDER BY "CreatedAt", "Id") AS rn FROM "Orders") n
                    WHERE o."Id" = n."Id";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261011224309_AddOrderPaymentAndTransactionKind') THEN

                    UPDATE "Transactions"
                    SET "Kind" = CASE WHEN "Narration" ~* '\m(capital|investment)\M' THEN 2 ELSE 1 END
                    WHERE "Amount" >= 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261011224309_AddOrderPaymentAndTransactionKind') THEN
    CREATE INDEX "IX_Transactions_FarmId_Reconciliation" ON "Transactions" ("FarmId", "Reconciliation");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261011224309_AddOrderPaymentAndTransactionKind') THEN
    CREATE UNIQUE INDEX "IX_Orders_FarmId_Number" ON "Orders" ("FarmId", "Number");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261011224309_AddOrderPaymentAndTransactionKind') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261011224309_AddOrderPaymentAndTransactionKind', '8.0.11');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261011231121_AddFarmBankAccount') THEN
    ALTER TABLE "Farms" ADD "BankAccountId" character varying(100);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261011231121_AddFarmBankAccount') THEN
    ALTER TABLE "Farms" ADD "BankLinkedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261011231121_AddFarmBankAccount') THEN
    ALTER TABLE "Farms" ADD "LastBankSyncAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261011231121_AddFarmBankAccount') THEN
    CREATE INDEX "IX_Farms_BankAccountId" ON "Farms" ("BankAccountId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261011231121_AddFarmBankAccount') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261011231121_AddFarmBankAccount', '8.0.11');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261012102634_AddTransactionBlock') THEN
    ALTER TABLE "Transactions" ADD "BlockId" character varying(50);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261012102634_AddTransactionBlock') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261012102634_AddTransactionBlock', '8.0.11');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261012110908_AddPriceChangeRequests') THEN
    CREATE TABLE "PriceChangeRequests" (
        "Id" text NOT NULL,
        "FarmId" character varying(50) NOT NULL,
        "InventoryItemId" character varying(50) NOT NULL,
        "PackLabel" character varying(40) NOT NULL,
        "PackSize" numeric(14,3) NOT NULL,
        "PricePerPack" numeric(14,2) NOT NULL,
        "BackfillActivities" boolean NOT NULL,
        "Notes" character varying(500),
        "Status" character varying(20) NOT NULL,
        "RequestedByUserId" character varying(100) NOT NULL,
        "ReviewedByUserId" character varying(100),
        "ReviewedAt" timestamp with time zone,
        "ReviewNotes" character varying(500),
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_PriceChangeRequests" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261012110908_AddPriceChangeRequests') THEN
    CREATE INDEX "IX_PriceChangeRequests_FarmId_Status" ON "PriceChangeRequests" ("FarmId", "Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261012110908_AddPriceChangeRequests') THEN
    CREATE INDEX "IX_PriceChangeRequests_InventoryItemId" ON "PriceChangeRequests" ("InventoryItemId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261012110908_AddPriceChangeRequests') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261012110908_AddPriceChangeRequests', '8.0.11');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261012112427_AddCoverageCompletions') THEN
    CREATE TABLE "CoverageCompletions" (
        "Id" text NOT NULL,
        "FarmId" character varying(50) NOT NULL,
        "BlockId" character varying(50) NOT NULL,
        "ActivityType" character varying(40) NOT NULL,
        "CompletedOn" timestamp with time zone NOT NULL,
        "Reason" character varying(500),
        "CompletedByUserId" character varying(100) NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_CoverageCompletions" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261012112427_AddCoverageCompletions') THEN
    CREATE INDEX "IX_CoverageCompletions_BlockId_ActivityType" ON "CoverageCompletions" ("BlockId", "ActivityType");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261012112427_AddCoverageCompletions') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261012112427_AddCoverageCompletions', '8.0.11');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261012115821_AddUserIsActive') THEN
    ALTER TABLE "Users" ADD "IsActive" boolean NOT NULL DEFAULT TRUE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261012115821_AddUserIsActive') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261012115821_AddUserIsActive', '8.0.11');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261012130048_AddSatelliteReadings') THEN
    ALTER TABLE "Farms" ADD "LastSatelliteSyncAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261012130048_AddSatelliteReadings') THEN
    CREATE TABLE "BlockSatelliteReadings" (
        "Id" text NOT NULL,
        "FarmId" character varying(50) NOT NULL,
        "BlockId" character varying(50) NOT NULL,
        "Date" timestamp with time zone NOT NULL,
        "Ndvi" double precision NOT NULL,
        "Ndmi" double precision NOT NULL,
        "ValidShare" double precision NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_BlockSatelliteReadings" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261012130048_AddSatelliteReadings') THEN
    CREATE UNIQUE INDEX "IX_BlockSatelliteReadings_BlockId_Date" ON "BlockSatelliteReadings" ("BlockId", "Date");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261012130048_AddSatelliteReadings') THEN
    CREATE INDEX "IX_BlockSatelliteReadings_FarmId" ON "BlockSatelliteReadings" ("FarmId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261012130048_AddSatelliteReadings') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261012130048_AddSatelliteReadings', '8.0.11');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261031224645_AddLogoInvoicesAndVendorBills') THEN
    ALTER TABLE "Farms" ADD "Address" character varying(300);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261031224645_AddLogoInvoicesAndVendorBills') THEN
    ALTER TABLE "Farms" ADD "BankAccountName" character varying(150);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261031224645_AddLogoInvoicesAndVendorBills') THEN
    ALTER TABLE "Farms" ADD "BankAccountNumber" character varying(40);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261031224645_AddLogoInvoicesAndVendorBills') THEN
    ALTER TABLE "Farms" ADD "BankName" character varying(100);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261031224645_AddLogoInvoicesAndVendorBills') THEN
    ALTER TABLE "Farms" ADD "Email" character varying(200);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261031224645_AddLogoInvoicesAndVendorBills') THEN
    ALTER TABLE "Farms" ADD "LogoFileId" character varying(50);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261031224645_AddLogoInvoicesAndVendorBills') THEN
    ALTER TABLE "Farms" ADD "LogoUpdatedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261031224645_AddLogoInvoicesAndVendorBills') THEN
    ALTER TABLE "Farms" ADD "Phone" character varying(60);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261031224645_AddLogoInvoicesAndVendorBills') THEN
    ALTER TABLE "Farms" ADD "Website" character varying(200);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261031224645_AddLogoInvoicesAndVendorBills') THEN
    CREATE TABLE "Invoices" (
        "Id" text NOT NULL,
        "FarmId" character varying(50) NOT NULL,
        "Number" integer NOT NULL,
        "Date" date NOT NULL,
        "DueDate" date NOT NULL,
        "BillToName" character varying(200) NOT NULL,
        "BillToPhone" character varying(60),
        "BillToEmail" character varying(200),
        "BillToAddress" character varying(500),
        "LineItems" jsonb NOT NULL,
        "VatPercent" numeric(5,2) NOT NULL,
        "PaymentAccountName" character varying(150),
        "PaymentAccountNumber" character varying(40),
        "PaymentBankName" character varying(100),
        "TermsAndNotes" character varying(2000),
        "SignatoryName" character varying(150),
        "TemplateKey" character varying(50) NOT NULL,
        "CreatedByUserId" character varying(100),
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Invoices" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_Invoices_Farms_FarmId" FOREIGN KEY ("FarmId") REFERENCES "Farms" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261031224645_AddLogoInvoicesAndVendorBills') THEN
    CREATE TABLE "StoredFiles" (
        "Id" text NOT NULL,
        "FarmId" character varying(50) NOT NULL,
        "FileName" character varying(200) NOT NULL,
        "ContentType" character varying(100) NOT NULL,
        "Size" bigint NOT NULL,
        "Content" bytea NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_StoredFiles" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261031224645_AddLogoInvoicesAndVendorBills') THEN
    CREATE TABLE "VendorInvoices" (
        "Id" text NOT NULL,
        "FarmId" character varying(50) NOT NULL,
        "OrderId" character varying(50) NOT NULL,
        "SupplierId" character varying(50) NOT NULL,
        "FileId" character varying(50) NOT NULL,
        "FileName" character varying(200) NOT NULL,
        "ContentType" character varying(100) NOT NULL,
        "FileSize" bigint NOT NULL,
        "VendorName" character varying(200) NOT NULL,
        "InvoiceNumber" character varying(100) NOT NULL,
        "Amount" numeric(18,2) NOT NULL,
        "InvoiceDate" date NOT NULL,
        "DueDate" date,
        "Status" integer NOT NULL,
        "PaidAt" timestamp with time zone,
        "Notes" character varying(1000),
        "UploadedByUserId" character varying(100),
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_VendorInvoices" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_VendorInvoices_Farms_FarmId" FOREIGN KEY ("FarmId") REFERENCES "Farms" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_VendorInvoices_Orders_OrderId" FOREIGN KEY ("OrderId") REFERENCES "Orders" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_VendorInvoices_Suppliers_SupplierId" FOREIGN KEY ("SupplierId") REFERENCES "Suppliers" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261031224645_AddLogoInvoicesAndVendorBills') THEN
    CREATE UNIQUE INDEX "IX_Invoices_FarmId_Number" ON "Invoices" ("FarmId", "Number");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261031224645_AddLogoInvoicesAndVendorBills') THEN
    CREATE INDEX "IX_StoredFiles_FarmId" ON "StoredFiles" ("FarmId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261031224645_AddLogoInvoicesAndVendorBills') THEN
    CREATE INDEX "IX_VendorInvoices_FarmId_Status" ON "VendorInvoices" ("FarmId", "Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261031224645_AddLogoInvoicesAndVendorBills') THEN
    CREATE INDEX "IX_VendorInvoices_FarmId_SupplierId_InvoiceNumber" ON "VendorInvoices" ("FarmId", "SupplierId", "InvoiceNumber");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261031224645_AddLogoInvoicesAndVendorBills') THEN
    CREATE INDEX "IX_VendorInvoices_OrderId" ON "VendorInvoices" ("OrderId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261031224645_AddLogoInvoicesAndVendorBills') THEN
    CREATE INDEX "IX_VendorInvoices_SupplierId" ON "VendorInvoices" ("SupplierId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261031224645_AddLogoInvoicesAndVendorBills') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261031224645_AddLogoInvoicesAndVendorBills', '8.0.11');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261031235504_VendorBillUniqueKeyAndInvoiceCounters') THEN
    DROP INDEX "IX_VendorInvoices_FarmId_SupplierId_InvoiceNumber";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261031235504_VendorBillUniqueKeyAndInvoiceCounters') THEN
    ALTER TABLE "VendorInvoices" ADD "InvoiceNumberKey" character varying(140) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261031235504_VendorBillUniqueKeyAndInvoiceCounters') THEN
    CREATE TABLE "InvoiceCounters" (
        "FarmId" character varying(50) NOT NULL,
        "LastNumber" integer NOT NULL,
        CONSTRAINT "PK_InvoiceCounters" PRIMARY KEY ("FarmId"),
        CONSTRAINT "FK_InvoiceCounters_Farms_FarmId" FOREIGN KEY ("FarmId") REFERENCES "Farms" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261031235504_VendorBillUniqueKeyAndInvoiceCounters') THEN
    UPDATE "VendorInvoices" SET "InvoiceNumberKey" = UPPER(BTRIM("InvoiceNumber"));
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261031235504_VendorBillUniqueKeyAndInvoiceCounters') THEN
    UPDATE "VendorInvoices" v
    SET "InvoiceNumberKey" = v."InvoiceNumberKey" || '#DUP-' || d.rn
    FROM (
        SELECT "Id", ROW_NUMBER() OVER (PARTITION BY "FarmId", "SupplierId", "InvoiceNumberKey" ORDER BY "CreatedAt", "Id") AS rn
        FROM "VendorInvoices"
    ) d
    WHERE v."Id" = d."Id" AND d.rn > 1;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261031235504_VendorBillUniqueKeyAndInvoiceCounters') THEN
    INSERT INTO "InvoiceCounters" ("FarmId", "LastNumber")
    SELECT "FarmId", MAX("Number") FROM "Invoices" GROUP BY "FarmId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261031235504_VendorBillUniqueKeyAndInvoiceCounters') THEN
    CREATE UNIQUE INDEX "IX_VendorInvoices_FarmId_SupplierId_InvoiceNumberKey" ON "VendorInvoices" ("FarmId", "SupplierId", "InvoiceNumberKey");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261031235504_VendorBillUniqueKeyAndInvoiceCounters') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261031235504_VendorBillUniqueKeyAndInvoiceCounters', '8.0.11');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261101110452_InvoiceBrandColours') THEN
    ALTER TABLE "Invoices" ADD "AccentColor" character varying(7);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261101110452_InvoiceBrandColours') THEN
    ALTER TABLE "Invoices" ADD "PrimaryColor" character varying(7);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261101110452_InvoiceBrandColours') THEN
    ALTER TABLE "Farms" ADD "BrandAccentColor" character varying(7);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261101110452_InvoiceBrandColours') THEN
    ALTER TABLE "Farms" ADD "BrandPrimaryColor" character varying(7);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261101110452_InvoiceBrandColours') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261101110452_InvoiceBrandColours', '8.0.11');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261109152503_AddCrops') THEN
    CREATE TABLE "Crops" (
        "Id" text NOT NULL,
        "FarmId" character varying(50),
        "Name" character varying(100) NOT NULL,
        "IsBuiltIn" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Crops" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261109152503_AddCrops') THEN
    INSERT INTO "Crops" ("Id", "CreatedAt", "FarmId", "IsBuiltIn", "Name", "UpdatedAt")
    VALUES ('Banana', TIMESTAMPTZ '2026-11-09T00:00:00Z', NULL, TRUE, 'Banana', TIMESTAMPTZ '2026-11-09T00:00:00Z');
    INSERT INTO "Crops" ("Id", "CreatedAt", "FarmId", "IsBuiltIn", "Name", "UpdatedAt")
    VALUES ('Cassava', TIMESTAMPTZ '2026-11-09T00:00:00Z', NULL, TRUE, 'Cassava', TIMESTAMPTZ '2026-11-09T00:00:00Z');
    INSERT INTO "Crops" ("Id", "CreatedAt", "FarmId", "IsBuiltIn", "Name", "UpdatedAt")
    VALUES ('Cocoa', TIMESTAMPTZ '2026-11-09T00:00:00Z', NULL, TRUE, 'Cocoa', TIMESTAMPTZ '2026-11-09T00:00:00Z');
    INSERT INTO "Crops" ("Id", "CreatedAt", "FarmId", "IsBuiltIn", "Name", "UpdatedAt")
    VALUES ('Maize', TIMESTAMPTZ '2026-11-09T00:00:00Z', NULL, TRUE, 'Maize', TIMESTAMPTZ '2026-11-09T00:00:00Z');
    INSERT INTO "Crops" ("Id", "CreatedAt", "FarmId", "IsBuiltIn", "Name", "UpdatedAt")
    VALUES ('OilPalm', TIMESTAMPTZ '2026-11-09T00:00:00Z', NULL, TRUE, 'Oil palm', TIMESTAMPTZ '2026-11-09T00:00:00Z');
    INSERT INTO "Crops" ("Id", "CreatedAt", "FarmId", "IsBuiltIn", "Name", "UpdatedAt")
    VALUES ('Plantain', TIMESTAMPTZ '2026-11-09T00:00:00Z', NULL, TRUE, 'Plantain', TIMESTAMPTZ '2026-11-09T00:00:00Z');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261109152503_AddCrops') THEN
    CREATE INDEX "IX_Crops_FarmId" ON "Crops" ("FarmId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261109152503_AddCrops') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261109152503_AddCrops', '8.0.11');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261110140721_AddInvitationsAndPasswordReset') THEN
    CREATE TABLE "Invitations" (
        "Id" text NOT NULL,
        "CompanyId" character varying(50) NOT NULL,
        "Email" character varying(320) NOT NULL,
        "Role" character varying(20) NOT NULL,
        "TokenHash" character varying(64) NOT NULL,
        "InvitedByUserId" character varying(50) NOT NULL,
        "ExpiresAt" timestamp with time zone NOT NULL,
        "AcceptedAt" timestamp with time zone,
        "RevokedAt" timestamp with time zone,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Invitations" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261110140721_AddInvitationsAndPasswordReset') THEN
    CREATE TABLE "PasswordResetTokens" (
        "Id" text NOT NULL,
        "UserId" character varying(50) NOT NULL,
        "TokenHash" character varying(64) NOT NULL,
        "ExpiresAt" timestamp with time zone NOT NULL,
        "UsedAt" timestamp with time zone,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_PasswordResetTokens" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261110140721_AddInvitationsAndPasswordReset') THEN
    CREATE INDEX "IX_Invitations_CompanyId_Email" ON "Invitations" ("CompanyId", "Email");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261110140721_AddInvitationsAndPasswordReset') THEN
    CREATE UNIQUE INDEX "IX_Invitations_TokenHash" ON "Invitations" ("TokenHash");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261110140721_AddInvitationsAndPasswordReset') THEN
    CREATE UNIQUE INDEX "IX_PasswordResetTokens_TokenHash" ON "PasswordResetTokens" ("TokenHash");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261110140721_AddInvitationsAndPasswordReset') THEN
    CREATE INDEX "IX_PasswordResetTokens_UserId" ON "PasswordResetTokens" ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261110140721_AddInvitationsAndPasswordReset') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261110140721_AddInvitationsAndPasswordReset', '8.0.11');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261110150338_AddEmailOutboxAndSessionVersion') THEN
    ALTER TABLE "Users" ADD "EmailNotificationsEnabled" boolean NOT NULL DEFAULT TRUE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261110150338_AddEmailOutboxAndSessionVersion') THEN
    ALTER TABLE "Users" ADD "TokenVersion" integer NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261110150338_AddEmailOutboxAndSessionVersion') THEN
    CREATE TABLE "EmailOutbox" (
        "Id" text NOT NULL,
        "To" character varying(320) NOT NULL,
        "Subject" character varying(300) NOT NULL,
        "Html" text NOT NULL,
        "Text" text NOT NULL,
        "Status" character varying(20) NOT NULL,
        "Attempts" integer NOT NULL,
        "NextAttemptAt" timestamp with time zone NOT NULL,
        "LeaseUntil" timestamp with time zone,
        "ClaimToken" character varying(50),
        "LastError" character varying(200),
        "SentAt" timestamp with time zone,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_EmailOutbox" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261110150338_AddEmailOutboxAndSessionVersion') THEN
    CREATE INDEX "IX_EmailOutbox_ClaimToken" ON "EmailOutbox" ("ClaimToken");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261110150338_AddEmailOutboxAndSessionVersion') THEN
    CREATE INDEX "IX_EmailOutbox_Status_NextAttemptAt" ON "EmailOutbox" ("Status", "NextAttemptAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261110150338_AddEmailOutboxAndSessionVersion') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261110150338_AddEmailOutboxAndSessionVersion', '8.0.11');
    END IF;
END $EF$;
COMMIT;

