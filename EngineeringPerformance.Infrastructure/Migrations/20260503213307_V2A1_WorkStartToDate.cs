using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EngineeringPerformance.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class V2A1_WorkStartToDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // V2-A.1: пустые/невалидные строковые WorkStart переводим в NULL,
            // иначе AlterColumn(nvarchar -> date) упадёт на CAST('' AS date).
            migrationBuilder.Sql(@"
                UPDATE [Projects]
                SET [WorkStart] = NULL
                WHERE [WorkStart] IS NOT NULL
                  AND (LTRIM(RTRIM([WorkStart])) = N''
                       OR TRY_CONVERT(date, [WorkStart], 23) IS NULL);
            ");

            migrationBuilder.AlterColumn<DateOnly>(
                name: "WorkStart",
                table: "Projects",
                type: "date",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "WorkStart",
                table: "Projects",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(DateOnly),
                oldType: "date",
                oldNullable: true);
        }
    }
}
