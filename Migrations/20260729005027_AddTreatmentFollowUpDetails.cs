using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartClinic.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddTreatmentFollowUpDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "FollowUpClinic",
                table: "TreatmentRecords",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "FollowUpClinicNote",
                table: "TreatmentRecords",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "FollowUpNone",
                table: "TreatmentRecords",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "FollowUpOther",
                table: "TreatmentRecords",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "FollowUpOtherNote",
                table: "TreatmentRecords",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "FollowUpPhone",
                table: "TreatmentRecords",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ReferralDetail",
                table: "TreatmentRecords",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "ServiceEndDateTime",
                table: "TreatmentRecords",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TreatmentAndAdvice",
                table: "TreatmentRecords",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FollowUpClinic",
                table: "TreatmentRecords");

            migrationBuilder.DropColumn(
                name: "FollowUpClinicNote",
                table: "TreatmentRecords");

            migrationBuilder.DropColumn(
                name: "FollowUpNone",
                table: "TreatmentRecords");

            migrationBuilder.DropColumn(
                name: "FollowUpOther",
                table: "TreatmentRecords");

            migrationBuilder.DropColumn(
                name: "FollowUpOtherNote",
                table: "TreatmentRecords");

            migrationBuilder.DropColumn(
                name: "FollowUpPhone",
                table: "TreatmentRecords");

            migrationBuilder.DropColumn(
                name: "ReferralDetail",
                table: "TreatmentRecords");

            migrationBuilder.DropColumn(
                name: "ServiceEndDateTime",
                table: "TreatmentRecords");

            migrationBuilder.DropColumn(
                name: "TreatmentAndAdvice",
                table: "TreatmentRecords");
        }
    }
}
