using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartClinic.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddMedicationLabelsAndClinicHours : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OpeningHours",
                table: "Clinics",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "MedicationLabelTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClinicCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    DiseaseCategory = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TemplateName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    MedicineName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    DoseAmount = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FrequencyPerDay = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MealTiming = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Morning = table.Column<bool>(type: "bit", nullable: false),
                    Noon = table.Column<bool>(type: "bit", nullable: false),
                    Evening = table.Column<bool>(type: "bit", nullable: false),
                    Bedtime = table.Column<bool>(type: "bit", nullable: false),
                    IntervalHours = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    FinishCourse = table.Column<bool>(type: "bit", nullable: false),
                    MayCauseDrowsiness = table.Column<bool>(type: "bit", nullable: false),
                    BeforeMeal30Minutes = table.Column<bool>(type: "bit", nullable: false),
                    ImmediatelyAfterMeal = table.Column<bool>(type: "bit", nullable: false),
                    AdditionalAdvice = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicationLabelTemplates", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MedicationLabelTemplates_ClinicCode_DiseaseCategory_TemplateName",
                table: "MedicationLabelTemplates",
                columns: new[] { "ClinicCode", "DiseaseCategory", "TemplateName" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MedicationLabelTemplates");

            migrationBuilder.DropColumn(
                name: "OpeningHours",
                table: "Clinics");
        }
    }
}
