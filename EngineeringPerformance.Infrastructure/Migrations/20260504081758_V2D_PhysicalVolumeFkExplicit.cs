using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EngineeringPerformance.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class V2D_PhysicalVolumeFkExplicit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectLaborIntensities_PhysicalVolumes_PhysicalVolumeId",
                table: "ProjectLaborIntensities");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectLaborIntensities_PhysicalVolumes_PhysicalVolumeId",
                table: "ProjectLaborIntensities",
                column: "PhysicalVolumeId",
                principalTable: "PhysicalVolumes",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectLaborIntensities_PhysicalVolumes_PhysicalVolumeId",
                table: "ProjectLaborIntensities");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectLaborIntensities_PhysicalVolumes_PhysicalVolumeId",
                table: "ProjectLaborIntensities",
                column: "PhysicalVolumeId",
                principalTable: "PhysicalVolumes",
                principalColumn: "Id");
        }
    }
}
