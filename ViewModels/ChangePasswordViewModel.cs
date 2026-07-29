using System.ComponentModel.DataAnnotations;

namespace SmartClinic.Web.ViewModels;

public class ChangePasswordViewModel
{
    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "รหัสผ่านเดิม")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "รหัสผ่านใหม่")]
    [MinLength(6)]
    public string NewPassword { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword), ErrorMessage = "รหัสผ่านใหม่ไม่ตรงกัน")]
    [Display(Name = "ยืนยันรหัสผ่านใหม่")]
    public string ConfirmPassword { get; set; } = string.Empty;
}