using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartClinic.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddIcd10AndRestoreNamePrefix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DifferentialIcd10Codes",
                table: "TreatmentRecords",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PrimaryIcd10Code",
                table: "TreatmentRecords",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "Icd10Codes",
                columns: table => new
                {
                    Code = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    ThaiName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    EnglishName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SearchTerms = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Version = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Icd10Codes", x => x.Code);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Icd10Codes_ThaiName",
                table: "Icd10Codes",
                column: "ThaiName");

            migrationBuilder.Sql(
                """
                INSERT INTO [Icd10Codes] ([Code],[ThaiName],[EnglishName],[SearchTerms],[Version],[IsActive]) VALUES
                (N'J00',N'คอหอยส่วนจมูกอักเสบเฉียบพลัน (ไข้หวัด)',N'Acute nasopharyngitis (common cold)',N'หวัด ไข้หวัด common cold',N'ICD-10-TM 2016',1),
                (N'H811',N'การรู้สึกหมุนเป็นระยะแบบไม่รุนแรง',N'Benign paroxysmal vertigo',N'เวียนศีรษะ บ้านหมุน vertigo',N'ICD-10-TM 2016',1),
                (N'K0880',N'ปวดฟัน',N'Toothache',N'ปวดฟัน tooth pain',N'ICD-10-TM 2016',1),
                (N'R21',N'ผื่นและการเปลี่ยนแปลงอื่นของผิวหนัง',N'Rash and other nonspecific skin eruption',N'ผื่น rash',N'ICD-10-TM 2016',1),
                (N'M7916',N'ปวดกล้ามเนื้อบริเวณขาส่วนล่าง',N'Myalgia, lower leg',N'ปวดกล้ามเนื้อ ปวดขา myalgia',N'ICD-10-TM 2016',1),
                (N'M545',N'ปวดหลังส่วนล่าง',N'Low back pain',N'ปวดหลัง low back pain',N'ICD-10-TM 2016',1),
                (N'K30',N'อาหารไม่ย่อย',N'Functional dyspepsia',N'ท้องอืด อาหารไม่ย่อย dyspepsia',N'ICD-10-TM 2016',1),
                (N'I10',N'ความดันโลหิตสูงปฐมภูมิ',N'Essential (primary) hypertension',N'ความดันสูง hypertension',N'ICD-10-TM 2016',1),
                (N'E119',N'เบาหวานชนิดที่ 2 ไม่มีภาวะแทรกซ้อน',N'Type 2 diabetes mellitus without complications',N'เบาหวาน diabetes',N'ICD-10-TM 2016',1),
                (N'J069',N'การติดเชื้อเฉียบพลันของทางเดินหายใจส่วนบน ไม่ระบุรายละเอียด',N'Acute upper respiratory infection, unspecified',N'หวัด ทางเดินหายใจ URI',N'ICD-10-TM 2016',1),
                (N'J029',N'คอหอยอักเสบเฉียบพลัน ไม่ระบุรายละเอียด',N'Acute pharyngitis, unspecified',N'เจ็บคอ คออักเสบ pharyngitis',N'ICD-10-TM 2016',1),
                (N'J039',N'ทอนซิลอักเสบเฉียบพลัน ไม่ระบุรายละเอียด',N'Acute tonsillitis, unspecified',N'ทอนซิลอักเสบ tonsillitis',N'ICD-10-TM 2016',1),
                (N'R05',N'ไอ',N'Cough',N'ไอ cough',N'ICD-10-TM 2016',1),
                (N'R509',N'ไข้ ไม่ระบุรายละเอียด',N'Fever, unspecified',N'ไข้ fever',N'ICD-10-TM 2016',1),
                (N'R51',N'ปวดศีรษะ',N'Headache',N'ปวดหัว ปวดศีรษะ headache',N'ICD-10-TM 2016',1),
                (N'R42',N'เวียนศีรษะและมึนงง',N'Dizziness and giddiness',N'เวียนหัว มึนงง dizziness',N'ICD-10-TM 2016',1),
                (N'R109',N'ปวดท้อง ไม่ระบุตำแหน่ง',N'Unspecified abdominal pain',N'ปวดท้อง abdominal pain',N'ICD-10-TM 2016',1),
                (N'A099',N'กระเพาะและลำไส้อักเสบจากการติดเชื้อ ไม่ระบุรายละเอียด',N'Gastroenteritis and colitis of unspecified origin',N'ท้องเสีย ลำไส้อักเสบ gastroenteritis',N'ICD-10-TM 2016',1),
                (N'K219',N'โรคกรดไหลย้อนโดยไม่มีหลอดอาหารอักเสบ',N'Gastro-oesophageal reflux disease without oesophagitis',N'กรดไหลย้อน GERD',N'ICD-10-TM 2016',1),
                (N'K590',N'ท้องผูก',N'Constipation',N'ท้องผูก constipation',N'ICD-10-TM 2016',1),
                (N'N399',N'การติดเชื้อทางเดินปัสสาวะ ไม่ระบุตำแหน่ง',N'Urinary tract infection, site not specified',N'ปัสสาวะอักเสบ UTI',N'ICD-10-TM 2016',1),
                (N'M255',N'ปวดข้อ',N'Pain in joint',N'ปวดข้อ joint pain',N'ICD-10-TM 2016',1),
                (N'B349',N'การติดเชื้อไวรัส ไม่ระบุรายละเอียด',N'Viral infection, unspecified',N'ติดเชื้อไวรัส viral infection',N'ICD-10-TM 2016',1),
                (N'L239',N'ผิวหนังอักเสบจากการสัมผัสสารก่อภูมิแพ้ ไม่ระบุสาเหตุ',N'Allergic contact dermatitis, unspecified cause',N'ผื่นแพ้ dermatitis',N'ICD-10-TM 2016',1),
                (N'S019',N'แผลเปิดที่ศีรษะ ไม่ระบุตำแหน่ง',N'Open wound of head, part unspecified',N'แผลศีรษะ open wound',N'ICD-10-TM 2016',1),
                (N'Z000',N'การตรวจสุขภาพทั่วไป',N'General medical examination',N'ตรวจสุขภาพ checkup',N'ICD-10-TM 2016',1);

                UPDATE [TreatmentRecords] SET [PrimaryIcd10Code]=N'J00' WHERE [Diagnosis] LIKE N'%J00%';
                UPDATE [TreatmentRecords] SET [PrimaryIcd10Code]=N'H811' WHERE [Diagnosis] LIKE N'%H811%';
                UPDATE [TreatmentRecords] SET [PrimaryIcd10Code]=N'K0880' WHERE [Diagnosis] LIKE N'%K0880%';
                UPDATE [TreatmentRecords] SET [PrimaryIcd10Code]=N'R21' WHERE [Diagnosis] LIKE N'%R21%';
                UPDATE [TreatmentRecords] SET [PrimaryIcd10Code]=N'M7916' WHERE [Diagnosis] LIKE N'%M7916%';
                UPDATE [TreatmentRecords] SET [PrimaryIcd10Code]=N'M545' WHERE [Diagnosis] LIKE N'%M545%';
                UPDATE [TreatmentRecords] SET [PrimaryIcd10Code]=N'K30' WHERE [Diagnosis] LIKE N'%K30%';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Icd10Codes");

            migrationBuilder.DropColumn(
                name: "DifferentialIcd10Codes",
                table: "TreatmentRecords");

            migrationBuilder.DropColumn(
                name: "PrimaryIcd10Code",
                table: "TreatmentRecords");

        }
    }
}
