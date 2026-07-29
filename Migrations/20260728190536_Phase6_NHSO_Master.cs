using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartClinic.Web.Migrations
{
    /// <inheritdoc />
    public partial class Phase6_NHSO_Master : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NhssoClinicMasters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClinicCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    ClinicName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ContactPhone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    ContactEmail = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NhssoClinicMasters", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NhssoClinicMasters_ClinicCode",
                table: "NhssoClinicMasters",
                column: "ClinicCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NhssoClinicMasters");
        }
    }
}
