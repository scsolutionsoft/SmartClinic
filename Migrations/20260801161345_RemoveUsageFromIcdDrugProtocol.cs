using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartClinic.Web.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUsageFromIcdDrugProtocol : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdviceText",
                table: "IcdDrugProtocols");

            migrationBuilder.DropColumn(
                name: "Bedtime",
                table: "IcdDrugProtocols");

            migrationBuilder.DropColumn(
                name: "DoseAmount",
                table: "IcdDrugProtocols");

            migrationBuilder.DropColumn(
                name: "Evening",
                table: "IcdDrugProtocols");

            migrationBuilder.DropColumn(
                name: "FrequencyPerDay",
                table: "IcdDrugProtocols");

            migrationBuilder.DropColumn(
                name: "IntervalHours",
                table: "IcdDrugProtocols");

            migrationBuilder.DropColumn(
                name: "MealTiming",
                table: "IcdDrugProtocols");

            migrationBuilder.DropColumn(
                name: "Morning",
                table: "IcdDrugProtocols");

            migrationBuilder.DropColumn(
                name: "Noon",
                table: "IcdDrugProtocols");

            migrationBuilder.DropColumn(
                name: "UsageText",
                table: "IcdDrugProtocols");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AdviceText",
                table: "IcdDrugProtocols",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "Bedtime",
                table: "IcdDrugProtocols",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "DoseAmount",
                table: "IcdDrugProtocols",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "Evening",
                table: "IcdDrugProtocols",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "FrequencyPerDay",
                table: "IcdDrugProtocols",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "IntervalHours",
                table: "IcdDrugProtocols",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MealTiming",
                table: "IcdDrugProtocols",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "Morning",
                table: "IcdDrugProtocols",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Noon",
                table: "IcdDrugProtocols",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "UsageText",
                table: "IcdDrugProtocols",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");
        }
    }
}
