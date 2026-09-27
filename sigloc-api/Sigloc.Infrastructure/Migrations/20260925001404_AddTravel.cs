using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sigloc.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTravel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Travel",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LanceVencedorId = table.Column<Guid>(type: "uuid", nullable: false),
                    IniciadaEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    FinalizadaEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    PisoAnttFinal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Travel", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Monitoring",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ViagemId = table.Column<Guid>(type: "uuid", nullable: false),
                    UltimoProgressoPorcentual = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    UltimoEtaCalculado = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UltimaAtualizacaoPing = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Monitoring", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Monitoring_Travel_ViagemId",
                        column: x => x.ViagemId,
                        principalTable: "Travel",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Telemetry",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ViagemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Latitude = table.Column<double>(type: "double precision", nullable: false),
                    Longitude = table.Column<double>(type: "double precision", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Telemetry", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Telemetry_Travel_ViagemId",
                        column: x => x.ViagemId,
                        principalTable: "Travel",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Monitoring_ViagemId",
                table: "Monitoring",
                column: "ViagemId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Telemetry_ViagemId",
                table: "Telemetry",
                column: "ViagemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Monitoring");

            migrationBuilder.DropTable(
                name: "Telemetry");

            migrationBuilder.DropTable(
                name: "Travel");
        }
    }
}
