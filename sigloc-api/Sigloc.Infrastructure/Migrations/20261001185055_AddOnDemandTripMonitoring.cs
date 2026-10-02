using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sigloc.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOnDemandTripMonitoring : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Trip_RouteId",
                table: "Trip");

            migrationBuilder.AddColumn<string>(
                name: "DriverPhone",
                table: "Vehicle",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "TraccarDeviceId",
                table: "Vehicle",
                type: "bigint",
                nullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "LastCalculatedEta",
                table: "TripMonitoring",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<long>(
                name: "LastDeviceId",
                table: "TripMonitoring",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastObservationId",
                table: "TripMonitoring",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastSuccessfulCalculationAt",
                table: "TripMonitoring",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "TripMonitoring",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "TripMonitoring",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "NextStopDeadline",
                table: "TripMonitoring",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "NextStopId",
                table: "TripMonitoring",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "RemainingDistanceKm",
                table: "TripMonitoring",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Risk",
                table: "TripMonitoring",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "TraveledDistanceKm",
                table: "TripMonitoring",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RouteSequence",
                table: "RouteSegment",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TripMonitoringEvent",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TripId = table.Column<Guid>(type: "uuid", nullable: false),
                    RefreshId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TripMonitoringEvent", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TripMonitoringEvent_Trip_TripId",
                        column: x => x.TripId,
                        principalTable: "Trip",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TripStop",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TripId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sequence = table.Column<int>(type: "integer", nullable: false),
                    City = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    State = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Latitude = table.Column<double>(type: "double precision", nullable: false),
                    Longitude = table.Column<double>(type: "double precision", nullable: false),
                    IsCompleted = table.Column<bool>(type: "boolean", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletionSource = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TripStop", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TripStop_Trip_TripId",
                        column: x => x.TripId,
                        principalTable: "Trip",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TripTelemetry",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TripId = table.Column<Guid>(type: "uuid", nullable: false),
                    ObservationId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    DeviceId = table.Column<long>(type: "bigint", nullable: false),
                    Latitude = table.Column<double>(type: "double precision", nullable: false),
                    Longitude = table.Column<double>(type: "double precision", nullable: false),
                    FixTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CalculatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TripTelemetry", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TripTelemetry_Trip_TripId",
                        column: x => x.TripId,
                        principalTable: "Trip",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TripStopAction",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TripStopId = table.Column<Guid>(type: "uuid", nullable: false),
                    SegmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProductName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Deadline = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsCompleted = table.Column<bool>(type: "boolean", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletionSource = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TripStopAction", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TripStopAction_RouteSegment_SegmentId",
                        column: x => x.SegmentId,
                        principalTable: "RouteSegment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TripStopAction_TripStop_TripStopId",
                        column: x => x.TripStopId,
                        principalTable: "TripStop",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Trip_RouteId_Status_CreatedAt",
                table: "Trip",
                columns: new[] { "RouteId", "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TripMonitoringEvent_TripId_OccurredAt",
                table: "TripMonitoringEvent",
                columns: new[] { "TripId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TripMonitoringEvent_TripId_RefreshId_Kind",
                table: "TripMonitoringEvent",
                columns: new[] { "TripId", "RefreshId", "Kind" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TripStop_TripId_Sequence",
                table: "TripStop",
                columns: new[] { "TripId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TripStopAction_SegmentId",
                table: "TripStopAction",
                column: "SegmentId");

            migrationBuilder.CreateIndex(
                name: "IX_TripStopAction_TripStopId_SegmentId_Kind_ProductId",
                table: "TripStopAction",
                columns: new[] { "TripStopId", "SegmentId", "Kind", "ProductId" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);

            migrationBuilder.CreateIndex(
                name: "IX_TripTelemetry_TripId_DeviceId_ObservationId",
                table: "TripTelemetry",
                columns: new[] { "TripId", "DeviceId", "ObservationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TripTelemetry_TripId_FixTime",
                table: "TripTelemetry",
                columns: new[] { "TripId", "FixTime" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TripMonitoringEvent");

            migrationBuilder.DropTable(
                name: "TripStopAction");

            migrationBuilder.DropTable(
                name: "TripTelemetry");

            migrationBuilder.DropTable(
                name: "TripStop");

            migrationBuilder.DropIndex(
                name: "IX_Trip_RouteId_Status_CreatedAt",
                table: "Trip");

            migrationBuilder.DropColumn(
                name: "DriverPhone",
                table: "Vehicle");

            migrationBuilder.DropColumn(
                name: "TraccarDeviceId",
                table: "Vehicle");

            migrationBuilder.DropColumn(
                name: "LastDeviceId",
                table: "TripMonitoring");

            migrationBuilder.DropColumn(
                name: "LastObservationId",
                table: "TripMonitoring");

            migrationBuilder.DropColumn(
                name: "LastSuccessfulCalculationAt",
                table: "TripMonitoring");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "TripMonitoring");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "TripMonitoring");

            migrationBuilder.DropColumn(
                name: "NextStopDeadline",
                table: "TripMonitoring");

            migrationBuilder.DropColumn(
                name: "NextStopId",
                table: "TripMonitoring");

            migrationBuilder.DropColumn(
                name: "RemainingDistanceKm",
                table: "TripMonitoring");

            migrationBuilder.DropColumn(
                name: "Risk",
                table: "TripMonitoring");

            migrationBuilder.DropColumn(
                name: "TraveledDistanceKm",
                table: "TripMonitoring");

            migrationBuilder.DropColumn(
                name: "RouteSequence",
                table: "RouteSegment");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "LastCalculatedEta",
                table: "TripMonitoring",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)),
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Trip_RouteId",
                table: "Trip",
                column: "RouteId");
        }
    }
}
