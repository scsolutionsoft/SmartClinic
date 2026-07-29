using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartClinic.Web.Data;
using SmartClinic.Web.Models;
using SmartClinic.Web.ViewModels;

namespace SmartClinic.Web.Controllers;

[Authorize(Roles = "SuperAdmin,AdminClinic,Nurse")]
public class ReportsController : Controller
{
    private readonly ApplicationDbContext dbContext;
    private readonly UserManager<ApplicationUser> userManager;

    public ReportsController(ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager)
    {
        this.dbContext = dbContext;
        this.userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index(DateTime? dateFrom = null, DateTime? dateTo = null)
    {
        var clinicCode = await GetClinicCode();
        var fromDate = (dateFrom ?? DateTime.Today.AddDays(-30)).Date;
        var toDate = (dateTo ?? DateTime.Today).Date.AddDays(1).AddTicks(-1);

        var records = await dbContext.TreatmentRecords
            .AsNoTracking()
            .Include(x => x.Patient)
            .Where(x => x.ClinicCode == clinicCode && x.VisitDate >= fromDate && x.VisitDate <= toDate)
            .OrderByDescending(x => x.VisitDate)
            .Take(300)
            .Select(x => new
            {
                x.Id,
                x.Diagnosis,
                x.VisitDate,
                x.PatientId,
                PatientName = x.Patient.FullName,
                CitizenId = x.Patient.CitizenId
            })
            .ToListAsync();

        var citizenIds = records.Select(x => x.CitizenId).Distinct().ToList();
        var signedCitizenIds = await dbContext.SignImgs
            .AsNoTracking()
            .Where(x => x.ClinicCode == clinicCode && citizenIds.Contains(x.CitizenId))
            .Select(x => x.CitizenId)
            .Distinct()
            .ToListAsync();

        var signedSet = signedCitizenIds.ToHashSet(StringComparer.Ordinal);

        var items = records
            .Select(x => new ReportItemViewModel
            {
                RecordId = x.Id,
                PatientName = x.PatientName,
                CitizenId = x.CitizenId,
                Diagnosis = x.Diagnosis,
                VisitDateText = x.VisitDate.ToString("dd/MM/yyyy HH:mm"),
                HasSignature = signedSet.Contains(x.CitizenId)
            })
            .ToList();

        var model = new ReportsDashboardViewModel
        {
            Filter = new ReportFilterViewModel
            {
                DateFrom = fromDate,
                DateTo = toDate.Date
            },
            Items = items,
            TotalVisits = items.Count,
            SignedVisits = items.Count(x => x.HasSignature),
            UniquePatients = items.Select(x => x.CitizenId).Distinct().Count()
        };

        return View(model);
    }

    private async Task<string> GetClinicCode()
    {
        var user = await userManager.GetUserAsync(User);
        return user?.ClinicCode ?? "SMARTCLINIC";
    }
}