namespace SmartClinic.Web.ViewModels;

public class PrivacyNoticeViewModel
{
    public string ClinicName { get; set; } = "SmartClinic";

    public string ClinicCode { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? LogoUrl { get; set; }

    public string LastUpdatedText { get; set; } = string.Empty;
}
