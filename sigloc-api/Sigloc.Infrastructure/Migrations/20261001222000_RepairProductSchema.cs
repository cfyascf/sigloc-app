using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sigloc.Infrastructure.Migrations;

/// <summary>
/// Repairs columns that were present in the EF model snapshot but absent from the
/// historical migration chain. The monitoring refresh expands product navigation
/// data, so a database upgraded solely through migrations must contain them.
/// </summary>
[DbContext(typeof(Sigloc.Infrastructure.Contexts.SiglocDbContext))]
[Migration("20261001222000_RepairProductSchema")]
public partial class RepairProductSchema : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE "Product"
              ADD COLUMN IF NOT EXISTS "ContractorId" uuid,
              ADD COLUMN IF NOT EXISTS "Name" text NOT NULL DEFAULT '',
              ADD COLUMN IF NOT EXISTS "Type" text,
              ADD COLUMN IF NOT EXISTS "Category" text NOT NULL DEFAULT 'General',
              ADD COLUMN IF NOT EXISTS "TransportEnvironment" text NOT NULL DEFAULT 'Dry',
              ADD COLUMN IF NOT EXISTS "TempMin" double precision,
              ADD COLUMN IF NOT EXISTS "TempMax" double precision,
              ADD COLUMN IF NOT EXISTS "PackagingType" text,
              ADD COLUMN IF NOT EXISTS "Dangerous" boolean NOT NULL DEFAULT false,
              ADD COLUMN IF NOT EXISTS "Fragile" boolean NOT NULL DEFAULT false,
              ADD COLUMN IF NOT EXISTS "DefaultWeight" double precision NOT NULL DEFAULT 0,
              ADD COLUMN IF NOT EXISTS "DefaultVolume" double precision NOT NULL DEFAULT 0,
              ADD COLUMN IF NOT EXISTS "HandlingRestriction" text;
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_Product_ContractorId_Sku" ON "Product" ("ContractorId", "Sku");
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // The columns can contain production data once repaired; rollback preserves it.
    }
}
