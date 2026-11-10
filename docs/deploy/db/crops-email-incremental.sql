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

