using Microsoft.AspNetCore.Identity;

namespace SmartClinic.Web.Models;

public class ApplicationUser : IdentityUser
{
    public string? FullName { get; set; }

    public string? ClinicCode { get; set; }

    public string? ProfessionalTitle { get; set; }

    public string? LicenseNo { get; set; }

    public string? ProviderSignatureFileName { get; set; }

    public string? ProviderSignatureContentType { get; set; }

    public byte[]? ProviderSignatureImageData { get; set; }

    public DateTime? ProviderSignatureUploadedAtUtc { get; set; }

    public bool MustChangePassword { get; set; }
}
