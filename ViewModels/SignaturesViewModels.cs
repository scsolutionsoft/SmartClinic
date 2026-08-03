using System.ComponentModel.DataAnnotations;

namespace SmartClinic.Web.ViewModels;

public class SignatureUploadViewModel
{
    [Display(Name = "เลขบัตรประชาชน")]
    public string CitizenId { get; set; } = string.Empty;

    [Display(Name = "ไฟล์ลายเซ็น")]
    public IFormFile? SignatureFile { get; set; }

    [Display(Name = "อัปโหลดหลายไฟล์")]
    public List<IFormFile> BatchFiles { get; set; } = new();
}

public class SignatureItemViewModel
{
    public int Id { get; set; }

    public string CitizenId { get; set; } = string.Empty;

    public string FullName { get; set; } = "ไม่พบข้อมูลผู้ป่วย";

    public string PatientPhotoUrl { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public string UploadedAtText { get; set; } = string.Empty;
}

public class SignaturesDashboardViewModel
{
    public const int PageSize = 20;

    public SignatureUploadViewModel Input { get; set; } = new();

    public IReadOnlyList<SignatureItemViewModel> Items { get; set; } = Array.Empty<SignatureItemViewModel>();

    public int CurrentPage { get; set; } = 1;

    public int TotalItems { get; set; }

    public int TotalPages { get; set; }

    public string SearchTerm { get; set; } = string.Empty;

    public string? StatusMessage { get; set; }
}
