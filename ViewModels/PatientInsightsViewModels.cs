namespace SmartClinic.Web.ViewModels;

public class PatientInsightsViewModel
{
    public string Search { get; set; } = string.Empty;
    public int? PatientId { get; set; }
    public int? Month { get; set; }
    public int? Year { get; set; }
    public List<PatientInsightsSearchItem> Patients { get; set; } = [];
    public PatientInsightsProfile? Profile { get; set; }
    public List<PatientInsightsVisit> Visits { get; set; } = [];
    public List<PatientInsightsVital> Vitals { get; set; } = [];
    public List<PatientInsightsDocument> Documents { get; set; } = [];
    public List<PatientInsightsAppointment> Appointments { get; set; } = [];
    public List<int> AvailableYears { get; set; } = [];
    public int TotalVisits { get; set; }
    public int VisitsThisYear { get; set; }
    public int FollowUpCount { get; set; }
    public int WoundCareCount { get; set; }
}

public class PatientInsightsAppointment
{
    public int TreatmentRecordId { get; set; }
    public DateTime AppointmentDateTime { get; set; }
    public string DateText { get; set; } = string.Empty;
    public string TimeText { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
    public string Diagnosis { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string StatusClass { get; set; } = string.Empty;
    public string ReportUrl { get; set; } = string.Empty;
}

public class PatientInsightsDocument
{
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}

public class PatientInsightsSearchItem
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string CitizenId { get; set; } = string.Empty;
    public bool HasPhoto { get; set; }
}

public class PatientInsightsProfile
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string CitizenId { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public string Age { get; set; } = string.Empty;
    public string BirthDate { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string UnderlyingDisease { get; set; } = string.Empty;
    public string AllergyHistory { get; set; } = string.Empty;
    public string MedicalBenefit { get; set; } = string.Empty;
    public bool HasPhoto { get; set; }
}

public class PatientInsightsVisit
{
    public int Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public DateTime VisitDate { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
    public string Treatment { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string ReportUrl { get; set; } = string.Empty;
}

public class PatientInsightsVital
{
    public string Date { get; set; } = string.Empty;
    public int? Systolic { get; set; }
    public int? Diastolic { get; set; }
    public int? Pulse { get; set; }
    public decimal? Temperature { get; set; }
    public int? Respiratory { get; set; }
    public decimal? Weight { get; set; }
    public decimal? Height { get; set; }
    public decimal? Bmi { get; set; }
    public string Source { get; set; } = string.Empty;
}
