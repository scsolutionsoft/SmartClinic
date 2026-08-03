using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartClinic.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddWoundCareAndTreatmentSelfPay : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSelfPay",
                table: "TreatmentRecords",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "WoundCareRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClinicCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    PatientId = table.Column<int>(type: "int", nullable: false),
                    CitizenId = table.Column<string>(type: "nvarchar(13)", maxLength: 13, nullable: false),
                    ServiceRecipientId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VisitDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AuthenticationCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    WoundCause = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    WoundLocation = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    IsBruiseOrSutured = table.Column<bool>(type: "bit", nullable: false),
                    IsOpenInfected = table.Column<bool>(type: "bit", nullable: false),
                    IsLargeOpenWound = table.Column<bool>(type: "bit", nullable: false),
                    WoundSize = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DressingSequence = table.Column<int>(type: "int", nullable: true),
                    WoundProgress = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    WoundProgressNote = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TemperatureCelsius = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    PulseRate = table.Column<int>(type: "int", nullable: true),
                    RespiratoryRate = table.Column<int>(type: "int", nullable: true),
                    SystolicPressure = table.Column<int>(type: "int", nullable: true),
                    DiastolicPressure = table.Column<int>(type: "int", nullable: true),
                    PostDressingResult = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FollowUpNone = table.Column<bool>(type: "bit", nullable: false),
                    FollowUpPhone = table.Column<bool>(type: "bit", nullable: false),
                    FollowUpClinic = table.Column<bool>(type: "bit", nullable: false),
                    FollowUpNote = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FollowUpOther = table.Column<bool>(type: "bit", nullable: false),
                    WoundAssessment = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PatientAdvice = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProviderUserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProviderName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProviderTitle = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OriginalContentType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OriginalDocumentData = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    OcrRawText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WoundCareRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WoundCareRecords_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WoundCarePhotos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WoundCareRecordId = table.Column<int>(type: "int", nullable: false),
                    SequenceNo = table.Column<byte>(type: "tinyint", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ImageData = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    Caption = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UploadedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WoundCarePhotos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WoundCarePhotos_WoundCareRecords_WoundCareRecordId",
                        column: x => x.WoundCareRecordId,
                        principalTable: "WoundCareRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WoundCarePhotos_WoundCareRecordId_SequenceNo",
                table: "WoundCarePhotos",
                columns: new[] { "WoundCareRecordId", "SequenceNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WoundCareRecords_ClinicCode_AuthenticationCode",
                table: "WoundCareRecords",
                columns: new[] { "ClinicCode", "AuthenticationCode" });

            migrationBuilder.CreateIndex(
                name: "IX_WoundCareRecords_ClinicCode_PatientId_VisitDate",
                table: "WoundCareRecords",
                columns: new[] { "ClinicCode", "PatientId", "VisitDate" });

            migrationBuilder.CreateIndex(
                name: "IX_WoundCareRecords_PatientId",
                table: "WoundCareRecords",
                column: "PatientId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WoundCarePhotos");

            migrationBuilder.DropTable(
                name: "WoundCareRecords");

            migrationBuilder.DropColumn(
                name: "IsSelfPay",
                table: "TreatmentRecords");
        }
    }
}
