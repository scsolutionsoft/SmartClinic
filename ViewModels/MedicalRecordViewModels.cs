using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace SmartClinic.Web.ViewModels;

public class MedicalRecordCreateViewModel
{
    private static DateTime CurrentMinute => new(
        DateTime.Now.Year,
        DateTime.Now.Month,
        DateTime.Now.Day,
        DateTime.Now.Hour,
        DateTime.Now.Minute,
        0);

    [Required]
    [Display(Name = "ผู้ป่วย")]
    public int? PatientId { get; set; }

    [Required]
    [Display(Name = "วันที่เข้ารักษา")]
    public DateTime VisitDate { get; set; } = CurrentMinute;

    [Display(Name = "เลขประจำตัวผู้รับบริการ")]
    public string? ServiceRecipientId { get; set; }

    [Display(Name = "Authentication Code")]
    public string? AuthenticationCode { get; set; }

    [Display(Name = "ผู้ป่วยประสงค์ชำระค่าบริการเอง")]
    public bool IsSelfPay { get; set; }

    [Display(Name = "เลขบัตรประจำตัวประชาชน")]
    public string? CitizenId { get; set; }

    [Required]
    [Display(Name = "การวินิจฉัย")]
    public string Diagnosis { get; set; } = string.Empty;

    public string? PrimaryIcd10Code { get; set; }

    public string? DifferentialIcd10Codes { get; set; }

    [Display(Name = "การวินิจฉัยแยกอาการเบื้องต้น")]
    public string? InitialDifferentialDiagnosis { get; set; }

    [Display(Name = "อาการสำคัญ (Chief Complaint)")]
    public string? ChiefComplaint { get; set; }

    [Display(Name = "ประวัติเจ็บป่วยปัจจุบัน (Present Illness)")]
    public string? PresentIllness { get; set; }

    [Display(Name = "การตรวจร่างกาย")]
    public string? PhysicalExam { get; set; }

    [Display(Name = "อุณหภูมิ")] public decimal? TemperatureCelsius { get; set; }
    [Display(Name = "ชีพจร")] public int? PulseRate { get; set; }
    [Display(Name = "การหายใจ")] public int? RespiratoryRate { get; set; }
    [Display(Name = "ความดันตัวบน")] public int? SystolicPressure { get; set; }
    [Display(Name = "ความดันตัวล่าง")] public int? DiastolicPressure { get; set; }
    [Display(Name = "น้ำหนัก")] public decimal? WeightKilograms { get; set; }
    [Display(Name = "ส่วนสูง")] public decimal? HeightCentimeters { get; set; }
    [Display(Name = "ดัชนีมวลกาย")] public decimal? BodyMassIndex { get; set; }

    [Display(Name = "การตรวจร่างกายตามปัญหาของผู้ป่วย")]
    public string? ProblemPhysicalExam { get; set; }

    [Display(Name = "การรักษาเบื้องต้นและการให้คำแนะนำ")]
    public string? TreatmentAndAdvice { get; set; }

    [Display(Name = "กรณีเกินศักยภาพของคลินิกได้ส่งต่อไปรับการรักษาที่")]
    public string? ReferralDetail { get; set; }

    [Display(Name = "ไม่นัด")]
    public bool FollowUpNone { get; set; }

    [Display(Name = "ติดตามทางโทรศัพท์")]
    public bool FollowUpPhone { get; set; }

    [Display(Name = "นัดตามที่คลินิก")]
    public bool FollowUpClinic { get; set; }

    [Display(Name = "ระบุ")]
    public string? FollowUpClinicNote { get; set; }

    [Display(Name = "วันและเวลานัดหมาย")]
    public DateTime? FollowUpAppointmentDateTime { get; set; }

    [Display(Name = "ติดตามทางช่องทางอื่น")]
    public bool FollowUpOther { get; set; }

    [Display(Name = "ระบุ")]
    public string? FollowUpOtherNote { get; set; }

