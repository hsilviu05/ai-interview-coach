using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIInterviewCoach.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIsPublicToProblem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TestCases_TestCases_TestCaseId",
                table: "TestCases");

            migrationBuilder.DropIndex(
                name: "IX_TestCases_TestCaseId",
                table: "TestCases");

            migrationBuilder.DropColumn(
                name: "TestCaseId",
                table: "TestCases");

            migrationBuilder.RenameColumn(
                name: "IsPrivate",
                table: "Problems",
                newName: "IsPublic");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IsPublic",
                table: "Problems",
                newName: "IsPrivate");

            migrationBuilder.AddColumn<Guid>(
                name: "TestCaseId",
                table: "TestCases",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TestCases_TestCaseId",
                table: "TestCases",
                column: "TestCaseId");

            migrationBuilder.AddForeignKey(
                name: "FK_TestCases_TestCases_TestCaseId",
                table: "TestCases",
                column: "TestCaseId",
                principalTable: "TestCases",
                principalColumn: "Id");
        }
    }
}
