using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EngineeringPerformance.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveLaborIntensityUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProjectLaborIntensities_ProjectAnalysisVersionId_ProjectId_DocumentationCode_WorkDirectionId",
                table: "ProjectLaborIntensities");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectLaborIntensities_ProjectAnalysisVersionId_ProjectId",
                table: "ProjectLaborIntensities",
                columns: new[] { "ProjectAnalysisVersionId", "ProjectId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProjectLaborIntensities_ProjectAnalysisVersionId_ProjectId",
                table: "ProjectLaborIntensities");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectLaborIntensities_ProjectAnalysisVersionId_ProjectId_DocumentationCode_WorkDirectionId",
                table: "ProjectLaborIntensities",
                columns: new[] { "ProjectAnalysisVersionId", "ProjectId", "DocumentationCode", "WorkDirectionId" },
                unique: true,
                filter: "[WorkDirectionId] IS NOT NULL");
        }
    }
}
