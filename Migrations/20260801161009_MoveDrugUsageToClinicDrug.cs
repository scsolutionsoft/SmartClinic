using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartClinic.Web.Migrations
{
    /// <inheritdoc />
    public partial class MoveDrugUsageToClinicDrug : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AdviceText",
                table: "ClinicDrugs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "Bedtime",
                table: "ClinicDrugs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "DoseAmount",
                table: "ClinicDrugs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "Evening",
                table: "ClinicDrugs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "FrequencyPerDay",
                table: "ClinicDrugs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "IntervalHours",
                table: "ClinicDrugs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ManufacturerName",
                table: "ClinicDrugs",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MealTiming",
                table: "ClinicDrugs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "Morning",
                table: "ClinicDrugs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Noon",
                table: "ClinicDrugs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "UsageText",
                table: "ClinicDrugs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(@"
                UPDATE d
                SET d.DoseAmount = p.DoseAmount,
                    d.FrequencyPerDay = p.FrequencyPerDay,
                    d.MealTiming = p.MealTiming,
                    d.Morning = p.Morning,
                    d.Noon = p.Noon,
                    d.Evening = p.Evening,
                    d.Bedtime = p.Bedtime,
                    d.IntervalHours = p.IntervalHours,
                    d.UsageText = p.UsageText,
                    d.AdviceText = p.AdviceText
                FROM ClinicDrugs d
                CROSS APPLY (
                    SELECT TOP (1)
                        x.DoseAmount, x.FrequencyPerDay, x.MealTiming,
                        x.Morning, x.Noon, x.Evening, x.Bedtime,
                        x.IntervalHours, x.UsageText, x.AdviceText
                    FROM IcdDrugProtocols x
                    WHERE x.ClinicDrugId = d.Id AND x.ClinicCode = d.ClinicCode
                    ORDER BY
                        CASE WHEN NULLIF(LTRIM(RTRIM(x.UsageText)), '') IS NULL THEN 1 ELSE 0 END,
                        x.UsageCount DESC, x.DisplayOrder, x.Id
                ) p;");

            migrationBuilder.Sql(@"
                UPDATE d
                SET d.ManufacturerName = LTRIM(RTRIM(SUBSTRING(src.TreatmentAndAdvice, pos.OpenPos + 1, pos.ClosePos - pos.OpenPos - 1)))
                FROM ClinicDrugs d
                CROSS APPLY (
                    SELECT TOP (1) t.TreatmentAndAdvice
                    FROM TreatmentRecords t
                    WHERE t.ClinicCode = d.ClinicCode
                      AND CHARINDEX(d.GenericName, t.TreatmentAndAdvice) > 0
                    ORDER BY t.VisitDate DESC, t.Id DESC
                ) src
                CROSS APPLY (
                    SELECT CHARINDEX('(', src.TreatmentAndAdvice, CHARINDEX(d.GenericName, src.TreatmentAndAdvice) + LEN(d.GenericName)) AS OpenPos
                ) opening
                CROSS APPLY (
                    SELECT opening.OpenPos, CHARINDEX(')', src.TreatmentAndAdvice, opening.OpenPos + 1) AS ClosePos
                ) pos
                WHERE d.ManufacturerName = ''
                  AND pos.OpenPos > 0 AND pos.ClosePos > pos.OpenPos;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdviceText",
                table: "ClinicDrugs");

            migrationBuilder.DropColumn(
                name: "Bedtime",
                table: "ClinicDrugs");

            migrationBuilder.DropColumn(
                name: "DoseAmount",
                table: "ClinicDrugs");

            migrationBuilder.DropColumn(
                name: "Evening",
                table: "ClinicDrugs");

            migrationBuilder.DropColumn(
                name: "FrequencyPerDay",
                table: "ClinicDrugs");

            migrationBuilder.DropColumn(
                name: "IntervalHours",
                table: "ClinicDrugs");

            migrationBuilder.DropColumn(
                name: "ManufacturerName",
                table: "ClinicDrugs");

            migrationBuilder.DropColumn(
                name: "MealTiming",
                table: "ClinicDrugs");

            migrationBuilder.DropColumn(
                name: "Morning",
                table: "ClinicDrugs");

            migrationBuilder.DropColumn(
                name: "Noon",
                table: "ClinicDrugs");

            migrationBuilder.DropColumn(
                name: "UsageText",
                table: "ClinicDrugs");
        }
    }
}
