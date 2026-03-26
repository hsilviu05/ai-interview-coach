using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIInterviewCoach.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateInterviewEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CandidateName",
                table: "Interviews",
                newName: "Title");

            migrationBuilder.AlterColumn<string>(
                name: "AccessToken",
                table: "Interviews",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Interviews",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateIndex(
                name: "IX_InterviewSessions_InterviewId",
                table: "InterviewSessions",
                column: "InterviewId");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewProblems_InterviewId",
                table: "InterviewProblems",
                column: "InterviewId");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewProblems_ProblemId",
                table: "InterviewProblems",
                column: "ProblemId");

            migrationBuilder.AddForeignKey(
                name: "FK_InterviewProblems_Interviews_InterviewId",
                table: "InterviewProblems",
                column: "InterviewId",
                principalTable: "Interviews",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_InterviewProblems_Problems_ProblemId",
                table: "InterviewProblems",
                column: "ProblemId",
                principalTable: "Problems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_InterviewSessions_Interviews_InterviewId",
                table: "InterviewSessions",
                column: "InterviewId",
                principalTable: "Interviews",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InterviewProblems_Interviews_InterviewId",
                table: "InterviewProblems");

            migrationBuilder.DropForeignKey(
                name: "FK_InterviewProblems_Problems_ProblemId",
                table: "InterviewProblems");

            migrationBuilder.DropForeignKey(
                name: "FK_InterviewSessions_Interviews_InterviewId",
                table: "InterviewSessions");

            migrationBuilder.DropIndex(
                name: "IX_InterviewSessions_InterviewId",
                table: "InterviewSessions");

            migrationBuilder.DropIndex(
                name: "IX_InterviewProblems_InterviewId",
                table: "InterviewProblems");

            migrationBuilder.DropIndex(
                name: "IX_InterviewProblems_ProblemId",
                table: "InterviewProblems");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Interviews");

            migrationBuilder.RenameColumn(
                name: "Title",
                table: "Interviews",
                newName: "CandidateName");

            migrationBuilder.AlterColumn<int>(
                name: "AccessToken",
                table: "Interviews",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");
        }
    }
}
