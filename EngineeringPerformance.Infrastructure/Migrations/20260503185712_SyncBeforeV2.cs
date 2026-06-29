using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EngineeringPerformance.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SyncBeforeV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LaborIntensityId",
                table: "ProjectLaborIntensities",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DocumentTypeName",
                table: "LaborIntensities",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectLaborIntensities_LaborIntensityId",
                table: "ProjectLaborIntensities",
                column: "LaborIntensityId");

            migrationBuilder.CreateIndex(
                name: "IX_LaborIntensities_DocumentTypeName",
                table: "LaborIntensities",
                column: "DocumentTypeName");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectLaborIntensities_LaborIntensities_LaborIntensityId",
                table: "ProjectLaborIntensities",
                column: "LaborIntensityId",
                principalTable: "LaborIntensities",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectLaborIntensities_LaborIntensities_LaborIntensityId",
                table: "ProjectLaborIntensities");

            migrationBuilder.DropIndex(
                name: "IX_ProjectLaborIntensities_LaborIntensityId",
                table: "ProjectLaborIntensities");

            migrationBuilder.DropIndex(
                name: "IX_LaborIntensities_DocumentTypeName",
                table: "LaborIntensities");

            migrationBuilder.DropColumn(
                name: "LaborIntensityId",
                table: "ProjectLaborIntensities");

            migrationBuilder.DropColumn(
                name: "DocumentTypeName",
                table: "LaborIntensities");
        }
    }
}
