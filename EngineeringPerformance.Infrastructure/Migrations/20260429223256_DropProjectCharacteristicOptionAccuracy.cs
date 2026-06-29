using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EngineeringPerformance.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DropProjectCharacteristicOptionAccuracy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ComparisonAccuracy",
                table: "ProjectCharacteristicOptions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ComparisonAccuracy",
                table: "ProjectCharacteristicOptions",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }
    }
}
