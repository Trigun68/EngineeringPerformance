using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EngineeringPerformance.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIndexesForPerformance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_PhysicalVolumes_IsArchived",
                table: "PhysicalVolumes",
                column: "IsArchived");

            migrationBuilder.CreateIndex(
                name: "IX_IicWorkDirections_IsArchived",
                table: "IicWorkDirections",
                column: "IsArchived");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PhysicalVolumes_IsArchived",
                table: "PhysicalVolumes");

            migrationBuilder.DropIndex(
                name: "IX_IicWorkDirections_IsArchived",
                table: "IicWorkDirections");
        }
    }
}
