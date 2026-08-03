using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace SmartClinic.Web.ViewModels;

public class WoundCareInputViewModel
{
    public int? Id { get; set; }
    [Required] public int? PatientId { get; set; }
    [Required] public DateTime VisitDate { get; set; } = DateTime.Now;
    public string? ServiceRecipientId { get; set; }
    public string? AuthenticationCode { get; set; }
    [Required] public string WoundCause { get; set; } = string.Empty;
    [Required] public string WoundLocation { get; set; } = string.Empty;
    public bool IsBruiseOrSutured { get; set; }
    public bool IsOpenInfected { get; set; }
    public bool IsLargeOpenWound { get; set; }
    public string? WoundSize { get; set; }
    public int? DressingSequence { get; set; }
    public string? WoundProgress { get; set; }
    public string? WoundProgressNote { get; set; }
    public decimal? TemperatureCelsius { get; set; }
    public int? PulseRate { get; set; }
    public int? RespiratoryRate { get; set; }
    public int? SystolicPressure { get; set; }
    public int? DiastolicPressure { get; set; }
    public string? PostDressingResult { get; set; }
    public bool FollowUpNone { get; set; }
    public bool FollowUpPhone { get; set; }
    public bool FollowUpClinic { get; set; }
    public string? FollowUpNote { get; set; }
    public bool FollowUpOther { get; set; }
    public string? WoundAssessment { get; set; }
    public string? PatientAdvice { get; set; }
    public IFormFile? SourceDocument { get; set; }
    public List<IFormFile> WoundPhotos { get; set; } = new();
}

public class WoundCareDashboardViewModel
{
    public WoundCareInputViewModel Input { get; set; } = new();
    public IReadOnlyList<SelectListItem> Patients { get; set; } = Array.Empty<SelectListItem>();
    public IReadOnlyList<SmartClinic.Web.Models.WoundCareRecord> Records { get; set; } = Array.Empty<SmartClinic.Web.Models.WoundCareRecord>();
}

public class WoundCareOcrResultViewModel
{
    public string RawText { get; set; } = string.Empty;
    public string ServiceRecipientId { get; set; } = string.Empty;
    public string AuthenticationCode { get; set; } = string.Empty;
    public string WoundCause { get; set; } = string.Empty;
    public string WoundLocation { get; set; } = string.Empty;
    public string WoundSize { get; set; } = string.Empty;
    public decimal? TemperatureCelsius { get; set; }
    public int? PulseRate { get; set; }
    public int? RespiratoryRate { get; set; }
    public int? SystolicPressure { get; set; }
    public int? DiastolicPressure { get; set; }
}
