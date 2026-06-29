using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EngineeringPerformance.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class V2C_ProjectPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProjectPermissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Level = table.Column<int>(type: "int", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectPermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectPermissions_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectPermissions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectPermissions_UserId",
                table: "ProjectPermissions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "UX_ProjectPermissions_Project_User",
                table: "ProjectPermissions",
                columns: new[] { "ProjectId", "UserId" },
                unique: true);

            // V2-C миграция данных:
            // 1) Visibility=1 (SpecificUsers) — явные записи в ProjectUserAccess переносим как Read.
            // 2) Visibility=0 (AllUsers) — раньше все не-админы имели неявный доступ,
            //    сохраняем совместимость: выдаём Read всем не-администраторам.
            // Level = 1 (AccessLevel.Read).
            migrationBuilder.Sql(@"
                INSERT INTO ProjectPermissions (ProjectId, UserId, Level)
                SELECT DISTINCT pua.ProjectId, pua.UserId, 1
                FROM ProjectUserAccess pua
                INNER JOIN Projects p ON p.Id = pua.ProjectId
                WHERE p.Visibility = 1
                  AND NOT EXISTS (
                      SELECT 1 FROM ProjectPermissions pp
                      WHERE pp.ProjectId = pua.ProjectId AND pp.UserId = pua.UserId);
            ");

            migrationBuilder.Sql(@"
                INSERT INTO ProjectPermissions (ProjectId, UserId, Level)
                SELECT p.Id, u.Id, 1
                FROM Projects p
                CROSS JOIN Users u
                INNER JOIN Roles r ON r.Id = u.RoleId
                WHERE p.Visibility = 0
                  AND r.Name <> 'Administrator'
                  AND NOT EXISTS (
                      SELECT 1 FROM ProjectPermissions pp
                      WHERE pp.ProjectId = p.Id AND pp.UserId = u.Id);
            ");

            migrationBuilder.DropTable(
                name: "ProjectUserAccess");

            migrationBuilder.DropColumn(
                name: "Visibility",
                table: "Projects");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProjectPermissions");

            migrationBuilder.AddColumn<int>(
                name: "Visibility",
                table: "Projects",
                type: "int",
                nullable: false,
                defaultValue: 0);

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

            migrationBuilder.CreateIndex(
                name: "IX_ProjectUserAccess_ProjectId_UserId",
                table: "ProjectUserAccess",
                columns: new[] { "ProjectId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectUserAccess_UserId",
                table: "ProjectUserAccess",
                column: "UserId");
        }
    }
}
