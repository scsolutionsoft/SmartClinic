namespace SmartClinic.Web.Models;

public class NhssoClinicMaster
{
    public int Id { get; set; }

    public string ClinicCode { get; set; } = string.Empty;

    public string ClinicName { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public string? ContactPhone { get; set; }

    public string? ContactEmail { get; set; }

    public bool IsActive { get; set; } = true;
}