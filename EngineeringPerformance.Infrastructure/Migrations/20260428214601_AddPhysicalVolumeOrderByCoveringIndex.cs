using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EngineeringPerformance.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPhysicalVolumeOrderByCoveringIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_PhysicalVolumes_IsArchived_SortOrder_Name",
                table: "PhysicalVolumes",
                columns: new[] { "IsArchived", "SortOrder", "Name" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PhysicalVolumes_IsArchived_SortOrder_Name",
                table: "PhysicalVolumes");
        }
    }
}
