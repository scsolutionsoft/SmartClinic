using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartClinic.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddFollowUpAppointments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FollowUpAppointmentDateTime",
                table: "TreatmentRecords",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentRecords_ClinicCode_FollowUpAppointmentDateTime",
                table: "TreatmentRecords",
                columns: new[] { "ClinicCode", "FollowUpAppointmentDateTime" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TreatmentRecords_ClinicCode_FollowUpAppointmentDateTime",
                table: "TreatmentRecords");

            migrationBuilder.DropColumn(
                name: "FollowUpAppointmentDateTime",
                table: "TreatmentRecords");
        }
    }
}
