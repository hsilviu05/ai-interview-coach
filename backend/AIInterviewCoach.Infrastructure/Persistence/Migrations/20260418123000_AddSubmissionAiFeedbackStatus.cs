using AIInterviewCoach.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIInterviewCoach.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20260418123000_AddSubmissionAiFeedbackStatus")]
    public partial class AddSubmissionAiFeedbackStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AiFeedbackStatus",
                table: "Submissions",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "Submissions"
                SET "AiFeedbackStatus" = 'Ready'
                WHERE "AiFeedbackStatus" IS NULL;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "AiFeedbackStatus",
                table: "Submissions",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Pending",
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AiFeedbackStatus",
                table: "Submissions");
        }
    }
}
