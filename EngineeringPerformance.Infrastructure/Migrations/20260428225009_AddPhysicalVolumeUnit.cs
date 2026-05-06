using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EngineeringPerformance.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPhysicalVolumeUnit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UnitId",
                table: "PhysicalVolumes",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PhysicalVolumes_UnitId",
                table: "PhysicalVolumes",
                column: "UnitId");

            migrationBuilder.AddForeignKey(
                name: "FK_PhysicalVolumes_UnitsOfMeasurement_UnitId",
                table: "PhysicalVolumes",
                column: "UnitId",
                principalTable: "UnitsOfMeasurement",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PhysicalVolumes_UnitsOfMeasurement_UnitId",
                table: "PhysicalVolumes");

            migrationBuilder.DropIndex(
                name: "IX_PhysicalVolumes_UnitId",
                table: "PhysicalVolumes");

            migrationBuilder.DropColumn(
                name: "UnitId",
                table: "PhysicalVolumes");
        }
    }
}
