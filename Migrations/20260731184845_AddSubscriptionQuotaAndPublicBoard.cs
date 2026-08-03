using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartClinic.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddSubscriptionQuotaAndPublicBoard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HasUnlimitedPatients",
                table: "Clinics",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "PatientLimit",
                table: "Clinics",
                type: "int",
                nullable: false,
                defaultValue: 20);

            migrationBuilder.CreateTable(
                name: "CreditRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClinicCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    ContactName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    RequestedCredits = table.Column<int>(type: "int", nullable: true),
                    RequestUnlimited = table.Column<bool>(type: "bit", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    AdminNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CompletedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PublicAnnouncements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    BadgeText = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    LinkUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    PublishedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PublicAnnouncements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "QuotaTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClinicCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    PreviousLimit = table.Column<int>(type: "int", nullable: false),
                    Adjustment = table.Column<int>(type: "int", nullable: false),
                    NewLimit = table.Column<int>(type: "int", nullable: false),
                    PreviousUnlimited = table.Column<bool>(type: "bit", nullable: false),
                    NewUnlimited = table.Column<bool>(type: "bit", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuotaTransactions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CreditRequests_ClinicCode_Status_CreatedAtUtc",
                table: "CreditRequests",
                columns: new[] { "ClinicCode", "Status", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PublicAnnouncements_IsPublished_DisplayOrder_PublishedAtUtc",
                table: "PublicAnnouncements",
                columns: new[] { "IsPublished", "DisplayOrder", "PublishedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_QuotaTransactions_ClinicCode_CreatedAtUtc",
                table: "QuotaTransactions",
                columns: new[] { "ClinicCode", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CreditRequests");

            migrationBuilder.DropTable(
                name: "PublicAnnouncements");

            migrationBuilder.DropTable(
                name: "QuotaTransactions");

            migrationBuilder.DropColumn(
                name: "HasUnlimitedPatients",
                table: "Clinics");

            migrationBuilder.DropColumn(
                name: "PatientLimit",
                table: "Clinics");
        }
    }
}
