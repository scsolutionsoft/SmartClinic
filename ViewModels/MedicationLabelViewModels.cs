using System.ComponentModel.DataAnnotations;
using SmartClinic.Web.Models;

namespace SmartClinic.Web.ViewModels;

public class MedicationLabelInputViewModel
{
    public string PrintMode { get; set; } = "manual";
    public int? OpdPatientId { get; set; }
    public int? TreatmentRecordId { get; set; }
    public string OpdSlotAssignmentsJson { get; set; } = "[]";
    public int? TemplateId { get; set; }
    [StringLength(200)] public string DiseaseCategory { get; set; } = string.Empty;
    [StringLength(200)] public string TemplateName { get; set; } = string.Empty;
    [StringLength(300)] public string MedicineName { get; set; } = string.Empty;
    [StringLength(50)] public string DoseAmount { get; set; } = string.Empty;
    [StringLength(50)] public string FrequencyPerDay { get; set; } = string.Empty;
    [StringLength(30)] public string MealTiming { get; set; } = string.Empty;
    public bool Morning { get; set; }
    public bool Noon { get; set; }
    public bool Evening { get; set; }
    public bool Bedtime { get; set; }
    [StringLength(30)] public string IntervalHours { get; set; } = string.Empty;
    public bool FinishCourse { get; set; }
    public bool MayCauseDrowsiness { get; set; }
    public bool BeforeMeal30Minutes { get; set; }
    public bool ImmediatelyAfterMeal { get; set; }
    [StringLength(500)] public string AdditionalAdvice { get; set; } = string.Empty;
    public int? PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    [Display(Name = "วันที่ (ไม่บังคับ)")]
    public DateTime? LabelDate { get; set; }
    public bool BlankMode { get; set; }
    [Range(1, 8)] public int StartPosition { get; set; } = 1;
    [Range(1, 8)] public int Quantity { get; set; } = 8;
    public decimal OffsetXMillimeters { get; set; }
    public decimal OffsetYMillimeters { get; set; }
}

public class MedicationLabelsDashboardViewModel
{
    public MedicationLabelInputViewModel Input { get; set; } = new();
    public IReadOnlyList<MedicationLabelTemplate> Templates { get; set; } = [];
    public IReadOnlyList<Patient> Patients { get; set; } = [];
    public string ClinicName { get; set; } = string.Empty;
    public string ClinicAddress { get; set; } = string.Empty;
    public string ClinicPhone { get; set; } = string.Empty;
    public string OpeningHours { get; set; } = string.Empty;
}

public class MedicationLabelPrintViewModel : MedicationLabelInputViewModel
{
    public string ClinicName { get; set; } = string.Empty;
    public string ClinicAddress { get; set; } = string.Empty;
    public string ClinicPhone { get; set; } = string.Empty;
    public string OpeningHours { get; set; } = string.Empty;
    public IReadOnlyList<MedicationLabelSlotViewModel> AssignedSlots { get; set; } = [];
}

public class MedicationLabelSlotViewModel
{
    public int Position { get; set; }
    public string MedicineName { get; set; } = string.Empty;
    public string DoseAmount { get; set; } = string.Empty;
    public string FrequencyPerDay { get; set; } = string.Empty;
    public string MealTiming { get; set; } = string.Empty;
    public bool Morning { get; set; }
    public bool Noon { get; set; }
    public bool Evening { get; set; }
    public bool Bedtime { get; set; }
    public string IntervalHours { get; set; } = string.Empty;
    public bool FinishCourse { get; set; }
    public bool MayCauseDrowsiness { get; set; }
    public bool BeforeMeal30Minutes { get; set; }
    public bool ImmediatelyAfterMeal { get; set; }
    public string AdditionalAdvice { get; set; } = string.Empty;
}

public static class NullableDateFormattingExtensions
{
    public static string ToString(this DateTime? value, string format) =>
        value.HasValue ? value.Value.ToString(format) : "...............";
}
