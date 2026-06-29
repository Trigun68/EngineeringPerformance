using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EngineeringPerformance.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCodeToPhysicalVolumeType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "PhysicalVolumeTypes",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Code",
                table: "PhysicalVolumeTypes");
        }
    }
}
