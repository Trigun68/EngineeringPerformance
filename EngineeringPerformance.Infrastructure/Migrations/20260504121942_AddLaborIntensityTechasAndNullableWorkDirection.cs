using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EngineeringPerformance.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLaborIntensityTechasAndNullableWorkDirection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProjectLaborIntensities_ProjectAnalysisVersionId_ProjectId_DocumentationCode_WorkDirectionId",
                table: "ProjectLaborIntensities");

            migrationBuilder.AlterColumn<int>(
                name: "WorkDirectionId",
                table: "ProjectLaborIntensities",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "Techas",
                table: "LaborIntensities",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectLaborIntensities_ProjectAnalysisVersionId_ProjectId_DocumentationCode_WorkDirectionId",
                table: "ProjectLaborIntensities",
                columns: new[] { "ProjectAnalysisVersionId", "ProjectId", "DocumentationCode", "WorkDirectionId" },
                unique: true,
                filter: "[WorkDirectionId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProjectLaborIntensities_ProjectAnalysisVersionId_ProjectId_DocumentationCode_WorkDirectionId",
                table: "ProjectLaborIntensities");

            migrationBuilder.DropColumn(
                name: "Techas",
                table: "LaborIntensities");

            migrationBuilder.AlterColumn<int>(
                name: "WorkDirectionId",
                table: "ProjectLaborIntensities",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectLaborIntensities_ProjectAnalysisVersionId_ProjectId_DocumentationCode_WorkDirectionId",
                table: "ProjectLaborIntensities",
                columns: new[] { "ProjectAnalysisVersionId", "ProjectId", "DocumentationCode", "WorkDirectionId" },
                unique: true);
        }
    }
}
