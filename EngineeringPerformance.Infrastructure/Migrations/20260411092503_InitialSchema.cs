using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EngineeringPerformance.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PhysicalVolumeTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ParentId = table.Column<int>(type: "int", nullable: true),
                    IsArchived = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhysicalVolumeTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    WorkNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ContractNumber = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CalendarPlanNumber = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Type = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Customer = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Contractor = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Gip = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    WorkStatus = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    WorkStart = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Program = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Visibility = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ParentId = table.Column<int>(type: "int", nullable: true),
                    IsArchived = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ParentId = table.Column<int>(type: "int", nullable: true),
                    IsArchived = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UnitsOfMeasurement",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ParentId = table.Column<int>(type: "int", nullable: true),
                    IsArchived = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnitsOfMeasurement", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProjectAnalysisVersions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    VersionName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ParentId = table.Column<int>(type: "int", nullable: true),
                    IsArchived = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectAnalysisVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectAnalysisVersions_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AccessRights",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    ObjectName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AccessLevel = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccessRights", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccessRights_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    WindowsUsername = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ParentId = table.Column<int>(type: "int", nullable: true),
                    IsArchived = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IicWorkDirections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    UnitId = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ParentId = table.Column<int>(type: "int", nullable: true),
                    IsArchived = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IicWorkDirections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IicWorkDirections_IicWorkDirections_ParentId",
                        column: x => x.ParentId,
                        principalTable: "IicWorkDirections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IicWorkDirections_UnitsOfMeasurement_UnitId",
                        column: x => x.UnitId,
                        principalTable: "UnitsOfMeasurement",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ProjectUserAccess",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectUserAccess", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectUserAccess_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectUserAccess_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IicWorkDirectionNorms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkDirectionId = table.Column<int>(type: "int", nullable: false),
                    NormValue = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    UnitId = table.Column<int>(type: "int", nullable: true),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IicWorkDirectionNorms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IicWorkDirectionNorms_IicWorkDirections_WorkDirectionId",
                        column: x => x.WorkDirectionId,
                        principalTable: "IicWorkDirections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IicWorkDirectionNorms_UnitsOfMeasurement_UnitId",
                        column: x => x.UnitId,
                        principalTable: "UnitsOfMeasurement",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "PhysicalVolumes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    VolumeTypeId = table.Column<int>(type: "int", nullable: false),
                    ClassifierId = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ParentId = table.Column<int>(type: "int", nullable: true),
                    IsArchived = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhysicalVolumes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PhysicalVolumes_IicWorkDirections_ClassifierId",
                        column: x => x.ClassifierId,
                        principalTable: "IicWorkDirections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PhysicalVolumes_PhysicalVolumeTypes_VolumeTypeId",
                        column: x => x.VolumeTypeId,
                        principalTable: "PhysicalVolumeTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PhysicalVolumes_PhysicalVolumes_ParentId",
                        column: x => x.ParentId,
                        principalTable: "PhysicalVolumes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProjectLaborIntensities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    DocumentationCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    WorkDirectionId = table.Column<int>(type: "int", nullable: false),
                    PlannedHours = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    ActualHours = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ProjectAnalysisVersionId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectLaborIntensities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectLaborIntensities_IicWorkDirections_WorkDirectionId",
                        column: x => x.WorkDirectionId,
                        principalTable: "IicWorkDirections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectLaborIntensities_ProjectAnalysisVersions_ProjectAnalysisVersionId",
                        column: x => x.ProjectAnalysisVersionId,
                        principalTable: "ProjectAnalysisVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectLaborIntensities_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProjectPhysicalVolumes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    PhysicalVolumeId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    ProjectAnalysisVersionId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectPhysicalVolumes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectPhysicalVolumes_PhysicalVolumes_PhysicalVolumeId",
                        column: x => x.PhysicalVolumeId,
                        principalTable: "PhysicalVolumes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectPhysicalVolumes_ProjectAnalysisVersions_ProjectAnalysisVersionId",
                        column: x => x.ProjectAnalysisVersionId,
                        principalTable: "ProjectAnalysisVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectPhysicalVolumes_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccessRights_RoleId_ObjectName",
                table: "AccessRights",
                columns: new[] { "RoleId", "ObjectName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IicWorkDirectionNorms_UnitId",
                table: "IicWorkDirectionNorms",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_IicWorkDirectionNorms_WorkDirectionId",
                table: "IicWorkDirectionNorms",
                column: "WorkDirectionId");

            migrationBuilder.CreateIndex(
                name: "IX_IicWorkDirections_Code",
                table: "IicWorkDirections",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IicWorkDirections_ParentId",
                table: "IicWorkDirections",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_IicWorkDirections_UnitId",
                table: "IicWorkDirections",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_PhysicalVolumes_ClassifierId",
                table: "PhysicalVolumes",
                column: "ClassifierId");

            migrationBuilder.CreateIndex(
                name: "IX_PhysicalVolumes_Code",
                table: "PhysicalVolumes",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_PhysicalVolumes_ParentId",
                table: "PhysicalVolumes",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_PhysicalVolumes_VolumeTypeId",
                table: "PhysicalVolumes",
                column: "VolumeTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectAnalysisVersions_ProjectId",
                table: "ProjectAnalysisVersions",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectLaborIntensities_ProjectAnalysisVersionId_ProjectId_DocumentationCode_WorkDirectionId",
                table: "ProjectLaborIntensities",
                columns: new[] { "ProjectAnalysisVersionId", "ProjectId", "DocumentationCode", "WorkDirectionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectLaborIntensities_ProjectId",
                table: "ProjectLaborIntensities",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectLaborIntensities_WorkDirectionId",
                table: "ProjectLaborIntensities",
                column: "WorkDirectionId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectPhysicalVolumes_PhysicalVolumeId",
                table: "ProjectPhysicalVolumes",
                column: "PhysicalVolumeId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectPhysicalVolumes_ProjectAnalysisVersionId_ProjectId_PhysicalVolumeId",
                table: "ProjectPhysicalVolumes",
                columns: new[] { "ProjectAnalysisVersionId", "ProjectId", "PhysicalVolumeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectPhysicalVolumes_ProjectId",
                table: "ProjectPhysicalVolumes",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_Code",
                table: "Projects",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectUserAccess_ProjectId_UserId",
                table: "ProjectUserAccess",
                columns: new[] { "ProjectId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectUserAccess_UserId",
                table: "ProjectUserAccess",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_Name",
                table: "Roles",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UnitsOfMeasurement_Name",
                table: "UnitsOfMeasurement",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_RoleId",
                table: "Users",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_WindowsUsername",
                table: "Users",
                column: "WindowsUsername",
                unique: true,
                filter: "WindowsUsername IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccessRights");

            migrationBuilder.DropTable(
                name: "IicWorkDirectionNorms");

            migrationBuilder.DropTable(
                name: "ProjectLaborIntensities");

            migrationBuilder.DropTable(
                name: "ProjectPhysicalVolumes");

            migrationBuilder.DropTable(
                name: "ProjectUserAccess");

            migrationBuilder.DropTable(
                name: "PhysicalVolumes");

            migrationBuilder.DropTable(
                name: "ProjectAnalysisVersions");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "IicWorkDirections");

            migrationBuilder.DropTable(
                name: "PhysicalVolumeTypes");

            migrationBuilder.DropTable(
                name: "Projects");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "UnitsOfMeasurement");
        }
    }
}
