using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartClinic.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddOpdVitalSigns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "BodyMassIndex",
                table: "TreatmentRecords",
                type: "decimal(6,2)",
                precision: 6,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DiastolicPressure",
                table: "TreatmentRecords",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "HeightCentimeters",
                table: "TreatmentRecords",
                type: "decimal(6,2)",
                precision: 6,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PulseRate",
                table: "TreatmentRecords",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RespiratoryRate",
                table: "TreatmentRecords",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SystolicPressure",
                table: "TreatmentRecords",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TemperatureCelsius",
                table: "TreatmentRecords",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WeightKilograms",
                table: "TreatmentRecords",
                type: "decimal(6,2)",
                precision: 6,
                scale: 2,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BodyMassIndex",
                table: "TreatmentRecords");

            migrationBuilder.DropColumn(
                name: "DiastolicPressure",
                table: "TreatmentRecords");

            migrationBuilder.DropColumn(
                name: "HeightCentimeters",
                table: "TreatmentRecords");

            migrationBuilder.DropColumn(
                name: "PulseRate",
                table: "TreatmentRecords");

            migrationBuilder.DropColumn(
                name: "RespiratoryRate",
                table: "TreatmentRecords");

            migrationBuilder.DropColumn(
                name: "SystolicPressure",
                table: "TreatmentRecords");

            migrationBuilder.DropColumn(
                name: "TemperatureCelsius",
                table: "TreatmentRecords");

            migrationBuilder.DropColumn(
                name: "WeightKilograms",
                table: "TreatmentRecords");
        }
    }
}
