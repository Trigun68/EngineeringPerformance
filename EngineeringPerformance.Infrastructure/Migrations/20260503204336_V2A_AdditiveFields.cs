using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EngineeringPerformance.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class V2A_AdditiveFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_IicWorkDirectionNorms_WorkDirectionId",
                table: "IicWorkDirectionNorms");

            migrationBuilder.AddColumn<string>(
                name: "WindowsSid",
                table: "Users",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "ContractDate",
                table: "Projects",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "WorkEnd",
                table: "Projects",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ImportedByUserId",
                table: "DataImportIterations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceFileHash",
                table: "DataImportIterations",
                type: "nchar(64)",
                fixedLength: true,
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceFileName",
                table: "DataImportIterations",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_WindowsSid",
                table: "Users",
                column: "WindowsSid",
                unique: true,
                filter: "WindowsSid IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_IicWorkDirectionNorms_WorkDirection_EffectiveFrom",
                table: "IicWorkDirectionNorms",
                columns: new[] { "WorkDirectionId", "EffectiveFrom" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_IicWorkDirectionNorms_EffectiveRange",
                table: "IicWorkDirectionNorms",
                sql: "[EffectiveTo] IS NULL OR [EffectiveTo] > [EffectiveFrom]");

            migrationBuilder.CreateIndex(
                name: "IX_DataImportIterations_ImportedByUserId",
                table: "DataImportIterations",
                column: "ImportedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DataImportIterations_SourceFileHash",
                table: "DataImportIterations",
                column: "SourceFileHash",
                filter: "SourceFileHash IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_DataImportIterations_Users_ImportedByUserId",
                table: "DataImportIterations",
                column: "ImportedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DataImportIterations_Users_ImportedByUserId",
                table: "DataImportIterations");

            migrationBuilder.DropIndex(
                name: "IX_Users_WindowsSid",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "UX_IicWorkDirectionNorms_WorkDirection_EffectiveFrom",
                table: "IicWorkDirectionNorms");

            migrationBuilder.DropCheckConstraint(
                name: "CK_IicWorkDirectionNorms_EffectiveRange",
                table: "IicWorkDirectionNorms");

            migrationBuilder.DropIndex(
                name: "IX_DataImportIterations_ImportedByUserId",
                table: "DataImportIterations");

            migrationBuilder.DropIndex(
                name: "IX_DataImportIterations_SourceFileHash",
                table: "DataImportIterations");

            migrationBuilder.DropColumn(
                name: "WindowsSid",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ContractDate",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "WorkEnd",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "ImportedByUserId",
                table: "DataImportIterations");

            migrationBuilder.DropColumn(
                name: "SourceFileHash",
                table: "DataImportIterations");

            migrationBuilder.DropColumn(
                name: "SourceFileName",
                table: "DataImportIterations");

            migrationBuilder.CreateIndex(
                name: "IX_IicWorkDirectionNorms_WorkDirectionId",
                table: "IicWorkDirectionNorms",
                column: "WorkDirectionId");
        }
    }
}
