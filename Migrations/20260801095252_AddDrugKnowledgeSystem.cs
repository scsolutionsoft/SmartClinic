using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartClinic.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddDrugKnowledgeSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClinicDrugs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClinicCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    GenericName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    TradeName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Strength = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DosageForm = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TmtCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RegistrationNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Source = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ApprovalStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicDrugs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DrugAdviceTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClinicCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Icd10Code = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Text = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DrugAdviceTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DrugKnowledgeAuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClinicCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EntityId = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Detail = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DrugKnowledgeAuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IcdDrugProtocols",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClinicCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    ClinicDrugId = table.Column<int>(type: "int", nullable: false),
                    Icd10Code = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    DiagnosisType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    DoseAmount = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FrequencyPerDay = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MealTiming = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Morning = table.Column<bool>(type: "bit", nullable: false),
                    Noon = table.Column<bool>(type: "bit", nullable: false),
                    Evening = table.Column<bool>(type: "bit", nullable: false),
                    Bedtime = table.Column<bool>(type: "bit", nullable: false),
                    IntervalHours = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    UsageText = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    AdviceText = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    UsageCount = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IcdDrugProtocols", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IcdDrugProtocols_ClinicDrugs_ClinicDrugId",
                        column: x => x.ClinicDrugId,
                        principalTable: "ClinicDrugs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClinicDrugs_ClinicCode_GenericName_Strength_DosageForm",
                table: "ClinicDrugs",
                columns: new[] { "ClinicCode", "GenericName", "Strength", "DosageForm" });

            migrationBuilder.CreateIndex(
                name: "IX_DrugAdviceTemplates_ClinicCode_Icd10Code_DisplayOrder",
                table: "DrugAdviceTemplates",
                columns: new[] { "ClinicCode", "Icd10Code", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_DrugKnowledgeAuditLogs_ClinicCode_CreatedAtUtc",
                table: "DrugKnowledgeAuditLogs",
                columns: new[] { "ClinicCode", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_IcdDrugProtocols_ClinicCode_Icd10Code_ClinicDrugId",
                table: "IcdDrugProtocols",
                columns: new[] { "ClinicCode", "Icd10Code", "ClinicDrugId" });

            migrationBuilder.CreateIndex(
                name: "IX_IcdDrugProtocols_ClinicDrugId",
                table: "IcdDrugProtocols",
                column: "ClinicDrugId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DrugAdviceTemplates");

            migrationBuilder.DropTable(
                name: "DrugKnowledgeAuditLogs");

            migrationBuilder.DropTable(
                name: "IcdDrugProtocols");

            migrationBuilder.DropTable(
                name: "ClinicDrugs");
        }
    }
}
