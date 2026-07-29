using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartClinic.Web.Migrations
{
    /// <inheritdoc />
    public partial class EnforceSinglePatientMedicalProfilePerPatient : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_PatientMedicalProfiles_ClinicCode_PatientId",
                table: "PatientMedicalProfiles",
                columns: new[] { "ClinicCode", "PatientId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PatientMedicalProfiles_ClinicCode_PatientId",
                table: "PatientMedicalProfiles");
        }
    }
}
