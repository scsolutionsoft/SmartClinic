using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartClinic.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddPatientPhotoData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "PhotoData",
                table: "Patients",
                type: "varbinary(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PhotoData",
                table: "Patients");
        }
    }
}
