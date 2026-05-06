using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EngineeringPerformance.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIsAssessmentToPhysicalVolume : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsAssessment",
                table: "PhysicalVolumes",
                type: "bit",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PhysicalVolumes_IsAssessment",
                table: "PhysicalVolumes",
                column: "IsAssessment");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PhysicalVolumes_IsAssessment",
                table: "PhysicalVolumes");

            migrationBuilder.DropColumn(
                name: "IsAssessment",
                table: "PhysicalVolumes");
        }
    }
}
