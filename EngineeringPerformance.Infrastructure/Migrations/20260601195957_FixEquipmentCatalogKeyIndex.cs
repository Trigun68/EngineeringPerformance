using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EngineeringPerformance.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixEquipmentCatalogKeyIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Equipments_Name",
                table: "Equipments");

            migrationBuilder.DropIndex(
                name: "IX_Equipments_Name_UnitId",
                table: "Equipments");

            migrationBuilder.AddColumn<string>(
                name: "CatalogKeyHash",
                table: "Equipments",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            // SHA-256(Name + CHAR(0) + UnitId) — тот же алгоритм, что EquipmentCatalogKeyHasher (UTF-8).
            migrationBuilder.Sql("""
                UPDATE e
                SET CatalogKeyHash = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(Name, CHAR(0), COALESCE(CAST(UnitId AS nvarchar(20)), ''))), 2))
                FROM Equipments e
                WHERE CatalogKeyHash = ''
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Equipments_CatalogKeyHash",
                table: "Equipments",
                column: "CatalogKeyHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Equipments_CatalogKeyHash",
                table: "Equipments");

            migrationBuilder.DropColumn(
                name: "CatalogKeyHash",
                table: "Equipments");

            migrationBuilder.CreateIndex(
                name: "IX_Equipments_Name",
                table: "Equipments",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Equipments_Name_UnitId",
                table: "Equipments",
                columns: new[] { "Name", "UnitId" });
        }
    }
}
