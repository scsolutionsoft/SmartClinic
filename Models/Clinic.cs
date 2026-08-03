namespace SmartClinic.Web.Models;

public class Clinic
{
    public int Id { get; set; }

    public string ClinicCode { get; set; } = string.Empty;

    public string ClinicName { get; set; } = string.Empty;

    public string? LogoPath { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public string OpeningHours { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string RegisteredBy { get; set; } = string.Empty;

    public string Theme { get; set; } = "lux";

    public int OpdRecordLimit { get; set; } = 30;

    public bool HasUnlimitedOpdRecords { get; set; }

    public string Status { get; set; } = "Active";

    public DateTime? LastReviewedAtUtc { get; set; }

    public string? LastReviewedByUserId { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
