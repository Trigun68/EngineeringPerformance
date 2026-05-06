using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EngineeringPerformance.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class V2B_AnalysisVersionSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AnalysisVersionSnapshots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectAnalysisVersionId = table.Column<int>(type: "int", nullable: false),
                    WorkDirectionId = table.Column<int>(type: "int", nullable: false),
                    NormId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalysisVersionSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnalysisVersionSnapshots_IicWorkDirectionNorms_NormId",
                        column: x => x.NormId,
                        principalTable: "IicWorkDirectionNorms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AnalysisVersionSnapshots_IicWorkDirections_WorkDirectionId",
                        column: x => x.WorkDirectionId,
                        principalTable: "IicWorkDirections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AnalysisVersionSnapshots_ProjectAnalysisVersions_ProjectAnalysisVersionId",
                        column: x => x.ProjectAnalysisVersionId,
                        principalTable: "ProjectAnalysisVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnalysisVersionSnapshots_NormId",
                table: "AnalysisVersionSnapshots",
                column: "NormId");

            migrationBuilder.CreateIndex(
                name: "IX_AnalysisVersionSnapshots_WorkDirectionId",
                table: "AnalysisVersionSnapshots",
                column: "WorkDirectionId");

            migrationBuilder.CreateIndex(
                name: "UX_AnalysisVersionSnapshots_Version_WorkDirection",
                table: "AnalysisVersionSnapshots",
                columns: new[] { "ProjectAnalysisVersionId", "WorkDirectionId" },
                unique: true);

            // V2-B: наполняем снимки для уже существующих версий. Для каждой версии
            // подбираем ту норму, что действовала на CreatedAt. Уникальный индекс
            // (WorkDirectionId, EffectiveFrom) из V2-A гарантирует однозначность
            // по направлению в рамках окна.
            migrationBuilder.Sql(@"
                INSERT INTO [AnalysisVersionSnapshots] (ProjectAnalysisVersionId, WorkDirectionId, NormId)
                SELECT v.Id, n.WorkDirectionId, n.Id
                FROM [ProjectAnalysisVersions] v
                INNER JOIN [IicWorkDirectionNorms] n
                    ON n.EffectiveFrom <= v.CreatedAt
                   AND (n.EffectiveTo IS NULL OR n.EffectiveTo >= v.CreatedAt);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnalysisVersionSnapshots");
        }
    }
}
