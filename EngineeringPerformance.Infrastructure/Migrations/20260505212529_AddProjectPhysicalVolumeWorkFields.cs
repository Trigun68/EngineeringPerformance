using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EngineeringPerformance.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectPhysicalVolumeWorkFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CatalogCode",
                table: "ProjectPhysicalVolumes",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CatalogName",
                table: "ProjectPhysicalVolumes",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WorkId",
                table: "ProjectPhysicalVolumes",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WorkName",
                table: "ProjectPhysicalVolumes",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CatalogCode",
                table: "ProjectPhysicalVolumes");

            migrationBuilder.DropColumn(
                name: "CatalogName",
                table: "ProjectPhysicalVolumes");

            migrationBuilder.DropColumn(
                name: "WorkId",
                table: "ProjectPhysicalVolumes");

            migrationBuilder.DropColumn(
                name: "WorkName",
                table: "ProjectPhysicalVolumes");
        }
    }
}
