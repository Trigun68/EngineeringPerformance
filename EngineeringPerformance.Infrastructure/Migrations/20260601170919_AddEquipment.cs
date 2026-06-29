using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EngineeringPerformance.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEquipment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Equipments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    ClassifierId = table.Column<int>(type: "int", nullable: true),
                    IsAssessment = table.Column<bool>(type: "bit", nullable: true),
                    Coefficient = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    Name = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ParentId = table.Column<int>(type: "int", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsArchived = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Equipments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Equipments_Equipments_ParentId",
                        column: x => x.ParentId,
                        principalTable: "Equipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Equipments_IicWorkDirections_ClassifierId",
                        column: x => x.ClassifierId,
                        principalTable: "IicWorkDirections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ProjectEquipments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    EquipmentId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    QuantityInWork = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    PositionNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    FacilityCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FacilityName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DocumentSet = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DiagramNumber = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ShortName = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CatalogName = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    TypeMark = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CatalogId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Tag = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FullEquipmentId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SupplyStandard = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    OlReference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    EstimationBasis = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ProjectAnalysisVersionId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectEquipments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectEquipments_Equipments_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "Equipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectEquipments_ProjectAnalysisVersions_ProjectAnalysisVersionId",
                        column: x => x.ProjectAnalysisVersionId,
                        principalTable: "ProjectAnalysisVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectEquipments_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Equipments_ClassifierId",
                table: "Equipments",
                column: "ClassifierId");

            migrationBuilder.CreateIndex(
                name: "IX_Equipments_IsArchived",
                table: "Equipments",
                column: "IsArchived");

            migrationBuilder.CreateIndex(
                name: "IX_Equipments_IsArchived_ParentId_SortOrder",
                table: "Equipments",
                columns: new[] { "IsArchived", "ParentId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_Equipments_IsAssessment",
                table: "Equipments",
                column: "IsAssessment");

            migrationBuilder.CreateIndex(
                name: "IX_Equipments_Name",
                table: "Equipments",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Equipments_ParentId",
                table: "Equipments",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectEquipments_EquipmentId",
                table: "ProjectEquipments",
                column: "EquipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectEquipments_FacilityCode",
                table: "ProjectEquipments",
                column: "FacilityCode");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectEquipments_ProjectAnalysisVersionId",
                table: "ProjectEquipments",
                column: "ProjectAnalysisVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectEquipments_ProjectId",
                table: "ProjectEquipments",
                column: "ProjectId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProjectEquipments");

            migrationBuilder.DropTable(
                name: "Equipments");
        }
    }
}
