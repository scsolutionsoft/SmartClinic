using System.ComponentModel.DataAnnotations;

namespace SmartClinic.Web.ViewModels;

public class NhssoMasterImportViewModel
{
    [Required]
    [Display(Name = "ไฟล์ NHSO CSV")]
    public IFormFile? CsvFile { get; set; }
}

public class NhssoMasterItemViewModel
{
    public int Id { get; set; }

    public string ClinicCode { get; set; } = string.Empty;

    public string ClinicName { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public string? ContactPhone { get; set; }

    public string? ContactEmail { get; set; }

    public bool IsActive { get; set; }
}

public class NhssoMasterDashboardViewModel
{
    public NhssoMasterImportViewModel ImportInput { get; set; } = new();

    public NhssoMasterEditViewModel CreateInput { get; set; } = new();

    public IReadOnlyList<NhssoMasterItemViewModel> Items { get; set; } = Array.Empty<NhssoMasterItemViewModel>();

    public string? StatusMessage { get; set; }
}

public class NhssoMasterEditViewModel
{
    public int Id { get; set; }

    [Required]
    [RegularExpression("^[A-Za-z0-9]{9,10}$", ErrorMessage = "รหัสคลินิกต้องเป็นตัวอักษร/ตัวเลข 9-10 ตัว")]
    [Display(Name = "รหัสคลินิก")]
    public string ClinicCode { get; set; } = string.Empty;

    [Required]
    [Display(Name = "ชื่อคลินิก")]
    public string ClinicName { get; set; } = string.Empty;

    [Required]
    [Display(Name = "ที่อยู่")]
    public string Address { get; set; } = string.Empty;

    [Display(Name = "เบอร์ติดต่อ")]
    public string? ContactPhone { get; set; }

    [EmailAddress]
    [Display(Name = "อีเมล")]
    public string? ContactEmail { get; set; }

    [Display(Name = "ใช้งาน")]
    public bool IsActive { get; set; } = true;
}