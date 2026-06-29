using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EngineeringPerformance.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserActionLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserActionLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Timestamp = table.Column<System.DateTime>(type: "datetime2", nullable: false),
                    ActionType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EntityId = table.Column<int>(type: "int", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IpOrHost = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserActionLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserActionLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserActionLogs_UserId_Timestamp",
                table: "UserActionLogs",
                columns: new[] { "UserId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_UserActionLogs_ActionType",
                table: "UserActionLogs",
                column: "ActionType");

            migrationBuilder.CreateIndex(
                name: "IX_UserActionLogs_EntityType_EntityId",
                table: "UserActionLogs",
                columns: new[] { "EntityType", "EntityId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "UserActionLogs");
        }
    }
}
