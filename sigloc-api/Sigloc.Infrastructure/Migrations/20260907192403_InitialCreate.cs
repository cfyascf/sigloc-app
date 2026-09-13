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
                    Sku = table.Column<string>(type: "text", nullable: false),
                    PackageType = table.Column<string>(type: "text", nullable: true),
                    Temperature = table.Column<string>(type: "text", nullable: true),
                    HandlingRestrictions = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Product", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Vehicle",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TransportadoraId = table.Column<Guid>(type: "uuid", nullable: false),
                    Plate = table.Column<string>(type: "text", nullable: false),
                    Model = table.Column<string>(type: "text", nullable: false),
                    CapacityWeight = table.Column<decimal>(type: "numeric", nullable: false),
                    CapacityVolume = table.Column<decimal>(type: "numeric", nullable: false),
                    AxleCount = table.Column<int>(type: "integer", nullable: false),
                    HasCargoSecuring = table.Column<bool>(type: "boolean", nullable: false),
                    BodyType = table.Column<int>(type: "integer", nullable: false),
                    RefrigerationLevel = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    HasMopp = table.Column<bool>(type: "boolean", nullable: false),
                    Driver = table.Column<string>(type: "text", nullable: false),
                    CurrentLocation = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vehicle", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Product");

            migrationBuilder.DropTable(
                name: "Vehicle");
        }
    }
}
