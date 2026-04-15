using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIInterviewCoach.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SyncModelWithDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CppHarnessTemplate",
                table: "Problems",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CppStarterCode",
                table: "Problems",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CsharpHarnessTemplate",
                table: "Problems",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CsharpStarterCode",
                table: "Problems",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ExecutionMode",
                table: "Problems",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PythonHarnessTemplate",
                table: "Problems",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PythonStarterCode",
                table: "Problems",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CppHarnessTemplate",
                table: "Problems");

            migrationBuilder.DropColumn(
                name: "CppStarterCode",
                table: "Problems");

            migrationBuilder.DropColumn(
                name: "CsharpHarnessTemplate",
                table: "Problems");

            migrationBuilder.DropColumn(
                name: "CsharpStarterCode",
                table: "Problems");

            migrationBuilder.DropColumn(
                name: "ExecutionMode",
                table: "Problems");

            migrationBuilder.DropColumn(
                name: "PythonHarnessTemplate",
                table: "Problems");

            migrationBuilder.DropColumn(
                name: "PythonStarterCode",
                table: "Problems");
        }
    }
}
