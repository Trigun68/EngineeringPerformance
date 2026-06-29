using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EngineeringPerformance.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEquipmentUnitId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "Coefficient",
                table: "Equipments",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 1m,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,4)",
                oldPrecision: 18,
                oldScale: 4,
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UnitId",
                table: "Equipments",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Equipments_Name_UnitId",
                table: "Equipments",
                columns: new[] { "Name", "UnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_Equipments_UnitId",
                table: "Equipments",
                column: "UnitId");

            migrationBuilder.AddForeignKey(
                name: "FK_Equipments_UnitsOfMeasurement_UnitId",
                table: "Equipments",
                column: "UnitId",
                principalTable: "UnitsOfMeasurement",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Equipments_UnitsOfMeasurement_UnitId",
                table: "Equipments");

            migrationBuilder.DropIndex(
                name: "IX_Equipments_Name_UnitId",
                table: "Equipments");

            migrationBuilder.DropIndex(
                name: "IX_Equipments_UnitId",
                table: "Equipments");

            migrationBuilder.DropColumn(
                name: "UnitId",
                table: "Equipments");

            migrationBuilder.AlterColumn<decimal>(
                name: "Coefficient",
                table: "Equipments",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,4)",
                oldPrecision: 18,
                oldScale: 4,
                oldDefaultValue: 1m);
        }
    }
}
