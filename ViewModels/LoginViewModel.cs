using System.ComponentModel.DataAnnotations;

namespace SmartClinic.Web.ViewModels;

public class LoginViewModel
{
    [Required]
    [Display(Name = "รหัสคลินิก")]
    public string UserName { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "รหัสผ่าน")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "จดจำการเข้าสู่ระบบ")]
    public bool RememberMe { get; set; }
}