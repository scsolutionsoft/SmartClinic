namespace SmartClinic.Web.Models;

public class ClinicDrug
{
    public int Id { get; set; }
    public string ClinicCode { get; set; } = string.Empty;
    public string GenericName { get; set; } = string.Empty;
    public string TradeName { get; set; } = string.Empty;
    public string ManufacturerName { get; set; } = string.Empty;
    public string Strength { get; set; } = string.Empty;
    public string DosageForm { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public string TmtCode { get; set; } = string.Empty;
    public string RegistrationNumber { get; set; } = string.Empty;
    public string Source { get; set; } = "Clinic";
    public string DoseAmount { get; set; } = string.Empty;
    public string FrequencyPerDay { get; set; } = string.Empty;
    public string MealTiming { get; set; } = string.Empty;
    public bool Morning { get; set; }
    public bool Noon { get; set; }
    public bool Evening { get; set; }
    public bool Bedtime { get; set; }
    public string IntervalHours { get; set; } = string.Empty;
    public string UsageText { get; set; } = string.Empty;
    public string AdviceText { get; set; } = string.Empty;
    public string ApprovalStatus { get; set; } = "Approved";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public string CreatedByUserId { get; set; } = string.Empty;
    public ICollection<IcdDrugProtocol> Protocols { get; set; } = new List<IcdDrugProtocol>();
}

public class IcdDrugProtocol
{
    public int Id { get; set; }
    public string ClinicCode { get; set; } = string.Empty;
    public int ClinicDrugId { get; set; }
    public ClinicDrug Drug { get; set; } = null!;
    public string Icd10Code { get; set; } = string.Empty;
    public string DiagnosisType { get; set; } = "Primary";
    public int DisplayOrder { get; set; }
    public int UsageCount { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class DrugAdviceTemplate
{
    public int Id { get; set; }
    public string ClinicCode { get; set; } = string.Empty;
    public string Icd10Code { get; set; } = string.Empty;
    public string Category { get; set; } = "General";
    public string Text { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class DrugKnowledgeAuditLog
{
    public int Id { get; set; }
    public string ClinicCode { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public int EntityId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
