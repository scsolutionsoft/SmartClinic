using System.ComponentModel.DataAnnotations;
using SmartClinic.Web.Models;

namespace SmartClinic.Web.ViewModels;

public class DrugCatalogDashboardViewModel
{
    public IReadOnlyList<ClinicDrug> Drugs { get; set; } = [];
    public IReadOnlyList<DrugAdviceTemplate> AdviceTemplates { get; set; } = [];
    public IReadOnlyList<DrugKnowledgeAuditLog> RecentAudit { get; set; } = [];
    public IReadOnlyList<IcdDiseaseDrugReportViewModel> DiseaseDrugReports { get; set; } = [];
    public IReadOnlyDictionary<string, string> Icd10ThaiNames { get; set; } = new Dictionary<string, string>();
    public IReadOnlyList<string> ManufacturerNames { get; set; } = [];
    public string SearchTerm { get; set; } = string.Empty;
    public string Icd10Code { get; set; } = string.Empty;
    public int ActiveDrugCount { get; set; }
    public int PendingCount { get; set; }
    public int DuplicateCandidateCount { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalCount { get; set; }
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
}

public class IcdDiseaseDrugReportViewModel
{
    public string Icd10Code { get; set; } = string.Empty;
    public string ThaiName { get; set; } = string.Empty;
    public string EnglishName { get; set; } = string.Empty;
    public IReadOnlyList<IcdDiseaseDrugItemViewModel> Drugs { get; set; } = [];
}

public class IcdDiseaseDrugItemViewModel
{
    public int DrugId { get; set; }
    public string GenericName { get; set; } = string.Empty;
    public string TradeName { get; set; } = string.Empty;
    public string ManufacturerName { get; set; } = string.Empty;
    public string Strength { get; set; } = string.Empty;
    public string DosageForm { get; set; } = string.Empty;
    public string UsageText { get; set; } = string.Empty;
}

public class DrugAdviceDashboardViewModel
{
    public IReadOnlyList<DrugAdviceTemplate> Items { get; set; } = [];
    public IReadOnlyList<DrugKnowledgeAuditLog> RecentAudit { get; set; } = [];
    public IReadOnlyDictionary<string, string> Icd10ThaiNames { get; set; } = new Dictionary<string, string>();
    public string SearchTerm { get; set; } = string.Empty;
    public string Icd10Code { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int ActiveCount { get; set; }
    public int WarningCount { get; set; }
    public int FollowUpCount { get; set; }
}

public class ClinicDrugInputViewModel
{
    public int? Id { get; set; }
    [Required, StringLength(300)] public string GenericName { get; set; } = string.Empty;
    [StringLength(300)] public string TradeName { get; set; } = string.Empty;
    [StringLength(300)] public string ManufacturerName { get; set; } = string.Empty;
    [StringLength(100)] public string Strength { get; set; } = string.Empty;
    [StringLength(100)] public string DosageForm { get; set; } = string.Empty;
    [StringLength(50)] public string Unit { get; set; } = string.Empty;
    [StringLength(50)] public string TmtCode { get; set; } = string.Empty;
    [StringLength(100)] public string RegistrationNumber { get; set; } = string.Empty;
    [StringLength(50)] public string Source { get; set; } = "Clinic";
    [StringLength(100)] public string DoseAmount { get; set; } = string.Empty;
    [StringLength(50)] public string FrequencyPerDay { get; set; } = string.Empty;
    [StringLength(50)] public string MealTiming { get; set; } = string.Empty;
    public bool Morning { get; set; }
    public bool Noon { get; set; }
    public bool Evening { get; set; }
    public bool Bedtime { get; set; }
    [StringLength(30)] public string IntervalHours { get; set; } = string.Empty;
    [StringLength(1000)] public string UsageText { get; set; } = string.Empty;
    [StringLength(1000)] public string AdviceText { get; set; } = string.Empty;
}

public class ClinicDrugDetailsInputViewModel : ClinicDrugInputViewModel
{
    public List<IcdDrugProtocolInputViewModel> Protocols { get; set; } = [];
}

public class IcdDrugProtocolInputViewModel
{
    public int? Id { get; set; }
    [Required] public int ClinicDrugId { get; set; }
    [Required, StringLength(10)] public string Icd10Code { get; set; } = string.Empty;
    [StringLength(30)] public string DiagnosisType { get; set; } = "Primary";
    public int DisplayOrder { get; set; }
}

public class DrugAdviceInputViewModel
{
    public int? Id { get; set; }
    [Required, StringLength(10)] public string Icd10Code { get; set; } = string.Empty;
    [StringLength(50)] public string Category { get; set; } = "General";
    [Required, StringLength(1000)] public string Text { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
}
