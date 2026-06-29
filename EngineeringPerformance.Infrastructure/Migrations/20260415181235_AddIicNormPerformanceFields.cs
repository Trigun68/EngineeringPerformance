using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EngineeringPerformance.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIicNormPerformanceFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Estimate",
                table: "IicWorkDirectionNorms",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Performance",
                table: "IicWorkDirectionNorms",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "StandardDeviation",
                table: "IicWorkDirectionNorms",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Estimate",
                table: "IicWorkDirectionNorms");

            migrationBuilder.DropColumn(
                name: "Performance",
                table: "IicWorkDirectionNorms");

            migrationBuilder.DropColumn(
                name: "StandardDeviation",
                table: "IicWorkDirectionNorms");
        }
    }
}
