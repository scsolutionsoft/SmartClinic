using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartClinic.Web.Migrations
{
    /// <inheritdoc />
    public partial class ExpandTreatmentRecordDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Diagnosis",
                table: "TreatmentRecords",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(300)",
                oldMaxLength: 300);

            migrationBuilder.AddColumn<string>(
                name: "AuthenticationCode",
                table: "TreatmentRecords",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ChiefComplaint",
                table: "TreatmentRecords",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ChildDevelopmentStatus",
                table: "TreatmentRecords",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChildGrowthStatus",
                table: "TreatmentRecords",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChildVaccineNote",
                table: "TreatmentRecords",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChildVaccineStatus",
                table: "TreatmentRecords",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CitizenId",
                table: "TreatmentRecords",
                type: "nvarchar(13)",
                maxLength: 13,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "InitialDifferentialDiagnosis",
                table: "TreatmentRecords",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsChildCase",
                table: "TreatmentRecords",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PhysicalExam",
                table: "TreatmentRecords",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PresentIllness",
                table: "TreatmentRecords",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProblemPhysicalExam",
                table: "TreatmentRecords",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ServiceRecipientId",
                table: "TreatmentRecords",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("""
                UPDATE tr
                SET tr.CitizenId = p.CitizenId
                FROM TreatmentRecords tr
                INNER JOIN Patients p ON tr.PatientId = p.Id
                WHERE tr.CitizenId = ''
                """);

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentRecords_ClinicCode_CitizenId_VisitDate",
                table: "TreatmentRecords",
                columns: new[] { "ClinicCode", "CitizenId", "VisitDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TreatmentRecords_ClinicCode_CitizenId_VisitDate",
                table: "TreatmentRecords");

            migrationBuilder.DropColumn(
                name: "AuthenticationCode",
                table: "TreatmentRecords");

            migrationBuilder.DropColumn(
                name: "ChiefComplaint",
                table: "TreatmentRecords");

            migrationBuilder.DropColumn(
                name: "ChildDevelopmentStatus",
                table: "TreatmentRecords");

            migrationBuilder.DropColumn(
                name: "ChildGrowthStatus",
                table: "TreatmentRecords");

            migrationBuilder.DropColumn(
                name: "ChildVaccineNote",
                table: "TreatmentRecords");

            migrationBuilder.DropColumn(
                name: "ChildVaccineStatus",
                table: "TreatmentRecords");

            migrationBuilder.DropColumn(
                name: "CitizenId",
                table: "TreatmentRecords");

            migrationBuilder.DropColumn(
                name: "InitialDifferentialDiagnosis",
                table: "TreatmentRecords");

            migrationBuilder.DropColumn(
                name: "IsChildCase",
                table: "TreatmentRecords");

            migrationBuilder.DropColumn(
                name: "PhysicalExam",
                table: "TreatmentRecords");

            migrationBuilder.DropColumn(
                name: "PresentIllness",
                table: "TreatmentRecords");

            migrationBuilder.DropColumn(
                name: "ProblemPhysicalExam",
                table: "TreatmentRecords");

            migrationBuilder.DropColumn(
                name: "ServiceRecipientId",
                table: "TreatmentRecords");

            migrationBuilder.AlterColumn<string>(
                name: "Diagnosis",
                table: "TreatmentRecords",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000);
        }
    }
}
