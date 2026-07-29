namespace SmartClinic.Web.ViewModels;

public class ReportFilterViewModel
{
    public DateTime DateFrom { get; set; } = DateTime.Today.AddDays(-30);

    public DateTime DateTo { get; set; } = DateTime.Today;
}

public class ReportItemViewModel
{
    public int RecordId { get; set; }

    public string PatientName { get; set; } = string.Empty;

    public string CitizenId { get; set; } = string.Empty;

    public string Diagnosis { get; set; } = string.Empty;

    public string VisitDateText { get; set; } = string.Empty;

    public bool HasSignature { get; set; }
}

public class ReportsDashboardViewModel
{
    public ReportFilterViewModel Filter { get; set; } = new();

    public IReadOnlyList<ReportItemViewModel> Items { get; set; } = Array.Empty<ReportItemViewModel>();

    public int TotalVisits { get; set; }

    public int SignedVisits { get; set; }

    public int UniquePatients { get; set; }
}