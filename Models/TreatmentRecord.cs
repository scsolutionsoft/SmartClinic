namespace SmartClinic.Web.Models;

public class TreatmentRecord
{
    public int Id { get; set; }

    public string ClinicCode { get; set; } = string.Empty;

    public int PatientId { get; set; }

    public Patient Patient { get; set; } = null!;

    public DateTime VisitDate { get; set; }

    public string ServiceRecipientId { get; set; } = string.Empty;

    public string AuthenticationCode { get; set; } = string.Empty;

    public string CitizenId { get; set; } = string.Empty;

    public string Diagnosis { get; set; } = string.Empty;

    public string InitialDifferentialDiagnosis { get; set; } = string.Empty;

    public string ChiefComplaint { get; set; } = string.Empty;

    public string PresentIllness { get; set; } = string.Empty;

    public string PhysicalExam { get; set; } = string.Empty;

    public string ProblemPhysicalExam { get; set; } = string.Empty;

    public string TreatmentAndAdvice { get; set; } = string.Empty;

    public string ReferralDetail { get; set; } = string.Empty;

    public bool FollowUpNone { get; set; }

    public bool FollowUpPhone { get; set; }

    public bool FollowUpClinic { get; set; }

    public string? FollowUpClinicNote { get; set; }

    public bool FollowUpOther { get; set; }

    public string? FollowUpOtherNote { get; set; }

    public DateTime? ServiceEndDateTime { get; set; }

    public bool IsChildCase { get; set; }

    public string? ChildGrowthStatus { get; set; }

    public string? ChildDevelopmentStatus { get; set; }

    public string? ChildVaccineStatus { get; set; }

    public string? ChildVaccineNote { get; set; }

    public string? Note { get; set; }

    public string OpdFileName { get; set; } = string.Empty;

    public string OpdContentType { get; set; } = "application/pdf";

    public byte[] OpdPdfData { get; set; } = Array.Empty<byte>();

    public string? ProviderUserId { get; set; }

    public string ProviderName { get; set; } = string.Empty;

    public string ProviderProfessionalTitle { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
