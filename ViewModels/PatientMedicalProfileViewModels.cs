using System.ComponentModel.DataAnnotations;

namespace SmartClinic.Web.ViewModels;

public class PatientMedicalProfileCreateViewModel
{
    [Display(Name = "ผู้ป่วย")]
    public int? PatientId { get; set; }

    [Display(Name = "ค้นหาผู้ป่วย")]
    public string? PatientSearchText { get; set; }

    [Display(Name = "เลขประจำตัวผู้รับบริการ")]
    public string? ServiceRecipientId { get; set; }

    [Display(Name = "เลขบัตรประจำตัวประชาชน")]
    public string? CitizenId { get; set; }

    [Display(Name = "ชื่อคลินิก")]
    public string? ClinicName { get; set; }

    [Display(Name = "ที่ตั้งคลินิก")]
    public string? ClinicAddress { get; set; }

    [Display(Name = "ชื่อ-สกุล")]
    public string? PatientName { get; set; }

    [Display(Name = "เพศ")]
    public string? Gender { get; set; }

    [Display(Name = "วัน/เดือน/ปีเกิด")]
    public DateOnly? BirthDate { get; set; }

    [Display(Name = "เชื้อชาติ")]
    public string? Race { get; set; }

    [Display(Name = "สัญชาติ")]
    public string? Nationality { get; set; }

    [Display(Name = "สถานภาพ")]
    public string? MaritalStatus { get; set; }

    [Display(Name = "ที่อยู่ตามบัตรประชาชน")]
    public string? RegisteredAddress { get; set; }

    [Display(Name = "ที่อยู่ที่สามารถติดต่อได้")]
    public string? ContactAddress { get; set; }

    [Display(Name = "เบอร์โทรศัพท์")]
    public string? PhoneNumber { get; set; }

    [Display(Name = "โรงพยาบาล / หน่วยบริการประจำ")]
    public string? PrimaryHospital { get; set; }

    [Display(Name = "โรคประจำตัว")]
    public string? UnderlyingDisease { get; set; }

    [Display(Name = "ประวัติเจ็บป่วยในอดีต (Past History)")]
    public string? PastHistory { get; set; } = "ปฏิเสธ";

    [Display(Name = "ประวัติครอบครัว (Family History)")]
    public string? FamilyHistory { get; set; } = "ปฏิเสธ";

    [Display(Name = "ประวัติการแพ้ยา/อาหาร/สารเคมี")]
    public string? AllergyHistory { get; set; } = "ปฏิเสธ";

    [Display(Name = "สิทธิการรักษาพยาบาล")]
    public string? MedicalBenefit { get; set; }

    [Display(Name = "ผู้ติดต่อกรณีฉุกเฉิน")]
    public string? EmergencyContactName { get; set; }

    [Display(Name = "เบอร์โทรผู้ติดต่อฉุกเฉิน")]
    public string? EmergencyContactPhone { get; set; }

    [Display(Name = "วัน/เดือน/ปี ที่ให้ข้อมูล")]
    public DateOnly? InformationGivenDate { get; set; }

    [Display(Name = "ไฟล์ PDF เวชระเบียนผู้รับบริการ")]
    public IFormFile? SourcePdfFile { get; set; }
}

public class PatientMedicalProfileItemViewModel
{
    public int Id { get; set; }

    public string PatientName { get; set; } = string.Empty;

    public string CitizenId { get; set; } = string.Empty;

    public string ServiceRecipientId { get; set; } = string.Empty;

