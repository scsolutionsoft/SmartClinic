using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartClinic.Web.Migrations
{
    /// <inheritdoc />
    public partial class ExpandIcd10Catalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BlockCode",
                table: "Icd10Codes",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BlockTitle",
                table: "Icd10Codes",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ChapterCode",
                table: "Icd10Codes",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ChapterTitle",
                table: "Icd10Codes",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DisplayCode",
                table: "Icd10Codes",
                type: "nvarchar(12)",
                maxLength: 12,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsDerived",
                table: "Icd10Codes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsTerminal",
                table: "Icd10Codes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Level",
                table: "Icd10Codes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ParentCode",
                table: "Icd10Codes",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "Icd10Codes",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SourceUrl",
                table: "Icd10Codes",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ThaiNameVersion",
                table: "Icd10Codes",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Icd10Codes_IsActive_IsTerminal_Code",
                table: "Icd10Codes",
                columns: new[] { "IsActive", "IsTerminal", "Code" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Icd10Codes_IsActive_IsTerminal_Code",
                table: "Icd10Codes");

            migrationBuilder.DropColumn(
                name: "BlockCode",
                table: "Icd10Codes");

            migrationBuilder.DropColumn(
                name: "BlockTitle",
                table: "Icd10Codes");

            migrationBuilder.DropColumn(
                name: "ChapterCode",
                table: "Icd10Codes");

            migrationBuilder.DropColumn(
                name: "ChapterTitle",
                table: "Icd10Codes");

            migrationBuilder.DropColumn(
                name: "DisplayCode",
                table: "Icd10Codes");

            migrationBuilder.DropColumn(
                name: "IsDerived",
                table: "Icd10Codes");

            migrationBuilder.DropColumn(
                name: "IsTerminal",
                table: "Icd10Codes");

            migrationBuilder.DropColumn(
                name: "Level",
                table: "Icd10Codes");

            migrationBuilder.DropColumn(
                name: "ParentCode",
                table: "Icd10Codes");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "Icd10Codes");

            migrationBuilder.DropColumn(
                name: "SourceUrl",
                table: "Icd10Codes");

            migrationBuilder.DropColumn(
                name: "ThaiNameVersion",
                table: "Icd10Codes");
        }
    }
}
