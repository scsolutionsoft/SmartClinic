using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartClinic.Web.Migrations
{
    /// <inheritdoc />
    public partial class ChangeQuotaToOpdRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "PatientLimit",
                table: "Clinics",
                type: "int",
                nullable: false,
                defaultValue: 30,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 20);

            migrationBuilder.Sql("UPDATE [Clinics] SET [PatientLimit] = 30 WHERE [PatientLimit] = 20");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "PatientLimit",
                table: "Clinics",
                type: "int",
                nullable: false,
                defaultValue: 20,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 30);
        }
    }
}
