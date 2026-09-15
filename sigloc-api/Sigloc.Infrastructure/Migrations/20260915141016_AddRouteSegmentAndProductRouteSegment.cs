using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sigloc.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRouteSegmentAndProductRouteSegment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RouteSegment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractorId = table.Column<Guid>(type: "uuid", nullable: false),
                    RouteId = table.Column<Guid>(type: "uuid", nullable: true),
                    OriginAddress = table.Column<string>(type: "text", nullable: false),
                    DestinationAddress = table.Column<string>(type: "text", nullable: false),
                    DistanceKm = table.Column<double>(type: "double precision", nullable: false),
                    EstimatedTimeHours = table.Column<double>(type: "double precision", nullable: false),
                    OriginCoordinate = table.Column<string>(type: "text", nullable: false),
                    DestinationCoordinate = table.Column<string>(type: "text", nullable: false),
                    BudgetCeiling = table.Column<decimal>(type: "numeric", nullable: true),
                    EstimatedTollCost = table.Column<decimal>(type: "numeric", nullable: false),
                    PickupDeadline = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeliveryDeadline = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RouteSegment", x => x.Id);
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

            migrationBuilder.CreateTable(
                name: "ProductRouteSegment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RouteSegmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductRouteSegment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductRouteSegment_Product_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Product",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductRouteSegment_RouteSegment_RouteSegmentId",
                        column: x => x.RouteSegmentId,
                        principalTable: "RouteSegment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductRouteSegment_ProductId",
                table: "ProductRouteSegment",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductRouteSegment_RouteSegmentId",
                table: "ProductRouteSegment",
                column: "RouteSegmentId");

            migrationBuilder.CreateIndex(
                name: "IX_RouteSegment_ContractorId",
                table: "RouteSegment",
                column: "ContractorId");

            migrationBuilder.CreateIndex(
                name: "IX_RouteSegment_Status",
                table: "RouteSegment",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductRouteSegment");

            migrationBuilder.DropTable(
                name: "Vehicle");

            migrationBuilder.DropTable(
                name: "RouteSegment");
        }
    }
}
