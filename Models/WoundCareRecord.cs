namespace SmartClinic.Web.Models;

public class WoundCareRecord
{
    public int Id { get; set; }
    public string ClinicCode { get; set; } = string.Empty;
    public int PatientId { get; set; }
    public Patient Patient { get; set; } = null!;
    public string CitizenId { get; set; } = string.Empty;
    public string ServiceRecipientId { get; set; } = string.Empty;
    public DateTime VisitDate { get; set; } = DateTime.Now;
    public string AuthenticationCode { get; set; } = string.Empty;
    public string WoundCause { get; set; } = string.Empty;
    public string WoundLocation { get; set; } = string.Empty;
    public bool IsBruiseOrSutured { get; set; }
    public bool IsOpenInfected { get; set; }
    public bool IsLargeOpenWound { get; set; }
    public string WoundSize { get; set; } = string.Empty;
    public int? DressingSequence { get; set; }
    public string WoundProgress { get; set; } = string.Empty;
    public string WoundProgressNote { get; set; } = string.Empty;
    public decimal? TemperatureCelsius { get; set; }
    public int? PulseRate { get; set; }
    public int? RespiratoryRate { get; set; }
    public int? SystolicPressure { get; set; }
    public int? DiastolicPressure { get; set; }
    public string PostDressingResult { get; set; } = string.Empty;
    public bool FollowUpNone { get; set; }
    public bool FollowUpPhone { get; set; }
    public bool FollowUpClinic { get; set; }
    public string FollowUpNote { get; set; } = string.Empty;
    public bool FollowUpOther { get; set; }
    public string WoundAssessment { get; set; } = string.Empty;
    public string PatientAdvice { get; set; } = string.Empty;
    public string ProviderUserId { get; set; } = string.Empty;
    public string ProviderName { get; set; } = string.Empty;
    public string ProviderTitle { get; set; } = string.Empty;
    public string? OriginalFileName { get; set; }
    public string? OriginalContentType { get; set; }
    public byte[]? OriginalDocumentData { get; set; }
    public string OcrRawText { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
    public ICollection<WoundCarePhoto> Photos { get; set; } = new List<WoundCarePhoto>();
}

public class WoundCarePhoto
{
    public int Id { get; set; }
    public int WoundCareRecordId { get; set; }
    public WoundCareRecord WoundCareRecord { get; set; } = null!;
    public byte SequenceNo { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public byte[] ImageData { get; set; } = Array.Empty<byte>();
    public string Caption { get; set; } = string.Empty;
    public DateTime UploadedAtUtc { get; set; } = DateTime.UtcNow;
}