    [Display(Name = "เวลาสิ้นสุดการบริการ")]
    public DateTime? ServiceEndDateTime { get; set; }

    [Display(Name = "กรณีเด็ก อายุ 0 - 12 ปี")]
    public bool IsChildCase { get; set; }

    [Display(Name = "การเจริญเติบโต")]
    public string? ChildGrowthStatus { get; set; }

    [Display(Name = "พัฒนาการ")]
    public string? ChildDevelopmentStatus { get; set; }

    [Display(Name = "ประวัติการได้รับวัคซีน")]
    public string? ChildVaccineStatus { get; set; }

    [Display(Name = "ระบุ")]
    public string? ChildVaccineNote { get; set; }

    [Display(Name = "หมายเหตุ")]
    public string? Note { get; set; }

    [Display(Name = "ไฟล์ OPD Card (PDF)")]
    public IFormFile? OpdPdfFile { get; set; }
}

public class MedicalRecordItemViewModel
{
    public int Id { get; set; }

    public int PatientId { get; set; }

    public string PatientName { get; set; } = string.Empty;

    public string CitizenId { get; set; } = string.Empty;

    public string VisitDateText { get; set; } = string.Empty;

    public string ServiceRecipientId { get; set; } = string.Empty;

    public string AuthenticationCode { get; set; } = string.Empty;

    public bool IsSelfPay { get; set; }

    public string Diagnosis { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public string CreatedAtText { get; set; } = string.Empty;
}

public class MedicalRecordReportViewModel
{
    public int RecordId { get; set; }

    public string ClinicCode { get; set; } = string.Empty;

    public string? ClinicLogoUrl { get; set; }

    public string PatientName { get; set; } = string.Empty;

    public string CitizenId { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    public string Gender { get; set; } = string.Empty;

    public string AgeText { get; set; } = string.Empty;

    public string VisitDateText { get; set; } = string.Empty;

    public string VisitDateShortText { get; set; } = string.Empty;

    public string ServiceRecipientId { get; set; } = string.Empty;

    public string AuthenticationCode { get; set; } = string.Empty;

    public string Diagnosis { get; set; } = string.Empty;

    public string InitialDifferentialDiagnosis { get; set; } = string.Empty;

    public string ChiefComplaint { get; set; } = string.Empty;

    public string PresentIllness { get; set; } = string.Empty;

    public string PhysicalExam { get; set; } = string.Empty;
    public decimal? TemperatureCelsius { get; set; }
    public int? PulseRate { get; set; }
    public int? RespiratoryRate { get; set; }
    public int? SystolicPressure { get; set; }
    public int? DiastolicPressure { get; set; }
    public decimal? WeightKilograms { get; set; }
    public decimal? HeightCentimeters { get; set; }
    public decimal? BodyMassIndex { get; set; }

    public string ProblemPhysicalExam { get; set; } = string.Empty;

    public string TreatmentAndAdvice { get; set; } = string.Empty;

    public string ReferralDetail { get; set; } = string.Empty;

    public string FollowUpText { get; set; } = string.Empty;

    public string ServiceEndDateTimeText { get; set; } = string.Empty;

    public string AuthenticationTimeText { get; set; } = string.Empty;

    public string ProviderName { get; set; } = string.Empty;

    public string ProviderProfessionalTitle { get; set; } = string.Empty;

    public string? ProviderSignatureImageUrl { get; set; }

    public bool HasProviderSignature { get; set; }

    public string BarcodeText { get; set; } = string.Empty;

    public string? Note { get; set; }

    public string PreviewPdfUrl { get; set; } = string.Empty;

    public string? SignatureImageUrl { get; set; }

    public bool HasSignature { get; set; }

    public string? PatientPhotoUrl { get; set; }

    public bool HasPatientPhoto { get; set; }
}

public class MedicalRecordsDashboardViewModel
{
    public const int PageSize = 20;

    public MedicalRecordCreateViewModel Input { get; set; } = new();

    public int? EditRecordId { get; set; }

