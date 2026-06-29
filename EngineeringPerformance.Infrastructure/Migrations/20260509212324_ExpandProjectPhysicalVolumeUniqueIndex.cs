using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EngineeringPerformance.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ExpandProjectPhysicalVolumeUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProjectPhysicalVolumes_ProjectAnalysisVersionId_ProjectId_PhysicalVolumeId",
                table: "ProjectPhysicalVolumes");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectPhysicalVolumes_ProjectAnalysisVersionId_ProjectId_PhysicalVolumeId_FacilityCode_FacilityName_WorkId",
                table: "ProjectPhysicalVolumes",
                columns: new[] { "ProjectAnalysisVersionId", "ProjectId", "PhysicalVolumeId", "FacilityCode", "FacilityName", "WorkId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProjectPhysicalVolumes_ProjectAnalysisVersionId_ProjectId_PhysicalVolumeId_FacilityCode_FacilityName_WorkId",
                table: "ProjectPhysicalVolumes");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectPhysicalVolumes_ProjectAnalysisVersionId_ProjectId_PhysicalVolumeId",
                table: "ProjectPhysicalVolumes",
                columns: new[] { "ProjectAnalysisVersionId", "ProjectId", "PhysicalVolumeId" },
                unique: true);
        }
    }
}
