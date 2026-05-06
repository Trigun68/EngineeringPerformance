using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EngineeringPerformance.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectPhysicalVolumeDetailsAndLaborIntensityBrand : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BindingType",
                table: "ProjectPhysicalVolumes",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CatalogId",
                table: "ProjectPhysicalVolumes",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DocumentSet",
                table: "ProjectPhysicalVolumes",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EstimationBasis",
                table: "ProjectPhysicalVolumes",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FacilityCode",
                table: "ProjectPhysicalVolumes",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FacilityName",
                table: "ProjectPhysicalVolumes",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "LaborHoursPerUnit",
                table: "ProjectPhysicalVolumes",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MachineHoursPerUnit",
                table: "ProjectPhysicalVolumes",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "ProjectPhysicalVolumes",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Tag",
                table: "ProjectPhysicalVolumes",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalMachineHours",
                table: "ProjectPhysicalVolumes",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WorkComment",
                table: "ProjectPhysicalVolumes",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Brand",
                table: "ProjectLaborIntensities",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BindingType",
                table: "ProjectPhysicalVolumes");

            migrationBuilder.DropColumn(
                name: "CatalogId",
                table: "ProjectPhysicalVolumes");

            migrationBuilder.DropColumn(
                name: "DocumentSet",
                table: "ProjectPhysicalVolumes");

            migrationBuilder.DropColumn(
                name: "EstimationBasis",
                table: "ProjectPhysicalVolumes");

            migrationBuilder.DropColumn(
                name: "FacilityCode",
                table: "ProjectPhysicalVolumes");

            migrationBuilder.DropColumn(
                name: "FacilityName",
                table: "ProjectPhysicalVolumes");

            migrationBuilder.DropColumn(
                name: "LaborHoursPerUnit",
                table: "ProjectPhysicalVolumes");

            migrationBuilder.DropColumn(
                name: "MachineHoursPerUnit",
                table: "ProjectPhysicalVolumes");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "ProjectPhysicalVolumes");

            migrationBuilder.DropColumn(
                name: "Tag",
                table: "ProjectPhysicalVolumes");

            migrationBuilder.DropColumn(
                name: "TotalMachineHours",
                table: "ProjectPhysicalVolumes");

            migrationBuilder.DropColumn(
                name: "WorkComment",
                table: "ProjectPhysicalVolumes");

            migrationBuilder.DropColumn(
                name: "Brand",
                table: "ProjectLaborIntensities");
        }
    }
}
