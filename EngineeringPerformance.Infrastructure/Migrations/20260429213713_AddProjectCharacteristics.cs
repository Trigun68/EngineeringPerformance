using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EngineeringPerformance.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectCharacteristics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProjectCharacteristics",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ParentId = table.Column<int>(type: "int", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsArchived = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectCharacteristics", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProjectCharacteristicOptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CharacteristicId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ValuePercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    ComparisonAccuracy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectCharacteristicOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectCharacteristicOptions_ProjectCharacteristics_CharacteristicId",
                        column: x => x.CharacteristicId,
                        principalTable: "ProjectCharacteristics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectCharacteristicOptions_CharacteristicId",
                table: "ProjectCharacteristicOptions",
                column: "CharacteristicId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectCharacteristics_IsArchived",
                table: "ProjectCharacteristics",
                column: "IsArchived");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectCharacteristics_Name",
                table: "ProjectCharacteristics",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProjectCharacteristicOptions");

            migrationBuilder.DropTable(
                name: "ProjectCharacteristics");
        }
    }
}
