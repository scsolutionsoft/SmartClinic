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

    public string Email { get; set; } = string.Empty;

    public string RegisteredBy { get; set; } = string.Empty;

    public string Theme { get; set; } = "lux";

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}