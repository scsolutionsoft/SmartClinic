using System.ComponentModel.DataAnnotations;
using SmartClinic.Web.Models;

namespace SmartClinic.Web.ViewModels;

public class PublicSignupViewModel
{
    [Required, RegularExpression("^[A-Za-z0-9]{9,10}$", ErrorMessage = "รหัสคลินิกต้องเป็นตัวอักษรหรือตัวเลข 9-10 ตัว")]
    [Display(Name = "รหัสคลินิก")]
    public string ClinicCode { get; set; } = string.Empty;
    [Required, StringLength(200), Display(Name = "ชื่อคลินิก")]
    public string ClinicName { get; set; } = string.Empty;
    [Required, StringLength(200), Display(Name = "ชื่อ-นามสกุลผู้ดูแลคลินิก")]
    public string FullName { get; set; } = string.Empty;
    [Required, Phone, Display(Name = "เบอร์โทรศัพท์")]
    public string PhoneNumber { get; set; } = string.Empty;
    [Required, EmailAddress, Display(Name = "อีเมล")]
    public string Email { get; set; } = string.Empty;
    [Required, StringLength(500), Display(Name = "ที่อยู่คลินิก")]
    public string Address { get; set; } = string.Empty;
    [Range(typeof(bool), "true", "true", ErrorMessage = "กรุณายอมรับเงื่อนไขการใช้งาน")]
    public bool AcceptTerms { get; set; }
    public IReadOnlyList<PublicAnnouncement> Announcements { get; set; } = [];
    public IReadOnlyList<PromotionalMedia> PromotionalMedia { get; set; } = [];
}

public class SignupSuccessViewModel
{
    public string ClinicName { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string TemporaryPassword { get; set; } = string.Empty;
    public int PatientLimit { get; set; }
}

public class CreditRequestInputViewModel
{
    [Required] public string ContactName { get; set; } = string.Empty;
    [Required, Phone] public string PhoneNumber { get; set; } = string.Empty;
    [Range(1, 100000)] public int? RequestedCredits { get; set; }
    public bool RequestUnlimited { get; set; }
    [StringLength(1000)] public string? Note { get; set; }
}

public class SubscriptionAdminViewModel
{
    public IReadOnlyList<ClinicQuotaItemViewModel> Clinics { get; set; } = [];
    public IReadOnlyList<CreditRequest> Requests { get; set; } = [];
    public IReadOnlyList<PublicAnnouncement> Announcements { get; set; } = [];
    public IReadOnlyList<PromotionalMedia> PromotionalMedia { get; set; } = [];
}

public class ClinicQuotaItemViewModel
{
    public string ClinicCode { get; set; } = string.Empty;
    public string ClinicName { get; set; } = string.Empty;
    public int UsedPatients { get; set; }
    public int PatientLimit { get; set; }
    public bool IsUnlimited { get; set; }
    public int Remaining => IsUnlimited ? int.MaxValue : Math.Max(0, PatientLimit - UsedPatients);
}

public class QuotaUpdateViewModel
{
    [Required] public string ClinicCode { get; set; } = string.Empty;
    [Range(0, 1000000)] public int PatientLimit { get; set; }
    public bool IsUnlimited { get; set; }
    [Required, StringLength(500)] public string Reason { get; set; } = string.Empty;
}

public class AnnouncementInputViewModel
{
    public int? Id { get; set; }
    [Required, StringLength(200)] public string Title { get; set; } = string.Empty;
    [Required, StringLength(1000)] public string Summary { get; set; } = string.Empty;
    [Required, StringLength(40)] public string BadgeText { get; set; } = "ข่าวสาร";
    [StringLength(500)] public string? LinkUrl { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsPublished { get; set; } = true;
}

public class PromotionalMediaInputViewModel
{
    public int? Id { get; set; }
    [Required, StringLength(200)] public string Title { get; set; } = string.Empty;
    [StringLength(1000)] public string? Description { get; set; }
    [Required] public string MediaType { get; set; } = "Video";
    [Required] public string Placement { get; set; } = "Board";
    [Url, StringLength(500)] public string? YouTubeUrl { get; set; }
    public IFormFile? VideoFile { get; set; }
    public IFormFile? PosterFile { get; set; }
    public bool AutoPlay { get; set; }
    public bool Loop { get; set; }
    public bool IsPublished { get; set; } = true;
    public int DisplayOrder { get; set; }
    public DateTime? StartsAt { get; set; }
    public DateTime? EndsAt { get; set; }
}
