using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.RegularExpressions;
using SmartClinic.Web.Data;
using SmartClinic.Web.Models;
using SmartClinic.Web.ViewModels;

namespace SmartClinic.Web.Controllers;

[Authorize(Roles = "SuperAdmin,AdminClinic,Nurse")]
public class MedicationLabelsController(ApplicationDbContext db, UserManager<ApplicationUser> users) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(int? templateId = null)
    {
        var clinicCode = await GetClinicCode();
        var input = new MedicationLabelInputViewModel();
        if (templateId.HasValue)
        {
            var template = await db.MedicationLabelTemplates.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == templateId && x.ClinicCode == clinicCode);
            if (template is not null) input = Map(template);
        }
        return View(await BuildDashboard(clinicCode, input));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveTemplate(MedicationLabelInputViewModel input)
    {
        var clinicCode = await GetClinicCode();
        if (string.IsNullOrWhiteSpace(input.DiseaseCategory) || string.IsNullOrWhiteSpace(input.TemplateName))
        {
            ModelState.AddModelError(string.Empty, "กรุณาระบุประเภทโรคและชื่อแม่แบบ");
            return View("Index", await BuildDashboard(clinicCode, input));
        }

        var entity = input.TemplateId.HasValue
            ? await db.MedicationLabelTemplates.FirstOrDefaultAsync(x => x.Id == input.TemplateId && x.ClinicCode == clinicCode)
            : null;
        if (entity is null)
        {
            entity = new MedicationLabelTemplate { ClinicCode = clinicCode };
            db.MedicationLabelTemplates.Add(entity);
        }
        Apply(input, entity);
        entity.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();
        TempData["LabelStatus"] = "saved";
        return RedirectToAction(nameof(Index), new { templateId = entity.Id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteTemplate(int id)
    {
        var clinicCode = await GetClinicCode();
        var entity = await db.MedicationLabelTemplates.FirstOrDefaultAsync(x => x.Id == id && x.ClinicCode == clinicCode);
        if (entity is not null) { db.Remove(entity); await db.SaveChangesAsync(); }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Print(MedicationLabelInputViewModel input)
    {
        var clinicCode = await GetClinicCode();
        var assignedSlots = new List<MedicationLabelSlotViewModel>();
        if (string.Equals(input.PrintMode, "opd", StringComparison.OrdinalIgnoreCase))
        {
            var preparation = await PrepareOpdSlots(clinicCode, input);
            if (!preparation.Success)
            {
                ModelState.AddModelError(string.Empty, preparation.Error);
                return View("Index", await BuildDashboard(clinicCode, input));
            }

            assignedSlots = preparation.Slots;
            input.PatientId = input.OpdPatientId;
            input.PatientName = preparation.PatientName;
            input.BlankMode = false;
        }
        input.StartPosition = Math.Clamp(input.StartPosition, 1, 8);
        input.Quantity = Math.Clamp(input.Quantity, 1, 9 - input.StartPosition);
        if (input.PatientId.HasValue)
        {
            input.PatientName = await db.Patients.AsNoTracking()
                .Where(x => x.Id == input.PatientId && x.ClinicCode == clinicCode)
                .Select(x => x.FullName).FirstOrDefaultAsync() ?? input.PatientName;
        }
        var clinic = await db.Clinics.AsNoTracking().FirstOrDefaultAsync(x => x.ClinicCode == clinicCode);
        if (clinic is null) return NotFound();
        return View("Print", new MedicationLabelPrintViewModel
        {
            TemplateId = input.TemplateId, DiseaseCategory = input.DiseaseCategory, TemplateName = input.TemplateName,
            MedicineName = input.MedicineName, DoseAmount = input.DoseAmount, FrequencyPerDay = input.FrequencyPerDay,
            MealTiming = input.MealTiming, Morning = input.Morning, Noon = input.Noon, Evening = input.Evening,
            Bedtime = input.Bedtime, IntervalHours = input.IntervalHours, FinishCourse = input.FinishCourse,
            MayCauseDrowsiness = input.MayCauseDrowsiness, BeforeMeal30Minutes = input.BeforeMeal30Minutes,
            ImmediatelyAfterMeal = input.ImmediatelyAfterMeal, AdditionalAdvice = input.AdditionalAdvice,
            PatientId = input.PatientId, PatientName = input.PatientName, LabelDate = input.LabelDate,
            BlankMode = input.BlankMode, StartPosition = input.StartPosition, Quantity = input.Quantity,
            OffsetXMillimeters = input.OffsetXMillimeters, OffsetYMillimeters = input.OffsetYMillimeters,
            ClinicName = clinic.ClinicName, ClinicAddress = clinic.Address, ClinicPhone = clinic.PhoneNumber,
            OpeningHours = clinic.OpeningHours, AssignedSlots = assignedSlots
        });
    }

    [HttpGet]
    public async Task<IActionResult> OpdVisits(int patientId)
    {
        var clinicCode = await GetClinicCode();
        var patientExists = await db.Patients.AsNoTracking().AnyAsync(x => x.Id == patientId && x.ClinicCode == clinicCode);
        if (!patientExists) return NotFound();

        var visits = await db.TreatmentRecords.AsNoTracking()
            .Where(x => x.PatientId == patientId && x.ClinicCode == clinicCode)
            .OrderByDescending(x => x.VisitDate)
            .Select(x => new { id = x.Id, date = x.VisitDate.ToString("dd/MM/yyyy HH:mm"), diagnosis = x.Diagnosis })
            .Take(100)
            .ToListAsync();
        return Json(visits);
    }

    [HttpGet]
    public async Task<IActionResult> OpdMedicines(int treatmentRecordId)
    {
        var clinicCode = await GetClinicCode();
        var record = await db.TreatmentRecords.AsNoTracking()
            .Where(x => x.Id == treatmentRecordId && x.ClinicCode == clinicCode)
            .Select(x => new { x.PatientId, x.TreatmentAndAdvice })
            .FirstOrDefaultAsync();
        if (record is null) return NotFound();

        var medicines = ParseMedicines(record.TreatmentAndAdvice);
        return Json(new
        {
            patientId = record.PatientId,
            medicines = medicines.Select((x, index) => new { index, name = x.MedicineName, usage = BuildUsageSummary(x), advice = x.AdditionalAdvice })
        });
    }

    private async Task<MedicationLabelsDashboardViewModel> BuildDashboard(string clinicCode, MedicationLabelInputViewModel input)
    {
        var clinic = await db.Clinics.AsNoTracking().FirstOrDefaultAsync(x => x.ClinicCode == clinicCode);
        return new MedicationLabelsDashboardViewModel
        {
            Input = input,
            Templates = await db.MedicationLabelTemplates.AsNoTracking().Where(x => x.ClinicCode == clinicCode)
                .OrderBy(x => x.DiseaseCategory).ThenBy(x => x.TemplateName).ToListAsync(),
            Patients = await db.Patients.AsNoTracking().Where(x => x.ClinicCode == clinicCode)
                .OrderBy(x => x.FullName).Take(500).ToListAsync(),
            ClinicName = clinic?.ClinicName ?? clinicCode, ClinicAddress = clinic?.Address ?? string.Empty,
            ClinicPhone = clinic?.PhoneNumber ?? string.Empty, OpeningHours = clinic?.OpeningHours ?? string.Empty
        };
    }

    private async Task<string> GetClinicCode()
    {
        var user = await users.GetUserAsync(User);
        return user?.ClinicCode ?? "SMARTCLINIC";
    }

    private static MedicationLabelInputViewModel Map(MedicationLabelTemplate x) => new()
    {
        TemplateId=x.Id,DiseaseCategory=x.DiseaseCategory,TemplateName=x.TemplateName,MedicineName=x.MedicineName,
        DoseAmount=x.DoseAmount,FrequencyPerDay=x.FrequencyPerDay,MealTiming=x.MealTiming,Morning=x.Morning,Noon=x.Noon,
        Evening=x.Evening,Bedtime=x.Bedtime,IntervalHours=x.IntervalHours,FinishCourse=x.FinishCourse,
        MayCauseDrowsiness=x.MayCauseDrowsiness,BeforeMeal30Minutes=x.BeforeMeal30Minutes,
        ImmediatelyAfterMeal=x.ImmediatelyAfterMeal,AdditionalAdvice=x.AdditionalAdvice
    };

    private static void Apply(MedicationLabelInputViewModel x, MedicationLabelTemplate e)
    {
        e.DiseaseCategory = Clean(x.DiseaseCategory);
        e.TemplateName = Clean(x.TemplateName);
        e.MedicineName = Clean(x.MedicineName);
        e.DoseAmount = Clean(x.DoseAmount);
        e.FrequencyPerDay = Clean(x.FrequencyPerDay);
        e.MealTiming = Clean(x.MealTiming);
        e.Morning=x.Morning;e.Noon=x.Noon;e.Evening=x.Evening;e.Bedtime=x.Bedtime;e.IntervalHours=Clean(x.IntervalHours);
        e.FinishCourse=x.FinishCourse;e.MayCauseDrowsiness=x.MayCauseDrowsiness;e.BeforeMeal30Minutes=x.BeforeMeal30Minutes;
        e.ImmediatelyAfterMeal=x.ImmediatelyAfterMeal;e.AdditionalAdvice=Clean(x.AdditionalAdvice);
    }

    private static string Clean(string? value) => value?.Trim() ?? string.Empty;

    private async Task<(bool Success, string Error, string PatientName, List<MedicationLabelSlotViewModel> Slots)> PrepareOpdSlots(
        string clinicCode, MedicationLabelInputViewModel input)
    {
        if (!input.OpdPatientId.HasValue || !input.TreatmentRecordId.HasValue)
            return (false, "กรุณาเลือกผู้ป่วยและรายการ OPD", string.Empty, []);

        var record = await db.TreatmentRecords.AsNoTracking()
            .Where(x => x.Id == input.TreatmentRecordId && x.PatientId == input.OpdPatientId && x.ClinicCode == clinicCode)
            .Select(x => new { x.TreatmentAndAdvice, PatientName = x.Patient.FullName })
            .FirstOrDefaultAsync();
        if (record is null) return (false, "ไม่พบรายการ OPD ของผู้ป่วยที่เลือก", string.Empty, []);

        List<OpdSlotAssignment>? assignments;
        try { assignments = JsonSerializer.Deserialize<List<OpdSlotAssignment>>(input.OpdSlotAssignmentsJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }); }
        catch (JsonException) { return (false, "ข้อมูลตำแหน่งฉลากไม่ถูกต้อง", string.Empty, []); }

        assignments ??= [];
        if (assignments.Count == 0) return (false, "กรุณาเลือกยาและกำหนดตำแหน่งพิมพ์อย่างน้อย 1 รายการ", string.Empty, []);
        if (assignments.Any(x => x.Position is < 1 or > 8) || assignments.GroupBy(x => x.Position).Any(x => x.Count() > 1))
            return (false, "ตำแหน่งฉลากต้องอยู่ระหว่าง 1–8 และไม่สามารถเลือกตำแหน่งซ้ำได้", string.Empty, []);

        var medicines = ParseMedicines(record.TreatmentAndAdvice);
        if (assignments.Any(x => x.MedicineIndex < 0 || x.MedicineIndex >= medicines.Count))
            return (false, "พบรายการยาที่ไม่อยู่ใน OPD ที่เลือก กรุณาโหลดรายการยาใหม่", string.Empty, []);

        var slots = assignments.Select(x =>
        {
            var medicine = medicines[x.MedicineIndex];
            medicine.Position = x.Position;
            return medicine;
        }).OrderBy(x => x.Position).ToList();
        return (true, string.Empty, record.PatientName, slots);
    }

    private static List<MedicationLabelSlotViewModel> ParseMedicines(string? source)
    {
        var text = Regex.Replace(source ?? string.Empty, @"\s+", " ").Trim();
        text = Regex.Split(text, @"กรณีเกินศักยภาพ|การติดตามผู้รับบริการ|เวลาสิ้นสุดการรับบริการ", RegexOptions.IgnoreCase)[0].Trim();
        var matches = Regex.Matches(text, @"(?<index>\d+)\.\s*(?<body>.*?)(?=(?:\s+\d+\.\s)|$)");
        var result = new List<MedicationLabelSlotViewModel>();
        foreach (Match match in matches)
        {
            var body = match.Groups["body"].Value.Trim();
            var usageMatch = Regex.Match(body, @"การใช้ยา\s*:\s*(?<value>.*?)(?=คำแนะนำ\s*:|$)", RegexOptions.IgnoreCase);
            var adviceMatch = Regex.Match(body, @"คำแนะนำ\s*:\s*(?<value>.*)$", RegexOptions.IgnoreCase);
            var name = Regex.Replace(body, @"การใช้ยา\s*:.*$", string.Empty, RegexOptions.IgnoreCase).Trim(' ', '-', '.');
            var usage = usageMatch.Success ? usageMatch.Groups["value"].Value.Trim() : string.Empty;
            var advice = adviceMatch.Success ? adviceMatch.Groups["value"].Value.Trim() : string.Empty;
            if (string.IsNullOrWhiteSpace(name)) continue;

            var dose = Regex.Match(usage, @"ครั้งละ\s*(?<value>.*?)(?=\s*วันละ|\s*(?:ก่อน|หลัง)อาหาร|$)");
            var frequency = Regex.Match(usage, @"วันละ\s*(?<value>\d+)");
            var interval = Regex.Match(usage, @"ทุก\s*(?<value>\d+(?:\.\d+)?)\s*ชั่วโมง");
            result.Add(new MedicationLabelSlotViewModel
            {
                MedicineName = name,
                DoseAmount = dose.Success ? dose.Groups["value"].Value.Trim() : string.Empty,
                FrequencyPerDay = frequency.Success ? frequency.Groups["value"].Value.Trim() : string.Empty,
                MealTiming = usage.Contains("ก่อนอาหาร") ? "ก่อนอาหาร" : usage.Contains("หลังอาหาร") ? "หลังอาหาร" : string.Empty,
                Morning = usage.Contains("เช้า"), Noon = usage.Contains("เที่ยง"), Evening = usage.Contains("เย็น"), Bedtime = usage.Contains("ก่อนนอน"),
                IntervalHours = interval.Success ? interval.Groups["value"].Value : string.Empty,
                FinishCourse = advice.Contains("จนหมด"), MayCauseDrowsiness = advice.Contains("ง่วง"),
                BeforeMeal30Minutes = advice.Contains("ก่อนอาหาร 30") || advice.Contains("ก่อนอาหาร30"),
                ImmediatelyAfterMeal = advice.Contains("หลังอาหารทันที"), AdditionalAdvice = advice
            });
        }
        return result;
    }

    private static string BuildUsageSummary(MedicationLabelSlotViewModel x) =>
        string.Join(" · ", new[] { x.DoseAmount.Length > 0 ? $"ครั้งละ {x.DoseAmount}" : "", x.FrequencyPerDay.Length > 0 ? $"วันละ {x.FrequencyPerDay} ครั้ง" : "", x.MealTiming }.Where(v => v.Length > 0));

    private sealed class OpdSlotAssignment
    {
        public int MedicineIndex { get; set; }
        public int Position { get; set; }
    }
}
