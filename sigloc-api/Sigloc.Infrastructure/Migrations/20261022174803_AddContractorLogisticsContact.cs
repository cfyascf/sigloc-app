using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sigloc.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddContractorLogisticsContact : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LogisticsContactEmail",
                table: "Contractor",
                type: "character varying(254)",
                maxLength: 254,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LogisticsContactName",
                table: "Contractor",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LogisticsContactPhone",
                table: "Contractor",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LogisticsContactEmail",
                table: "Contractor");

            migrationBuilder.DropColumn(
                name: "LogisticsContactName",
                table: "Contractor");

            migrationBuilder.DropColumn(
                name: "LogisticsContactPhone",
                table: "Contractor");
        }
    }
}
