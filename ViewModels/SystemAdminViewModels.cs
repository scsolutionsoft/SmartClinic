using System.ComponentModel.DataAnnotations;
using SmartClinic.Web.Models;

namespace SmartClinic.Web.ViewModels;

public class SystemAdminDashboardViewModel
{
    public int TotalClinics { get; set; }
    public int ActiveClinics { get; set; }
    public int UnlimitedClinics { get; set; }
    public int TotalPatients { get; set; }
    public int PendingSlips { get; set; }
    public decimal PendingAmount { get; set; }
    public int NewMessages { get; set; }
    public IReadOnlyList<SystemClinicItemViewModel> Clinics { get; set; } = [];
    public IReadOnlyList<PaymentSlip> RecentSlips { get; set; } = [];
    public IReadOnlyList<AdminMessage> RecentMessages { get; set; } = [];
    public IReadOnlyList<AdminAuditLog> RecentActivity { get; set; } = [];
}

public class SystemClinicItemViewModel
{
    public int Id { get; set; }
    public string ClinicCode { get; set; } = string.Empty;
    public string ClinicName { get; set; } = string.Empty;
    public string ContactName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int UsedPatients { get; set; }
    public int PatientLimit { get; set; }
    public bool IsUnlimited { get; set; }
    public int Remaining => IsUnlimited ? int.MaxValue : Math.Max(0, PatientLimit - UsedPatients);
    public DateTime CreatedAtUtc { get; set; }
}

public class SystemClinicsViewModel
{
    public string Search { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public IReadOnlyList<SystemClinicItemViewModel> Clinics { get; set; } = [];
}

public class PaymentSlipInputViewModel
{
    [Required] public string ContactName { get; set; } = string.Empty;
    [Required, Phone] public string PhoneNumber { get; set; } = string.Empty;
    [Range(typeof(decimal), "0.01", "999999999")] public decimal Amount { get; set; }
    [Range(0, 1000000)] public int RequestedCredits { get; set; }
    public bool RequestUnlimited { get; set; }
    [Required] public DateTime TransferDateTime { get; set; } = DateTime.Now;
    [Required, StringLength(100)] public string TransferBank { get; set; } = string.Empty;
    [StringLength(100)] public string? TransferReference { get; set; }
    [StringLength(1000)] public string? Note { get; set; }
    [Required] public IFormFile? SlipFile { get; set; }
}

public class AdminMessageInputViewModel
{
    [Required, StringLength(200)] public string Subject { get; set; } = string.Empty;
    [Required, StringLength(4000)] public string Message { get; set; } = string.Empty;
    [Required] public string ContactName { get; set; } = string.Empty;
    [Required, Phone] public string PhoneNumber { get; set; } = string.Empty;
}

public class SlipReportViewModel
{
    public string Status { get; set; } = string.Empty;
    public string Search { get; set; } = string.Empty;
    public IReadOnlyList<PaymentSlipReportItemViewModel> Items { get; set; } = [];
    public decimal TotalAmount { get; set; }
}

public class PaymentSlipReportItemViewModel
{
    public PaymentSlip Slip { get; set; } = null!;
    public string ClinicName { get; set; } = string.Empty;
    public int UsedPatients { get; set; }
    public int PatientLimit { get; set; }
    public bool IsUnlimited { get; set; }
    public int Remaining => IsUnlimited ? int.MaxValue : Math.Max(0, PatientLimit - UsedPatients);
}
