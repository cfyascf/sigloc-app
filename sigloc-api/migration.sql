CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260907192403_InitialCreate') THEN
    CREATE TABLE "Product" (
        "Id" uuid NOT NULL,
        "Sku" text NOT NULL,
        "PackageType" text,
        "Temperature" text,
        "HandlingRestrictions" text,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "Version" integer NOT NULL,
        CONSTRAINT "PK_Product" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260907192403_InitialCreate') THEN
    CREATE TABLE "Vehicle" (
        "Id" uuid NOT NULL,
        "TransportadoraId" uuid NOT NULL,
        "Plate" text NOT NULL,
        "Model" text NOT NULL,
        "CapacityWeight" numeric NOT NULL,
        "CapacityVolume" numeric NOT NULL,
        "AxleCount" integer NOT NULL,
        "HasCargoSecuring" boolean NOT NULL,
        "BodyType" integer NOT NULL,
        "RefrigerationLevel" integer NOT NULL,
        "Status" integer NOT NULL,
        "HasMopp" boolean NOT NULL,
        "Driver" text NOT NULL,
        "CurrentLocation" text NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "Version" integer NOT NULL,
        CONSTRAINT "PK_Vehicle" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260907192403_InitialCreate') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260907192403_InitialCreate', '10.0.10');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260908212302_AddAuthenticationEntities') THEN
    CREATE TABLE "Carrier" (
        "Id" uuid NOT NULL,
        "Cnpj" text NOT NULL,
        "CompanyName" text NOT NULL,
        "TradeName" text,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "Version" integer NOT NULL,
        CONSTRAINT "PK_Carrier" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260908212302_AddAuthenticationEntities') THEN
    CREATE TABLE "Contractor" (
        "Id" uuid NOT NULL,
        "Cnpj" text NOT NULL,
        "CompanyName" text NOT NULL,
        "TradeName" text,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "Version" integer NOT NULL,
        CONSTRAINT "PK_Contractor" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260908212302_AddAuthenticationEntities') THEN
    CREATE TABLE "PartnerConnection" (
        "Id" uuid NOT NULL,
        "ContractorId" uuid NOT NULL,
        "CarrierId" uuid NOT NULL,
        "Status" text NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "Version" integer NOT NULL,
        CONSTRAINT "PK_PartnerConnection" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260908212302_AddAuthenticationEntities') THEN
    CREATE TABLE "PartnershipInvite" (
        "Id" uuid NOT NULL,
        "Token" text NOT NULL,
        "ContractorId" uuid NOT NULL,
        "ExpiresAt" timestamp with time zone,
        "IsUsed" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "Version" integer NOT NULL,
        CONSTRAINT "PK_PartnershipInvite" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260908212302_AddAuthenticationEntities') THEN
    CREATE TABLE "User" (
        "Id" uuid NOT NULL,
        "Email" text NOT NULL,
        "PasswordHash" text NOT NULL,
        "ProfileType" text NOT NULL,
        "CompanyId" uuid,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "Version" integer NOT NULL,
        CONSTRAINT "PK_User" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260908212302_AddAuthenticationEntities') THEN
    CREATE UNIQUE INDEX "IX_Carrier_Cnpj" ON "Carrier" ("Cnpj");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260908212302_AddAuthenticationEntities') THEN
    CREATE UNIQUE INDEX "IX_Contractor_Cnpj" ON "Contractor" ("Cnpj");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260908212302_AddAuthenticationEntities') THEN
    CREATE UNIQUE INDEX "IX_PartnerConnection_ContractorId_CarrierId" ON "PartnerConnection" ("ContractorId", "CarrierId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260908212302_AddAuthenticationEntities') THEN
    CREATE INDEX "IX_PartnershipInvite_ContractorId" ON "PartnershipInvite" ("ContractorId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260908212302_AddAuthenticationEntities') THEN
    CREATE UNIQUE INDEX "IX_PartnershipInvite_Token" ON "PartnershipInvite" ("Token");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260908212302_AddAuthenticationEntities') THEN
    CREATE UNIQUE INDEX "IX_User_Email" ON "User" ("Email");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260908212302_AddAuthenticationEntities') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260908212302_AddAuthenticationEntities', '10.0.10');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260910011957_AddGoogleAuthToUser') THEN
    ALTER TABLE "User" ALTER COLUMN "PasswordHash" DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260910011957_AddGoogleAuthToUser') THEN
    ALTER TABLE "User" ADD "AuthProvider" text NOT NULL DEFAULT 'Local';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260910011957_AddGoogleAuthToUser') THEN
    ALTER TABLE "User" ADD "GoogleId" text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260910011957_AddGoogleAuthToUser') THEN
    CREATE UNIQUE INDEX "IX_User_GoogleId" ON "User" ("GoogleId") WHERE "GoogleId" IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260910011957_AddGoogleAuthToUser') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260910011957_AddGoogleAuthToUser', '10.0.10');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260910153706_AddPartnershipInviteInviteeEmail') THEN
    ALTER TABLE "PartnershipInvite" ADD "InviteeEmail" character varying(320);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260910153706_AddPartnershipInviteInviteeEmail') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260910153706_AddPartnershipInviteInviteeEmail', '10.0.10');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260915141016_AddRouteSegmentAndProductRouteSegment') THEN
    CREATE TABLE "RouteSegment" (
        "Id" uuid NOT NULL,
        "ContractorId" uuid NOT NULL,
        "RouteId" uuid,
        "OriginAddress" text NOT NULL,
        "DestinationAddress" text NOT NULL,
        "DistanceKm" double precision NOT NULL,
        "EstimatedTimeHours" double precision NOT NULL,
        "OriginCoordinate" text NOT NULL,
        "DestinationCoordinate" text NOT NULL,
        "BudgetCeiling" numeric,
        "EstimatedTollCost" numeric NOT NULL,
        "PickupDeadline" timestamp with time zone NOT NULL,
        "DeliveryDeadline" timestamp with time zone NOT NULL,
        "Status" text NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "Version" integer NOT NULL,
        CONSTRAINT "PK_RouteSegment" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260915141016_AddRouteSegmentAndProductRouteSegment') THEN
    CREATE TABLE "Vehicle" (
        "Id" uuid NOT NULL,
        "TransportadoraId" uuid NOT NULL,
        "Plate" text NOT NULL,
        "Model" text NOT NULL,
        "CapacityWeight" numeric NOT NULL,
        "CapacityVolume" numeric NOT NULL,
        "AxleCount" integer NOT NULL,
        "HasCargoSecuring" boolean NOT NULL,
        "BodyType" integer NOT NULL,
        "RefrigerationLevel" integer NOT NULL,
        "Status" integer NOT NULL,
        "HasMopp" boolean NOT NULL,
        "Driver" text NOT NULL,
        "CurrentLocation" text NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "Version" integer NOT NULL,
        CONSTRAINT "PK_Vehicle" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260915141016_AddRouteSegmentAndProductRouteSegment') THEN
    CREATE TABLE "ProductRouteSegment" (
        "Id" uuid NOT NULL,
        "RouteSegmentId" uuid NOT NULL,
        "ProductId" uuid NOT NULL,
        "Quantity" integer NOT NULL,
        CONSTRAINT "PK_ProductRouteSegment" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_ProductRouteSegment_Product_ProductId" FOREIGN KEY ("ProductId") REFERENCES "Product" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_ProductRouteSegment_RouteSegment_RouteSegmentId" FOREIGN KEY ("RouteSegmentId") REFERENCES "RouteSegment" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260915141016_AddRouteSegmentAndProductRouteSegment') THEN
    CREATE INDEX "IX_ProductRouteSegment_ProductId" ON "ProductRouteSegment" ("ProductId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260915141016_AddRouteSegmentAndProductRouteSegment') THEN
    CREATE INDEX "IX_ProductRouteSegment_RouteSegmentId" ON "ProductRouteSegment" ("RouteSegmentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260915141016_AddRouteSegmentAndProductRouteSegment') THEN
    CREATE INDEX "IX_RouteSegment_ContractorId" ON "RouteSegment" ("ContractorId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260915141016_AddRouteSegmentAndProductRouteSegment') THEN
    CREATE INDEX "IX_RouteSegment_Status" ON "RouteSegment" ("Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260915141016_AddRouteSegmentAndProductRouteSegment') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260915141016_AddRouteSegmentAndProductRouteSegment', '10.0.10');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260915143735_AddConsolidatedRouteAndAuction') THEN
    CREATE TABLE "ConsolidatedRoute" (
        "Id" uuid NOT NULL,
        "ContractorId" uuid NOT NULL,
        "Status" text NOT NULL,
        "TotalDistanceKm" double precision NOT NULL,
        "EstimatedTimeHours" double precision NOT NULL,
        "ConsolidatedCeiling" numeric NOT NULL,
        "EstimatedAnttFloor" numeric NOT NULL,
        "TotalWeightKg" double precision NOT NULL,
        "TotalVolumeM3" double precision NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "Version" integer NOT NULL,
        CONSTRAINT "PK_ConsolidatedRoute" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260915143735_AddConsolidatedRouteAndAuction') THEN
    CREATE TABLE "Auction" (
        "Id" uuid NOT NULL,
        "RouteId" uuid NOT NULL,
        "OpenedAt" timestamp with time zone NOT NULL,
        "ExpiresAt" timestamp with time zone NOT NULL,
        "AutomaticAward" boolean NOT NULL,
        "Status" text NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "Version" integer NOT NULL,
        CONSTRAINT "PK_Auction" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_Auction_ConsolidatedRoute_RouteId" FOREIGN KEY ("RouteId") REFERENCES "ConsolidatedRoute" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260915143735_AddConsolidatedRouteAndAuction') THEN
    CREATE UNIQUE INDEX "IX_Auction_RouteId" ON "Auction" ("RouteId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260915143735_AddConsolidatedRouteAndAuction') THEN
    CREATE INDEX "IX_Auction_Status" ON "Auction" ("Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260915143735_AddConsolidatedRouteAndAuction') THEN
    CREATE INDEX "IX_ConsolidatedRoute_ContractorId" ON "ConsolidatedRoute" ("ContractorId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260915143735_AddConsolidatedRouteAndAuction') THEN
    CREATE INDEX "IX_ConsolidatedRoute_Status" ON "ConsolidatedRoute" ("Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260915143735_AddConsolidatedRouteAndAuction') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260915143735_AddConsolidatedRouteAndAuction', '10.0.10');
    END IF;
END $EF$;
COMMIT;

