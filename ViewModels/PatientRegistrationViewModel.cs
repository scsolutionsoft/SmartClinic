using System.ComponentModel.DataAnnotations;

namespace SmartClinic.Web.ViewModels;

public class PatientRegistrationViewModel
{
    [Required]
    [StringLength(13, MinimumLength = 13)]
    [Display(Name = "เลขบัตรประชาชน")]
    public string CitizenId { get; set; } = string.Empty;

    [Required]
    [Display(Name = "ชื่อ-นามสกุลผู้ป่วย")]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [Display(Name = "ที่อยู่")]
    public string Address { get; set; } = string.Empty;

    [Phone]
    [Display(Name = "เบอร์โทร")]
    public string? PhoneNumber { get; set; }

    [Display(Name = "วันเกิด")]
    public DateOnly? BirthDate { get; set; }

    [Required]
    [Display(Name = "เพศ")]
    public string Gender { get; set; } = "ชาย";

    [Display(Name = "รูปภาพผู้ป่วย")]
    public IFormFile? Photo { get; set; }

    [Display(Name = "ไฟล์ PDF เวชระเบียน")]
    public IFormFile? MedicalRecordPdf { get; set; }

    /// <summary>Base64-encoded photo from smart card (used when no file uploaded)</summary>
    public string? PhotoBase64 { get; set; }

    [Display(Name = "รหัสคลินิก")]
    public string? ClinicCode { get; set; }
}

public class PatientSummaryViewModel
{
    public string CitizenId { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public string Gender { get; set; } = string.Empty;

    public string CreatedAt { get; set; } = string.Empty;

    public bool HasPhoto { get; set; }
}

public class PatientDashboardViewModel
{
    public const int PageSize = 20;

    public PatientRegistrationViewModel Input { get; set; } = new();

    public IReadOnlyList<PatientSummaryViewModel> RecentPatients { get; set; } = Array.Empty<PatientSummaryViewModel>();

    public int CurrentPage { get; set; } = 1;

    public int TotalItems { get; set; }

    public int TotalPages { get; set; }

    public string SearchTerm { get; set; } = string.Empty;

    public string? StatusMessage { get; set; }

    public int PatientLimit { get; set; }

    public bool IsUnlimited { get; set; }

    public int RemainingPatients => IsUnlimited ? int.MaxValue : Math.Max(0, PatientLimit - TotalItems);

    public string ContactName { get; set; } = string.Empty;

    public string ContactPhone { get; set; } = string.Empty;
}

public class PatientEditViewModel
{
    public string CitizenId { get; set; } = string.Empty;

    [Required]
    [Display(Name = "ชื่อ-นามสกุลผู้ป่วย")]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [Display(Name = "ที่อยู่")]
    public string Address { get; set; } = string.Empty;

    [Phone]
    [Display(Name = "เบอร์โทร")]
    public string? PhoneNumber { get; set; }

    [Display(Name = "วันเกิด")]
    public DateOnly? BirthDate { get; set; }

    [Required]
    [Display(Name = "เพศ")]
    public string Gender { get; set; } = "ชาย";

    [Display(Name = "อัปโหลดรูปใหม่")]
    public IFormFile? Photo { get; set; }

    /// <summary>Base64-encoded photo from smart card (used when no file uploaded)</summary>
    public string? PhotoBase64 { get; set; }

    public bool HasPhoto { get; set; }

    public string? ClinicCode { get; set; }
}
