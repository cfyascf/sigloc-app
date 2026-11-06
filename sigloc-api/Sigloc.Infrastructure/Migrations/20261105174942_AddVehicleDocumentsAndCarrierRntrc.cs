using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sigloc.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVehicleDocumentsAndCarrierRntrc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "CivCippValidUntil",
                table: "Vehicle",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Composition",
                table: "Vehicle",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DriverCnhCategory",
                table: "Vehicle",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "DriverHasEar",
                table: "Vehicle",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "DriverToxicologicalExamValidUntil",
                table: "Vehicle",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "MoppValidUntil",
                table: "Vehicle",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RntrcCategory",
                table: "Vehicle",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RntrcHolderDocument",
                table: "Vehicle",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RntrcHolderName",
                table: "Vehicle",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RntrcNumber",
                table: "Vehicle",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RntrcSource",
                table: "Vehicle",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "RntrcValidUntil",
                table: "Vehicle",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RntrcCategory",
                table: "Carrier",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RntrcNumber",
                table: "Carrier",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "RntrcValidUntil",
                table: "Carrier",
                type: "date",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CivCippValidUntil",
                table: "Vehicle");

            migrationBuilder.DropColumn(
                name: "Composition",
                table: "Vehicle");

            migrationBuilder.DropColumn(
                name: "DriverCnhCategory",
                table: "Vehicle");

            migrationBuilder.DropColumn(
                name: "DriverHasEar",
                table: "Vehicle");

            migrationBuilder.DropColumn(
                name: "DriverToxicologicalExamValidUntil",
                table: "Vehicle");

            migrationBuilder.DropColumn(
                name: "MoppValidUntil",
                table: "Vehicle");

            migrationBuilder.DropColumn(
                name: "RntrcCategory",
                table: "Vehicle");

            migrationBuilder.DropColumn(
                name: "RntrcHolderDocument",
                table: "Vehicle");

            migrationBuilder.DropColumn(
                name: "RntrcHolderName",
                table: "Vehicle");

            migrationBuilder.DropColumn(
                name: "RntrcNumber",
                table: "Vehicle");

            migrationBuilder.DropColumn(
                name: "RntrcSource",
                table: "Vehicle");

            migrationBuilder.DropColumn(
                name: "RntrcValidUntil",
                table: "Vehicle");

            migrationBuilder.DropColumn(
                name: "RntrcCategory",
                table: "Carrier");

            migrationBuilder.DropColumn(
                name: "RntrcNumber",
                table: "Carrier");

            migrationBuilder.DropColumn(
                name: "RntrcValidUntil",
                table: "Carrier");
        }
    }
}
