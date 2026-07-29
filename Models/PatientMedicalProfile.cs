namespace SmartClinic.Web.Models;

public class PatientMedicalProfile
{
    public int Id { get; set; }

    public string ClinicCode { get; set; } = string.Empty;

    public int PatientId { get; set; }

    public Patient Patient { get; set; } = null!;

    public string CitizenId { get; set; } = string.Empty;

    public string ServiceRecipientId { get; set; } = string.Empty;

    public string ClinicName { get; set; } = string.Empty;

    public string ClinicAddress { get; set; } = string.Empty;

    public string PatientName { get; set; } = string.Empty;

    public string Gender { get; set; } = string.Empty;

    public DateOnly? BirthDate { get; set; }

    public string Race { get; set; } = string.Empty;

    public string Nationality { get; set; } = string.Empty;

    public string MaritalStatus { get; set; } = string.Empty;

    public string RegisteredAddress { get; set; } = string.Empty;

    public string ContactAddress { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    public string PrimaryHospital { get; set; } = string.Empty;

    public string UnderlyingDisease { get; set; } = string.Empty;

    public string PastHistory { get; set; } = string.Empty;

    public string FamilyHistory { get; set; } = string.Empty;

    public string AllergyHistory { get; set; } = string.Empty;

    public string MedicalBenefit { get; set; } = string.Empty;

    public string EmergencyContactName { get; set; } = string.Empty;

    public string EmergencyContactPhone { get; set; } = string.Empty;

    public DateOnly? InformationGivenDate { get; set; }

    public string? SourcePdfFileName { get; set; }

    public string? SourcePdfContentType { get; set; }

    public byte[]? SourcePdfData { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }
}
