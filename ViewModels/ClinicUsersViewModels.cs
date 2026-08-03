using System.ComponentModel.DataAnnotations;

namespace SmartClinic.Web.ViewModels;

public class CreateClinicUserViewModel
{
    [Required]
    [RegularExpression("^(นาย|นาง|นางสาว)$")]
    [Display(Name = "คำนำหน้าชื่อ")]
    public string NamePrefix { get; set; } = string.Empty;

    [Required]
    [Display(Name = "ชื่อผู้ใช้")]
    public string UserName { get; set; } = string.Empty;

    [Required]
    [Display(Name = "ชื่อ-นามสกุล")]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [Phone]
    [Display(Name = "เบอร์โทร")]
    public string PhoneNumber { get; set; } = string.Empty;

    [EmailAddress]
    [Display(Name = "อีเมล")]
    public string? Email { get; set; }

    [Required]
    [Display(Name = "บทบาท")]
    public string RoleName { get; set; } = "Nurse";

    [Display(Name = "รหัสคลินิก")]
    public string? ClinicCode { get; set; }

    [Display(Name = "ตำแหน่ง/วิชาชีพ")]
    public string? ProfessionalTitle { get; set; }

    [Display(Name = "เลขใบประกอบวิชาชีพ")]
    public string? LicenseNo { get; set; }
}

public class ClinicUserItemViewModel
{
    public string Id { get; set; } = string.Empty;

    public string UserName { get; set; } = string.Empty;

    public string NamePrefix { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string ClinicCode { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    public string RoleName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string ProfessionalTitle { get; set; } = string.Empty;

    public string LicenseNo { get; set; } = string.Empty;

    public bool HasProviderSignature { get; set; }

    public bool CanDelete { get; set; }

    public bool MustChangePassword { get; set; }
}

public class EditClinicUserViewModel
{
    [Required]
    public string Id { get; set; } = string.Empty;

    [Required]
    [RegularExpression("^(นาย|นาง|นางสาว)$")]
    [Display(Name = "คำนำหน้าชื่อ")]
    public string NamePrefix { get; set; } = string.Empty;

    [Required]
    [Display(Name = "ชื่อ-นามสกุล")]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [Phone]
    [Display(Name = "เบอร์โทร")]
    public string PhoneNumber { get; set; } = string.Empty;

    [EmailAddress]
    [Display(Name = "อีเมล")]
    public string? Email { get; set; }

    [Required]
    [Display(Name = "บทบาท")]
    public string RoleName { get; set; } = string.Empty;

    public string? ProfessionalTitle { get; set; }

    public string? LicenseNo { get; set; }
}

public class ClinicUsersDashboardViewModel
{
    public CreateClinicUserViewModel Input { get; set; } = new();

    public IReadOnlyList<string> RoleOptions { get; set; } = Array.Empty<string>();

    public IReadOnlyList<string> ClinicOptions { get; set; } = Array.Empty<string>();

    public IReadOnlyList<ClinicUserItemViewModel> Users { get; set; } = Array.Empty<ClinicUserItemViewModel>();

    public string? StatusMessage { get; set; }
}
