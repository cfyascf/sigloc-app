using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sigloc.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MergeManualInTransitVehicleStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // "Em trânsito" set by hand (OperationalStatus.EM_TRANSITO = 1) is retired: a vehicle is
            // in transit while it has an active trip, which the schedule checks already cover.
            migrationBuilder.Sql("UPDATE \"Vehicle\" SET \"Status\" = 0 WHERE \"Status\" = 1;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Data-only change: which vehicles were marked by hand is not recoverable.
        }
    }
}
