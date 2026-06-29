using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EngineeringPerformance.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectCharacteristicSelections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProjectCharacteristicSelections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    CharacteristicId = table.Column<int>(type: "int", nullable: false),
                    OptionId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectCharacteristicSelections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectCharacteristicSelections_ProjectCharacteristicOptions_OptionId",
                        column: x => x.OptionId,
                        principalTable: "ProjectCharacteristicOptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectCharacteristicSelections_ProjectCharacteristics_CharacteristicId",
                        column: x => x.CharacteristicId,
                        principalTable: "ProjectCharacteristics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectCharacteristicSelections_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectCharacteristicSelections_CharacteristicId",
                table: "ProjectCharacteristicSelections",
                column: "CharacteristicId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectCharacteristicSelections_OptionId",
                table: "ProjectCharacteristicSelections",
                column: "OptionId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectCharacteristicSelections_ProjectId_CharacteristicId",
                table: "ProjectCharacteristicSelections",
                columns: new[] { "ProjectId", "CharacteristicId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProjectCharacteristicSelections");
        }
    }
}
