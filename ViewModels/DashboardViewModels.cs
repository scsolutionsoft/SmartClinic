namespace SmartClinic.Web.ViewModels;

public class DashboardViewModel
{
    public string ClinicName { get; set; } = "SmartClinic";

    public string ClinicCode { get; set; } = string.Empty;

    public string? ClinicLogoUrl { get; set; }

    public int TotalPatients { get; set; }

    public int VisitsToday { get; set; }

    public int VisitsThisMonth { get; set; }

    public int TotalVisits { get; set; }

    public int MedicalProfiles { get; set; }

    public int SignedPatients { get; set; }

    public int MalePatients { get; set; }

    public int FemalePatients { get; set; }

    public int OtherPatients { get; set; }

    public int ProfileCoveragePercent { get; set; }

    public int SignatureCoveragePercent { get; set; }

    public int MedicationLabelTemplates { get; set; }

    public int ActiveDrugs { get; set; }

    public int LinkedIcdDiseases { get; set; }

    public int ActiveDrugAdvice { get; set; }

    public int WoundCareRecords { get; set; }

    public int OpdRecordLimit { get; set; }

    public bool HasUnlimitedOpdRecords { get; set; }

    public int RemainingOpdRecords => HasUnlimitedOpdRecords ? int.MaxValue : Math.Max(0, OpdRecordLimit - TotalVisits);

    public int OpdQuotaPercent => HasUnlimitedOpdRecords || OpdRecordLimit <= 0 ? 0 : Math.Min(100, (int)Math.Round(TotalVisits * 100d / OpdRecordLimit));

    public IReadOnlyList<DashboardAnnouncementViewModel> Announcements { get; set; } = [];

    public IReadOnlyList<DashboardDailyVisitViewModel> DailyVisits { get; set; } = Array.Empty<DashboardDailyVisitViewModel>();

    public IReadOnlyList<DashboardRecentVisitViewModel> RecentVisits { get; set; } = Array.Empty<DashboardRecentVisitViewModel>();
}

public class DashboardAnnouncementViewModel
{
    public int Id { get; set; }
    public string BadgeText { get; set; } = "ประกาศ";
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string LinkUrl { get; set; } = string.Empty;
    public string PublishedDateText { get; set; } = string.Empty;
}

public class DashboardDailyVisitViewModel
{
    public string DayLabel { get; set; } = string.Empty;

    public string DateLabel { get; set; } = string.Empty;

    public int Count { get; set; }

    public int HeightPercent { get; set; }
}

public class DashboardRecentVisitViewModel
{
    public int RecordId { get; set; }

    public string PatientName { get; set; } = string.Empty;

    public string CitizenId { get; set; } = string.Empty;

    public string Diagnosis { get; set; } = string.Empty;

    public string VisitDateText { get; set; } = string.Empty;
}
