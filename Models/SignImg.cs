namespace SmartClinic.Web.Models;

public class SignImg
{
    public int Id { get; set; }

    public string ClinicCode { get; set; } = string.Empty;

    public string CitizenId { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = "image/png";

    public byte[] ImageData { get; set; } = Array.Empty<byte>();

    public DateTime UploadedAtUtc { get; set; } = DateTime.UtcNow;
}