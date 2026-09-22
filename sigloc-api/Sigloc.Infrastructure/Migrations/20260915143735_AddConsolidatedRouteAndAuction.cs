using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sigloc.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddConsolidatedRouteAndAuction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConsolidatedRoute",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    TotalDistanceKm = table.Column<double>(type: "double precision", nullable: false),
                    EstimatedTimeHours = table.Column<double>(type: "double precision", nullable: false),
                    ConsolidatedCeiling = table.Column<decimal>(type: "numeric", nullable: false),
                    EstimatedAnttFloor = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalWeightKg = table.Column<double>(type: "double precision", nullable: false),
                    TotalVolumeM3 = table.Column<double>(type: "double precision", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsolidatedRoute", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Auction",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RouteId = table.Column<Guid>(type: "uuid", nullable: false),
                    OpenedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AutomaticAward = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Auction", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Auction_ConsolidatedRoute_RouteId",
                        column: x => x.RouteId,
                        principalTable: "ConsolidatedRoute",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Auction_RouteId",
                table: "Auction",
                column: "RouteId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Auction_Status",
                table: "Auction",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ConsolidatedRoute_ContractorId",
                table: "ConsolidatedRoute",
                column: "ContractorId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsolidatedRoute_Status",
                table: "ConsolidatedRoute",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Auction");

            migrationBuilder.DropTable(
                name: "ConsolidatedRoute");
        }
    }
}
