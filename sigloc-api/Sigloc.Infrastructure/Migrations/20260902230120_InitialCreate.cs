using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sigloc.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Product",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sku = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: true),
                    Category = table.Column<string>(type: "text", nullable: false),
                    TransportEnvironment = table.Column<string>(type: "text", nullable: false),
                    TempMin = table.Column<double>(type: "double precision", nullable: true),
                    TempMax = table.Column<double>(type: "double precision", nullable: true),
                    PackagingType = table.Column<string>(type: "text", nullable: true),
                    Dangerous = table.Column<bool>(type: "boolean", nullable: false),
                    Fragile = table.Column<bool>(type: "boolean", nullable: false),
                    DefaultWeight = table.Column<double>(type: "double precision", nullable: false),
                    DefaultVolume = table.Column<double>(type: "double precision", nullable: false),
                    HandlingRestriction = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Product", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Product_ContractorId_Sku",
                table: "Product",
                columns: new[] { "ContractorId", "Sku" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Product");
        }
    }
}
