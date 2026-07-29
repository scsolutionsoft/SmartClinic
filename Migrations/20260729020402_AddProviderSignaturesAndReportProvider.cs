using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartClinic.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddProviderSignaturesAndReportProvider : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProviderName",
                table: "TreatmentRecords",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProviderProfessionalTitle",
                table: "TreatmentRecords",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProviderUserId",
                table: "TreatmentRecords",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FullName",
                table: "AspNetUsers",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ClinicCode",
                table: "AspNetUsers",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LicenseNo",
                table: "AspNetUsers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProfessionalTitle",
                table: "AspNetUsers",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProviderSignatureContentType",
                table: "AspNetUsers",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProviderSignatureFileName",
                table: "AspNetUsers",
                type: "nvarchar(260)",
                maxLength: 260,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "ProviderSignatureImageData",
                table: "AspNetUsers",
                type: "varbinary(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ProviderSignatureUploadedAtUtc",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProviderName",
                table: "TreatmentRecords");

            migrationBuilder.DropColumn(
                name: "ProviderProfessionalTitle",
                table: "TreatmentRecords");

            migrationBuilder.DropColumn(
                name: "ProviderUserId",
                table: "TreatmentRecords");

            migrationBuilder.DropColumn(
                name: "LicenseNo",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ProfessionalTitle",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ProviderSignatureContentType",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ProviderSignatureFileName",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ProviderSignatureImageData",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ProviderSignatureUploadedAtUtc",
                table: "AspNetUsers");

            migrationBuilder.AlterColumn<string>(
                name: "FullName",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ClinicCode",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(10)",
                oldMaxLength: 10,
                oldNullable: true);
        }
    }
}
