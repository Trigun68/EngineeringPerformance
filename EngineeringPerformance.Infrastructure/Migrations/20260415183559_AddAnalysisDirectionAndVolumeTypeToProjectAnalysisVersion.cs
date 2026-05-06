using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EngineeringPerformance.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAnalysisDirectionAndVolumeTypeToProjectAnalysisVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Direction",
                table: "ProjectAnalysisVersions",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "PhysicalVolumeTypeId",
                table: "ProjectAnalysisVersions",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectAnalysisVersions_PhysicalVolumeTypeId",
                table: "ProjectAnalysisVersions",
                column: "PhysicalVolumeTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectAnalysisVersions_PhysicalVolumeTypes_PhysicalVolumeTypeId",
                table: "ProjectAnalysisVersions",
                column: "PhysicalVolumeTypeId",
                principalTable: "PhysicalVolumeTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectAnalysisVersions_PhysicalVolumeTypes_PhysicalVolumeTypeId",
                table: "ProjectAnalysisVersions");

            migrationBuilder.DropIndex(
                name: "IX_ProjectAnalysisVersions_PhysicalVolumeTypeId",
                table: "ProjectAnalysisVersions");

            migrationBuilder.DropColumn(
                name: "Direction",
                table: "ProjectAnalysisVersions");

            migrationBuilder.DropColumn(
                name: "PhysicalVolumeTypeId",
                table: "ProjectAnalysisVersions");
        }
    }
}
