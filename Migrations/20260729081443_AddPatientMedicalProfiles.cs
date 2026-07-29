using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartClinic.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddPatientMedicalProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PatientMedicalProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClinicCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    PatientId = table.Column<int>(type: "int", nullable: false),
                    CitizenId = table.Column<string>(type: "nvarchar(13)", maxLength: 13, nullable: false),
                    ServiceRecipientId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ClinicName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    ClinicAddress = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    PatientName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Gender = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    BirthDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Race = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Nationality = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MaritalStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RegisteredAddress = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ContactAddress = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    PrimaryHospital = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    UnderlyingDisease = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    PastHistory = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    FamilyHistory = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    AllergyHistory = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    MedicalBenefit = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    EmergencyContactName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EmergencyContactPhone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    InformationGivenDate = table.Column<DateOnly>(type: "date", nullable: true),
                    SourcePdfFileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    SourcePdfContentType = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    SourcePdfData = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientMedicalProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PatientMedicalProfiles_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PatientMedicalProfiles_ClinicCode_CitizenId_CreatedAtUtc",
                table: "PatientMedicalProfiles",
                columns: new[] { "ClinicCode", "CitizenId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PatientMedicalProfiles_ClinicCode_PatientId_InformationGivenDate",
                table: "PatientMedicalProfiles",
                columns: new[] { "ClinicCode", "PatientId", "InformationGivenDate" });

            migrationBuilder.CreateIndex(
                name: "IX_PatientMedicalProfiles_PatientId",
                table: "PatientMedicalProfiles",
                column: "PatientId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PatientMedicalProfiles");
        }
    }
}