    public string InformationGivenDateText { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    public string MedicalBenefit { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public bool HasPdfFile { get; set; }

    public string CreatedAtText { get; set; } = string.Empty;
}

public class PatientMedicalProfilesDashboardViewModel
{
    public PatientMedicalProfileCreateViewModel Input { get; set; } = new();

    public IReadOnlyList<PatientMedicalProfileItemViewModel> Profiles { get; set; } = Array.Empty<PatientMedicalProfileItemViewModel>();

    public string SearchTerm { get; set; } = string.Empty;

    public string? StatusMessage { get; set; }
}

public class PatientMedicalProfileEditViewModel
{
    public int Id { get; set; }

    [Display(Name = "เลขประจำตัวผู้รับบริการ")]
    public string? ServiceRecipientId { get; set; }

    [Display(Name = "เลขบัตรประจำตัวประชาชน")]
    public string? CitizenId { get; set; }

    [Display(Name = "ชื่อ-สกุล")]
    public string? PatientName { get; set; }

    [Display(Name = "เพศ")]
    public string? Gender { get; set; }

    [Display(Name = "วัน/เดือน/ปีเกิด")]
    public DateOnly? BirthDate { get; set; }

    [Display(Name = "เชื้อชาติ")]
    public string? Race { get; set; }

    [Display(Name = "สัญชาติ")]
    public string? Nationality { get; set; }

    [Display(Name = "สถานภาพ")]
    public string? MaritalStatus { get; set; }

    [Display(Name = "ที่อยู่ตามบัตรประชาชน")]
    public string? RegisteredAddress { get; set; }

    [Display(Name = "ที่อยู่ที่สามารถติดต่อได้")]
    public string? ContactAddress { get; set; }

    [Display(Name = "เบอร์โทรศัพท์")]
    public string? PhoneNumber { get; set; }

    [Display(Name = "โรงพยาบาล / หน่วยบริการประจำ")]
    public string? PrimaryHospital { get; set; }

    [Display(Name = "โรคประจำตัว")]
    public string? UnderlyingDisease { get; set; }

    [Display(Name = "ประวัติเจ็บป่วยในอดีต (Past History)")]
    public string? PastHistory { get; set; } = "ปฏิเสธ";

    [Display(Name = "ประวัติครอบครัว (Family History)")]
    public string? FamilyHistory { get; set; } = "ปฏิเสธ";

    [Display(Name = "ประวัติการแพ้ยา/อาหาร/สารเคมี")]
    public string? AllergyHistory { get; set; } = "ปฏิเสธ";

    [Display(Name = "สิทธิการรักษาพยาบาล")]
    public string? MedicalBenefit { get; set; }

    [Display(Name = "ผู้ติดต่อกรณีฉุกเฉิน")]
    public string? EmergencyContactName { get; set; }

    [Display(Name = "เบอร์โทรผู้ติดต่อฉุกเฉิน")]
    public string? EmergencyContactPhone { get; set; }

    [Display(Name = "วัน/เดือน/ปี ที่ให้ข้อมูล")]
    public DateOnly? InformationGivenDate { get; set; }

    [Display(Name = "เปลี่ยนไฟล์ PDF เวชระเบียนผู้รับบริการ")]
    public IFormFile? SourcePdfFile { get; set; }

    public string? CurrentPdfFileName { get; set; }

    public bool HasPdfFile { get; set; }
}

public class PatientMedicalProfilePdfImportViewModel
{
    public string PatientName { get; set; } = string.Empty;

    public string CitizenId { get; set; } = string.Empty;

    public string ServiceRecipientId { get; set; } = string.Empty;

    public string ClinicName { get; set; } = string.Empty;

    public string ClinicAddress { get; set; } = string.Empty;

    public string Gender { get; set; } = string.Empty;

    public string BirthDate { get; set; } = string.Empty;

    public string Race { get; set; } = string.Empty;

    public string Nationality { get; set; } = string.Empty;

    public string MaritalStatus { get; set; } = string.Empty;

    public string RegisteredAddress { get; set; } = string.Empty;

    public string ContactAddress { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    public string PrimaryHospital { get; set; } = string.Empty;

    public string UnderlyingDisease { get; set; } = string.Empty;

    public string PastHistory { get; set; } = string.Empty;

    public string FamilyHistory { get; set; } = string.Empty;

    public string AllergyHistory { get; set; } = string.Empty;

    public string MedicalBenefit { get; set; } = string.Empty;

    public string EmergencyContactName { get; set; } = string.Empty;

    public string EmergencyContactPhone { get; set; } = string.Empty;

    public string InformationGivenDate { get; set; } = string.Empty;
}
