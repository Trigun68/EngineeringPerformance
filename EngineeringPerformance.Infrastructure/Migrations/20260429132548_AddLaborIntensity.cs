using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EngineeringPerformance.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLaborIntensity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LaborIntensities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NormPpr = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    NormOpr = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    NormPd = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    NormRd = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    IicWorkDirectionId = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ParentId = table.Column<int>(type: "int", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsArchived = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LaborIntensities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LaborIntensities_IicWorkDirections_IicWorkDirectionId",
                        column: x => x.IicWorkDirectionId,
                        principalTable: "IicWorkDirections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_LaborIntensities_LaborIntensities_ParentId",
                        column: x => x.ParentId,
                        principalTable: "LaborIntensities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LaborIntensities_Code",
                table: "LaborIntensities",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_LaborIntensities_IicWorkDirectionId",
                table: "LaborIntensities",
                column: "IicWorkDirectionId");

            migrationBuilder.CreateIndex(
                name: "IX_LaborIntensities_IsArchived",
                table: "LaborIntensities",
                column: "IsArchived");

            migrationBuilder.CreateIndex(
                name: "IX_LaborIntensities_ParentId",
                table: "LaborIntensities",
                column: "ParentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LaborIntensities");
        }
    }
}