    public IReadOnlyList<SelectListItem> PatientOptions { get; set; } = Array.Empty<SelectListItem>();

    public IReadOnlyList<MedicalRecordItemViewModel> Records { get; set; } = Array.Empty<MedicalRecordItemViewModel>();

    public int CurrentPage { get; set; } = 1;

    public int TotalItems { get; set; }

    public int TotalPages { get; set; }

    public string SearchTerm { get; set; } = string.Empty;

    public string? StatusMessage { get; set; }

    public int OpdRecordLimit { get; set; } = 30;

    public int UsedOpdRecords { get; set; }

    public bool IsUnlimited { get; set; }

    public int RemainingOpdRecords => IsUnlimited ? int.MaxValue : Math.Max(0, OpdRecordLimit - UsedOpdRecords);

    public string ContactName { get; set; } = string.Empty;

    public string ContactPhone { get; set; } = string.Empty;
}

public class SelfPayConsentViewModel
{
    public int RecordId { get; set; }

    public string ClinicName { get; set; } = string.Empty;

    public string ClinicAddress { get; set; } = string.Empty;

    public string ClinicPhoneNumber { get; set; } = string.Empty;

    public string ClinicCode { get; set; } = string.Empty;

    public string? ClinicLogoUrl { get; set; }

    public string PatientName { get; set; } = string.Empty;

    public string CitizenId { get; set; } = string.Empty;

    public string VisitDateText { get; set; } = string.Empty;

    public string AuthenticationCode { get; set; } = string.Empty;

    public string? PatientSignatureUrl { get; set; }

    public bool HasPatientSignature { get; set; }

    public string ProviderName { get; set; } = string.Empty;

    public string ProviderProfessionalTitle { get; set; } = string.Empty;

    public string? ProviderSignatureUrl { get; set; }

    public bool HasProviderSignature { get; set; }
}

public class PatientSearchResultViewModel
{
    public int Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string CitizenId { get; set; } = string.Empty;

    public string ServiceRecipientId { get; set; } = string.Empty;

    public string Text => $"{FullName} ({CitizenId})";
}

public class MedicalRecordPdfImportViewModel
{
    public string PatientName { get; set; } = string.Empty;

    public string ServiceRecipientId { get; set; } = string.Empty;

    public string VisitDate { get; set; } = string.Empty;

    public string AuthenticationCode { get; set; } = string.Empty;

    public string InitialDifferentialDiagnosis { get; set; } = string.Empty;

    public string Diagnosis { get; set; } = string.Empty;

    public string ChiefComplaint { get; set; } = string.Empty;

    public string PresentIllness { get; set; } = string.Empty;

    public string PhysicalExam { get; set; } = string.Empty;
    public decimal? TemperatureCelsius { get; set; }
    public int? PulseRate { get; set; }
    public int? RespiratoryRate { get; set; }
    public int? SystolicPressure { get; set; }
    public int? DiastolicPressure { get; set; }
    public decimal? WeightKilograms { get; set; }
    public decimal? HeightCentimeters { get; set; }
    public decimal? BodyMassIndex { get; set; }

    public string ProblemPhysicalExam { get; set; } = string.Empty;

    public string TreatmentAndAdvice { get; set; } = string.Empty;

    public string ReferralDetail { get; set; } = string.Empty;

    public bool FollowUpNone { get; set; }

    public bool FollowUpPhone { get; set; }

    public bool FollowUpClinic { get; set; }

    public string FollowUpClinicNote { get; set; } = string.Empty;

    public bool FollowUpOther { get; set; }

    public string FollowUpOtherNote { get; set; } = string.Empty;

    public string ServiceEndDateTime { get; set; } = string.Empty;

    public bool IsChildCase { get; set; }

    public string ChildGrowthStatus { get; set; } = string.Empty;

    public string ChildDevelopmentStatus { get; set; } = string.Empty;

    public string ChildVaccineStatus { get; set; } = string.Empty;

    public string ChildVaccineNote { get; set; } = string.Empty;
}
