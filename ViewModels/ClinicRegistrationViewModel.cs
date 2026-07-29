using System.ComponentModel.DataAnnotations;

namespace SmartClinic.Web.ViewModels;

public class ClinicRegistrationViewModel
{
    [Required]
    [RegularExpression("^[A-Za-z0-9]{9,10}$", ErrorMessage = "รหัสคลินิกต้องมี 9-10 ตัวและเป็นตัวอักษร/ตัวเลข")]
    [Display(Name = "คลินิกจากระบบ สปสช.")]
    public string SelectedClinicCode { get; set; } = string.Empty;

    public string ClinicName { get; set; } = string.Empty;

    [Display(Name = "โลโก้คลินิก")]
    public IFormFile? Logo { get; set; }

    [Required]
    [Display(Name = "ชื่อ-นามสกุลผู้ลงทะเบียน")]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [Phone]
    [Display(Name = "เบอร์โทรผู้ลงทะเบียน")]
    public string PhoneNumber { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [Display(Name = "E-Mail")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [Display(Name = "ธีมระบบ")]
    public string Theme { get; set; } = "lux";

    public bool IsEditMode { get; set; }

    public bool CanCreateClinic { get; set; }

    public string? CurrentLogoPath { get; set; }

    public IReadOnlyList<NhssoClinicOptionViewModel> NhssoClinics { get; set; } = Array.Empty<NhssoClinicOptionViewModel>();
}

public class NhssoClinicOptionViewModel
{
    public string ClinicCode { get; set; } = string.Empty;

    public string ClinicName { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;
}
