namespace SmartClinic.Web.Models;

public class Patient
{
    public int Id { get; set; }

    public string ClinicCode { get; set; } = string.Empty;

    public string CitizenId { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty; // stored as empty string if not provided

    public DateOnly? BirthDate { get; set; }

    public string Gender { get; set; } = string.Empty;

    public string? PhotoPath { get; set; }

    public byte[]? PhotoData { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}