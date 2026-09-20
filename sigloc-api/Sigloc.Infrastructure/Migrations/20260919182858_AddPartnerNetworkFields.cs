using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sigloc.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPartnerNetworkFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InitiatedBy",
                table: "PartnerConnection",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "AverageRating",
                table: "Carrier",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasActiveInsurancePolicy",
                table: "Carrier",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_PartnerConnection_CarrierId",
                table: "PartnerConnection",
                column: "CarrierId");

            migrationBuilder.AddForeignKey(
                name: "FK_PartnerConnection_Carrier_CarrierId",
                table: "PartnerConnection",
                column: "CarrierId",
                principalTable: "Carrier",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PartnerConnection_Carrier_CarrierId",
                table: "PartnerConnection");

            migrationBuilder.DropIndex(
                name: "IX_PartnerConnection_CarrierId",
                table: "PartnerConnection");

            migrationBuilder.DropColumn(
                name: "InitiatedBy",
                table: "PartnerConnection");

            migrationBuilder.DropColumn(
                name: "AverageRating",
                table: "Carrier");

            migrationBuilder.DropColumn(
                name: "HasActiveInsurancePolicy",
                table: "Carrier");
        }
    }
}
