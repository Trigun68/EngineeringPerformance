using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EngineeringPerformance.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPhysicalVolumeRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PhysicalVolumeId",
                table: "ProjectLaborIntensities",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DocumentSet",
                table: "PhysicalVolumes",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectPhysicalVolumes_CatalogId",
                table: "ProjectPhysicalVolumes",
                column: "CatalogId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectLaborIntensities_Brand",
                table: "ProjectLaborIntensities",
                column: "Brand");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectLaborIntensities_PhysicalVolumeId",
                table: "ProjectLaborIntensities",
                column: "PhysicalVolumeId");

            migrationBuilder.CreateIndex(
                name: "IX_PhysicalVolumes_DocumentSet",
                table: "PhysicalVolumes",
                column: "DocumentSet");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectLaborIntensities_PhysicalVolumes_PhysicalVolumeId",
                table: "ProjectLaborIntensities",
                column: "PhysicalVolumeId",
                principalTable: "PhysicalVolumes",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectLaborIntensities_PhysicalVolumes_PhysicalVolumeId",
                table: "ProjectLaborIntensities");

            migrationBuilder.DropIndex(
                name: "IX_ProjectPhysicalVolumes_CatalogId",
                table: "ProjectPhysicalVolumes");

            migrationBuilder.DropIndex(
                name: "IX_ProjectLaborIntensities_Brand",
                table: "ProjectLaborIntensities");

            migrationBuilder.DropIndex(
                name: "IX_ProjectLaborIntensities_PhysicalVolumeId",
                table: "ProjectLaborIntensities");

            migrationBuilder.DropIndex(
                name: "IX_PhysicalVolumes_DocumentSet",
                table: "PhysicalVolumes");

            migrationBuilder.DropColumn(
                name: "PhysicalVolumeId",
                table: "ProjectLaborIntensities");

            migrationBuilder.DropColumn(
                name: "DocumentSet",
                table: "PhysicalVolumes");
        }
    }
}
