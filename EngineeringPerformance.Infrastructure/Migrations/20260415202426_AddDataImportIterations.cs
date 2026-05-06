using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EngineeringPerformance.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDataImportIterations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DataImportIterations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    ProjectAnalysisVersionId = table.Column<int>(type: "int", nullable: false),
                    DataType = table.Column<int>(type: "int", nullable: false),
                    ImportedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RecordCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataImportIterations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DataImportIterations_ProjectAnalysisVersions_ProjectAnalysisVersionId",
                        column: x => x.ProjectAnalysisVersionId,
                        principalTable: "ProjectAnalysisVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DataImportIterations_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DataImportIterations_ProjectAnalysisVersionId",
                table: "DataImportIterations",
                column: "ProjectAnalysisVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_DataImportIterations_ProjectAnalysisVersionId_DataType",
                table: "DataImportIterations",
                columns: new[] { "ProjectAnalysisVersionId", "DataType" });

            migrationBuilder.CreateIndex(
                name: "IX_DataImportIterations_ProjectId",
                table: "DataImportIterations",
                column: "ProjectId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DataImportIterations");
        }
    }
}
