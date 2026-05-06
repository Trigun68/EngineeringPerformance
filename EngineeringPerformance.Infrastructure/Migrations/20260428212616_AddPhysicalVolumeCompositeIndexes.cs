using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EngineeringPerformance.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPhysicalVolumeCompositeIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_PhysicalVolumes_IsArchived_ParentId_SortOrder",
                table: "PhysicalVolumes",
                columns: new[] { "IsArchived", "ParentId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_PhysicalVolumes_IsArchived_VolumeTypeId",
                table: "PhysicalVolumes",
                columns: new[] { "IsArchived", "VolumeTypeId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PhysicalVolumes_IsArchived_ParentId_SortOrder",
                table: "PhysicalVolumes");

            migrationBuilder.DropIndex(
                name: "IX_PhysicalVolumes_IsArchived_VolumeTypeId",
                table: "PhysicalVolumes");
        }
    }
}
