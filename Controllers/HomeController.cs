using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartClinic.Web.Data;
using SmartClinic.Web.Models;
using SmartClinic.Web.ViewModels;

namespace SmartClinic.Web.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext dbContext;
    private readonly UserManager<ApplicationUser> userManager;

    public HomeController(ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager)
    {
        this.dbContext = dbContext;
        this.userManager = userManager;
    }

    [Authorize]
    public async Task<IActionResult> Index()
    {
        var user = await userManager.GetUserAsync(User);
        var clinicCode = user?.ClinicCode ?? "SMARTCLINIC";
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var trendStart = today.AddDays(-6);

        var clinicInfo = await dbContext.Clinics
            .AsNoTracking()
            .Where(x => x.ClinicCode == clinicCode)
            .Select(x => new
            {
                x.ClinicName,
                x.LogoPath,
                x.OpdRecordLimit,
                x.HasUnlimitedOpdRecords
            })
            .FirstOrDefaultAsync();

        var patientQuery = dbContext.Patients.AsNoTracking().Where(x => x.ClinicCode == clinicCode);
        var totalPatients = await patientQuery.CountAsync();
        var genderCounts = await patientQuery
            .GroupBy(x => x.Gender)
            .Select(group => new { Gender = group.Key, Count = group.Count() })
            .ToListAsync();

        var visitQuery = dbContext.TreatmentRecords.AsNoTracking().Where(x => x.ClinicCode == clinicCode);
        var totalVisits = await visitQuery.CountAsync();
        var visitsToday = await visitQuery.CountAsync(x => x.VisitDate >= today && x.VisitDate < tomorrow);
        var visitsThisMonth = await visitQuery.CountAsync(x => x.VisitDate >= monthStart && x.VisitDate < tomorrow);
        var trendDates = await visitQuery
            .Where(x => x.VisitDate >= trendStart && x.VisitDate < tomorrow)
            .Select(x => x.VisitDate)
            .ToListAsync();

        var medicalProfiles = await dbContext.PatientMedicalProfiles
            .AsNoTracking()
            .CountAsync(x => x.ClinicCode == clinicCode);
        var signedPatients = await dbContext.SignImgs
            .AsNoTracking()
            .Where(x => x.ClinicCode == clinicCode)
            .Select(x => x.CitizenId)
            .Distinct()
            .CountAsync();

        var medicationLabelTemplates = await dbContext.MedicationLabelTemplates.AsNoTracking()
            .CountAsync(x => x.ClinicCode == clinicCode);
        var activeDrugs = await dbContext.ClinicDrugs.AsNoTracking()
            .CountAsync(x => x.ClinicCode == clinicCode && x.IsActive);
        var linkedIcdDiseases = await dbContext.IcdDrugProtocols.AsNoTracking()
            .Where(x => x.ClinicCode == clinicCode && x.IsActive)
            .Select(x => x.Icd10Code).Distinct().CountAsync();
        var activeDrugAdvice = await dbContext.DrugAdviceTemplates.AsNoTracking()
            .CountAsync(x => x.ClinicCode == clinicCode && x.IsActive);
        var woundCareRecords = await dbContext.WoundCareRecords.AsNoTracking()
            .CountAsync(x => x.ClinicCode == clinicCode);
        var announcementRows = await dbContext.PublicAnnouncements.AsNoTracking()
            .Where(x => x.IsPublished)
            .OrderBy(x => x.DisplayOrder).ThenByDescending(x => x.PublishedAtUtc)
            .Take(4)
            .ToListAsync();
        var announcements = announcementRows.Select(x => new DashboardAnnouncementViewModel
            {
                Id = x.Id, BadgeText = x.BadgeText, Title = x.Title, Summary = x.Summary,
                LinkUrl = x.LinkUrl,
                PublishedDateText = x.PublishedAtUtc.ToLocalTime().ToString("dd/MM/yyyy")
            }).ToList();

        var recentVisits = await visitQuery
            .OrderByDescending(x => x.VisitDate)
            .ThenByDescending(x => x.CreatedAtUtc)
            .Take(5)
            .Select(x => new DashboardRecentVisitViewModel
            {
                RecordId = x.Id,
                PatientName = x.Patient.FullName,
                CitizenId = x.Patient.CitizenId,
                Diagnosis = x.Diagnosis,
                VisitDateText = x.VisitDate.ToString("dd/MM/yyyy HH:mm")
            })
            .ToListAsync();

        var visitsByDate = trendDates
            .GroupBy(x => x.Date)
            .ToDictionary(group => group.Key, group => group.Count());
        var maxDailyVisits = Math.Max(1, visitsByDate.Values.DefaultIfEmpty(0).Max());
        var dailyVisits = Enumerable.Range(0, 7)
            .Select(offset =>
            {
                var date = trendStart.AddDays(offset);
                var count = visitsByDate.GetValueOrDefault(date);
                return new DashboardDailyVisitViewModel
                {
                    DayLabel = date.ToString("ddd", new System.Globalization.CultureInfo("th-TH")),
                    DateLabel = date.ToString("dd/MM"),
                    Count = count,
                    HeightPercent = count == 0 ? 5 : Math.Max(12, (int)Math.Round(count * 100d / maxDailyVisits))
                };
            })
            .ToList();

        int GenderCount(params string[] values) => genderCounts
            .Where(x => values.Contains(x.Gender, StringComparer.OrdinalIgnoreCase))
            .Sum(x => x.Count);

        var malePatients = GenderCount("ชาย", "Male");
        var femalePatients = GenderCount("หญิง", "Female");

        return View(new DashboardViewModel
        {
            ClinicName = string.IsNullOrWhiteSpace(clinicInfo?.ClinicName) ? "SmartClinic" : clinicInfo.ClinicName,
            ClinicCode = clinicCode,
            ClinicLogoUrl = clinicInfo?.LogoPath,
            TotalPatients = totalPatients,
            VisitsToday = visitsToday,
            VisitsThisMonth = visitsThisMonth,
            TotalVisits = totalVisits,
            MedicalProfiles = medicalProfiles,
            SignedPatients = signedPatients,
            MalePatients = malePatients,
            FemalePatients = femalePatients,
            OtherPatients = Math.Max(0, totalPatients - malePatients - femalePatients),
            ProfileCoveragePercent = totalPatients == 0 ? 0 : Math.Min(100, (int)Math.Round(medicalProfiles * 100d / totalPatients)),
            SignatureCoveragePercent = totalPatients == 0 ? 0 : Math.Min(100, (int)Math.Round(signedPatients * 100d / totalPatients)),
            MedicationLabelTemplates = medicationLabelTemplates,
            ActiveDrugs = activeDrugs,
            LinkedIcdDiseases = linkedIcdDiseases,
            ActiveDrugAdvice = activeDrugAdvice,
            WoundCareRecords = woundCareRecords,
            OpdRecordLimit = clinicInfo?.OpdRecordLimit ?? 30,
            HasUnlimitedOpdRecords = clinicInfo?.HasUnlimitedOpdRecords ?? false,
            Announcements = announcements,
            DailyVisits = dailyVisits,
            RecentVisits = recentVisits
        });
    }

    [Authorize]
    public async Task<IActionResult> Privacy()
    {
        var user = await userManager.GetUserAsync(User);
        var clinicCode = user?.ClinicCode ?? user?.UserName ?? "SMARTCLINIC";
        var clinic = await dbContext.Clinics
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ClinicCode == clinicCode);

        return View(new PrivacyNoticeViewModel
        {
            ClinicName = string.IsNullOrWhiteSpace(clinic?.ClinicName) ? "SmartClinic" : clinic.ClinicName,
            ClinicCode = clinicCode,
            Address = clinic?.Address ?? string.Empty,
            PhoneNumber = clinic?.PhoneNumber ?? string.Empty,
            Email = clinic?.Email ?? string.Empty,
            LogoUrl = clinic?.LogoPath,
            LastUpdatedText = DateTime.Today.ToString(
                "d MMMM yyyy",
                new System.Globalization.CultureInfo("th-TH"))
        });
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
