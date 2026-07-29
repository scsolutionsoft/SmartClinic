using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartClinic.Web.Migrations
{
    /// <inheritdoc />
    public partial class Phase4_Signatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SignImg",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClinicCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    CitizenId = table.Column<string>(type: "nvarchar(13)", maxLength: 13, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    ImageData = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    UploadedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SignImg", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SignImg_ClinicCode_CitizenId_UploadedAtUtc",
                table: "SignImg",
                columns: new[] { "ClinicCode", "CitizenId", "UploadedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SignImg");
        }
    }
}
