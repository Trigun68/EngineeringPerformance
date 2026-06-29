using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EngineeringPerformance.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSortOrderToDirectoryItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SortOrder",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SortOrder",
                table: "UnitsOfMeasurement",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SortOrder",
                table: "Roles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SortOrder",
                table: "Projects",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SortOrder",
                table: "ProjectAnalysisVersions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SortOrder",
                table: "PhysicalVolumeTypes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SortOrder",
                table: "PhysicalVolumes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SortOrder",
                table: "IicWorkDirections",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SortOrder",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SortOrder",
                table: "UnitsOfMeasurement");

            migrationBuilder.DropColumn(
                name: "SortOrder",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "SortOrder",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "SortOrder",
                table: "ProjectAnalysisVersions");

            migrationBuilder.DropColumn(
                name: "SortOrder",
                table: "PhysicalVolumeTypes");

            migrationBuilder.DropColumn(
                name: "SortOrder",
                table: "PhysicalVolumes");

            migrationBuilder.DropColumn(
                name: "SortOrder",
                table: "IicWorkDirections");
        }
    }
}
