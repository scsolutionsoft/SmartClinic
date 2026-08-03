namespace SmartClinic.Web.Models;

public class MedicationLabelTemplate
{
    public int Id { get; set; }
    public string ClinicCode { get; set; } = string.Empty;
    public string DiseaseCategory { get; set; } = string.Empty;
    public string TemplateName { get; set; } = string.Empty;
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
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
