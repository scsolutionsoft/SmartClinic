using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartClinic.Web.Data;
using SmartClinic.Web.Models;
using SmartClinic.Web.ViewModels;

namespace SmartClinic.Web.Controllers;

[Authorize(Roles = "SuperAdmin,AdminClinic,Nurse")]
public class PatientInsightsController(
    ApplicationDbContext db,
    UserManager<ApplicationUser> userManager) : Controller
{
    public async Task<IActionResult> Index(string? search, int? patientId, int? month, int? year)
    {
        var user = await userManager.GetUserAsync(User);
        var clinic = user?.ClinicCode ?? "SMARTCLINIC";
        var term = search?.Trim() ?? string.Empty;
        var model = new PatientInsightsViewModel { Search = term, PatientId = patientId, Month = month, Year = year };

        var patientQuery = db.Patients.AsNoTracking().Where(x => x.ClinicCode == clinic);
        if (!string.IsNullOrWhiteSpace(term))
            patientQuery = patientQuery.Where(x => x.FullName.Contains(term) || x.CitizenId.Contains(term) || x.PhoneNumber.Contains(term));

        model.Patients = await patientQuery.OrderBy(x => x.FullName).Take(12)
            .Select(x => new PatientInsightsSearchItem { Id = x.Id, FullName = x.FullName, CitizenId = x.CitizenId, HasPhoto = x.PhotoData != null })
            .ToListAsync();

        if (!patientId.HasValue && model.Patients.Count == 1) patientId = model.PatientId = model.Patients[0].Id;
        if (!patientId.HasValue) return View(model);

        var patient = await db.Patients.AsNoTracking().FirstOrDefaultAsync(x => x.Id == patientId && x.ClinicCode == clinic);
        if (patient is null) return View(model);
        var profile = await db.PatientMedicalProfiles.AsNoTracking().FirstOrDefaultAsync(x => x.PatientId == patient.Id && x.ClinicCode == clinic);
        var age = patient.BirthDate.HasValue ? Math.Max(0, DateTime.Today.Year - patient.BirthDate.Value.Year - (DateOnly.FromDateTime(DateTime.Today) < patient.BirthDate.Value.AddYears(DateTime.Today.Year - patient.BirthDate.Value.Year) ? 1 : 0)) : (int?)null;

        model.Profile = new PatientInsightsProfile {
            Id = patient.Id, FullName = patient.FullName, CitizenId = patient.CitizenId, Gender = patient.Gender,
            Age = age.HasValue ? $"{age} ปี" : "ไม่ระบุ", BirthDate = patient.BirthDate?.ToString("dd/MM/yyyy") ?? "ไม่ระบุ",
            PhoneNumber = patient.PhoneNumber, Address = patient.Address, HasPhoto = patient.PhotoData?.Length > 0,
            UnderlyingDisease = profile?.UnderlyingDisease ?? string.Empty, AllergyHistory = profile?.AllergyHistory ?? string.Empty,
            MedicalBenefit = profile?.MedicalBenefit ?? string.Empty
        };

        var opd = await db.TreatmentRecords.AsNoTracking().Where(x => x.PatientId == patient.Id && x.ClinicCode == clinic).ToListAsync();
        var wounds = await db.WoundCareRecords.AsNoTracking().Where(x => x.PatientId == patient.Id && x.ClinicCode == clinic).ToListAsync();
        model.TotalVisits = opd.Count + wounds.Count;
        model.VisitsThisYear = opd.Count(x => x.VisitDate.Year == DateTime.Today.Year) + wounds.Count(x => x.VisitDate.Year == DateTime.Today.Year);
        model.FollowUpCount = opd.Count(x => x.FollowUpClinic || x.FollowUpPhone || x.FollowUpOther) + wounds.Count(x => x.FollowUpClinic || x.FollowUpPhone || x.FollowUpOther);
        var now = DateTime.Now;
        model.Appointments = opd.Where(x => x.FollowUpClinic && x.FollowUpAppointmentDateTime.HasValue)
            .OrderBy(x => x.FollowUpAppointmentDateTime)
            .Select(x => {
                var appointment = x.FollowUpAppointmentDateTime!.Value;
                var isToday = appointment.Date == now.Date;
                return new PatientInsightsAppointment {
                    TreatmentRecordId=x.Id, AppointmentDateTime=appointment,
                    DateText=appointment.ToString("dd MMMM yyyy", new System.Globalization.CultureInfo("th-TH")), TimeText=appointment.ToString("HH:mm") + " น.",
                    Note=TextOr(x.FollowUpClinicNote, "นัดติดตามอาการที่คลินิก"), Diagnosis=TextOr(x.Diagnosis, "ติดตามการรักษา"),
                    Status=isToday ? "นัดวันนี้" : appointment > now ? "กำลังจะถึง" : "ผ่านวันนัดแล้ว",
                    StatusClass=isToday ? "today" : appointment > now ? "upcoming" : "past",
                    ReportUrl=Url.Action("Report", "MedicalRecords", new { id=x.Id }) ?? ""
                };
            }).ToList();
        model.WoundCareCount = wounds.Count;
        model.AvailableYears = opd.Select(x => x.VisitDate.Year).Concat(wounds.Select(x => x.VisitDate.Year)).Distinct().OrderByDescending(x => x).ToList();

        IEnumerable<PatientInsightsVisit> visits = opd.Select(x => new PatientInsightsVisit {
            Id=x.Id, Type="OPD", VisitDate=x.VisitDate, Date=x.VisitDate.ToString("dd MMM yyyy · HH:mm"),
            Title=TextOr(x.Diagnosis, "รับบริการตรวจรักษา"), Detail=TextOr(x.ChiefComplaint, "ไม่มีรายละเอียดอาการ"),
            Treatment=TextOr(x.TreatmentAndAdvice, "ไม่ระบุ"), Provider=TextOr(x.ProviderName, "ไม่ระบุผู้ให้บริการ"),
            Status=x.FollowUpClinic || x.FollowUpPhone || x.FollowUpOther ? "มีนัดติดตาม" : "เสร็จสิ้น",
            ReportUrl=Url.Action("Report", "MedicalRecords", new { id=x.Id }) ?? ""
        }).Concat(wounds.Select(x => new PatientInsightsVisit {
            Id=x.Id, Type="WOUND", VisitDate=x.VisitDate, Date=x.VisitDate.ToString("dd MMM yyyy · HH:mm"),
            Title="บริการทำแผล", Detail=TextOr(x.WoundCause, "ประเมินและดูแลบาดแผล"),
            Treatment=TextOr(x.PostDressingResult, x.PatientAdvice, "ทำแผลเรียบร้อย"), Provider=TextOr(x.ProviderName, "ไม่ระบุผู้ให้บริการ"),
            Status=x.FollowUpClinic || x.FollowUpPhone || x.FollowUpOther ? "มีนัดติดตาม" : "เสร็จสิ้น",
            ReportUrl=Url.Action("Report", "WoundCare", new { id=x.Id }) ?? ""
        }));
        if (year.HasValue) visits = visits.Where(x => x.VisitDate.Year == year);
        if (month.HasValue) visits = visits.Where(x => x.VisitDate.Month == month);
        model.Visits = visits.OrderByDescending(x => x.VisitDate).ToList();
        var woundVitals = wounds.Where(x => x.SystolicPressure.HasValue || x.PulseRate.HasValue || x.TemperatureCelsius.HasValue)
            .Select(x => new { x.VisitDate, Vital = new PatientInsightsVital {
                Date=x.VisitDate.ToString("dd/MM"), Systolic=x.SystolicPressure, Diastolic=x.DiastolicPressure,
                Pulse=x.PulseRate, Temperature=x.TemperatureCelsius, Respiratory=x.RespiratoryRate, Source="ทำแผล"
            }});
        var opdVitals = opd.Where(x => x.SystolicPressure.HasValue || x.PulseRate.HasValue || x.TemperatureCelsius.HasValue || x.WeightKilograms.HasValue)
            .Select(x => new { x.VisitDate, Vital = new PatientInsightsVital {
                Date=x.VisitDate.ToString("dd/MM"), Systolic=x.SystolicPressure, Diastolic=x.DiastolicPressure,
                Pulse=x.PulseRate, Temperature=x.TemperatureCelsius, Respiratory=x.RespiratoryRate,
                Weight=x.WeightKilograms, Height=x.HeightCentimeters, Bmi=x.BodyMassIndex, Source="OPD"
            }});
        model.Vitals = woundVitals.Concat(opdVitals).OrderBy(x => x.VisitDate).TakeLast(12).Select(x => x.Vital).ToList();
        model.Documents = opd.OrderByDescending(x => x.VisitDate).Select(x => new PatientInsightsDocument {
            Type = "OPD", Title = "เวชระเบียน OPD", Subtitle = TextOr(x.Diagnosis, "รายงานการตรวจรักษา"),
            Date = x.VisitDate.ToString("dd/MM/yyyy HH:mm"), Url = Url.Action("Report", "MedicalRecords", new { id = x.Id }) ?? ""
        }).ToList();
        if (profile is not null)
        {
            model.Documents.Insert(0, new PatientInsightsDocument {
                Type = "PROFILE", Title = "เวชระเบียนผู้รับบริการ", Subtitle = TextOr(profile.MedicalBenefit, "ข้อมูลและประวัติผู้รับบริการ"),
                Date = (profile.InformationGivenDate?.ToString("dd/MM/yyyy") ?? profile.CreatedAtUtc.ToLocalTime().ToString("dd/MM/yyyy")),
                Url = Url.Action("Report", "PatientMedicalProfiles", new { id = profile.Id }) ?? ""
            });
        }
        return View(model);
    }

    private static string TextOr(params string?[] values) => values.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x))?.Trim() ?? "";
}
