using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SmartClinic.Web.Data;
using SmartClinic.Web.Models;
using SmartClinic.Web.ViewModels;
using System.Text;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;

namespace SmartClinic.Web.Controllers;

[Authorize(Roles = "SuperAdmin,AdminClinic,Nurse")]
public class PatientsController : Controller
{
    private readonly ApplicationDbContext dbContext;
    private readonly UserManager<ApplicationUser> userManager;

    public PatientsController(ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager)
    {
        this.dbContext = dbContext;
        this.userManager = userManager;
    }

    public async Task<IActionResult> Index(string? search, int page = 1)
    {
        var currentUser = await userManager.GetUserAsync(User);
        var clinicCode = currentUser?.ClinicCode ?? "SMARTCLINIC";
        var normalizedSearch = search?.Trim();

        var patientsQuery = dbContext.Patients
            .AsNoTracking()
            .Where(patient => patient.ClinicCode == clinicCode);

        if (!string.IsNullOrWhiteSpace(normalizedSearch))
        {
            patientsQuery = patientsQuery.Where(patient =>
                patient.FullName.Contains(normalizedSearch) ||
                patient.CitizenId.Contains(normalizedSearch));
        }

        var totalItems = await patientsQuery.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalItems / (double)PatientDashboardViewModel.PageSize));
        var currentPage = Math.Clamp(page, 1, totalPages);
        var skip = (currentPage - 1) * PatientDashboardViewModel.PageSize;

        var recentPatients = await patientsQuery
            .OrderByDescending(patient => patient.CreatedAtUtc)
            .Skip(skip)
            .Take(PatientDashboardViewModel.PageSize)
            .Select(patient => new PatientSummaryViewModel
            {
                CitizenId = patient.CitizenId,
                FullName = patient.FullName,
                Address = patient.Address,
                Gender = patient.Gender,
                CreatedAt = patient.CreatedAtUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm"),
                HasPhoto = patient.PhotoData != null && patient.PhotoData.Length > 0
            })
            .ToListAsync();

        var clinic = await dbContext.Clinics.AsNoTracking().FirstOrDefaultAsync(x => x.ClinicCode == clinicCode);
        return View(new PatientDashboardViewModel
        {
            Input = new PatientRegistrationViewModel { ClinicCode = clinicCode },
            RecentPatients = recentPatients,
            CurrentPage = currentPage,
            TotalItems = totalItems,
            TotalPages = totalPages,
            SearchTerm = normalizedSearch ?? string.Empty,
            StatusMessage = "พร้อมรับข้อมูลจากบัตรประชาชนและฟอร์มผู้ป่วย",
            PatientLimit = clinic?.OpdRecordLimit ?? 30,
            IsUnlimited = clinic?.HasUnlimitedOpdRecords ?? User.IsInRole("SuperAdmin"),
            ContactName = clinic?.FullName ?? currentUser?.FullName ?? string.Empty,
            ContactPhone = clinic?.PhoneNumber ?? currentUser?.PhoneNumber ?? string.Empty
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(PatientDashboardViewModel model)
    {
        var currentUser = await userManager.GetUserAsync(User);
        var clinicCode = currentUser?.ClinicCode ?? model.Input.ClinicCode ?? "SMARTCLINIC";

        if (!ModelState.IsValid)
        {
            return await ReloadDashboard(model, clinicCode, "กรุณากรอกข้อมูลผู้ป่วยให้ครบถ้วน");
        }

        var existingPatient = await dbContext.Patients.AnyAsync(patient => patient.ClinicCode == clinicCode && patient.CitizenId == model.Input.CitizenId);
        if (existingPatient)
        {
            ModelState.AddModelError(nameof(model.Input.CitizenId), "ผู้ป่วยรายนี้ถูกบันทึกไว้แล้ว");
            ViewData["DuplicateCitizenId"] = model.Input.CitizenId;
            return await ReloadDashboard(model, clinicCode, "พบข้อมูลซ้ำในระบบ");
        }

        var patient = new Patient
        {
            ClinicCode = clinicCode,
            CitizenId = model.Input.CitizenId,
            FullName = model.Input.FullName,
            Address = model.Input.Address,
            PhoneNumber = model.Input.PhoneNumber ?? string.Empty,
            BirthDate = model.Input.BirthDate,
            Gender = model.Input.Gender
        };

        // Save photo: prefer file upload, fall back to base64 from card reader
        if (model.Input.Photo is not null && model.Input.Photo.Length > 0)
        {
            using var ms = new MemoryStream();
            await model.Input.Photo.CopyToAsync(ms);
            patient.PhotoData = ms.ToArray();
        }
        else if (!string.IsNullOrWhiteSpace(model.Input.PhotoBase64))
        {
            try { patient.PhotoData = Convert.FromBase64String(model.Input.PhotoBase64); }
            catch { /* ignore invalid base64 */ }
        }

        dbContext.Patients.Add(patient);
        try
        {
            await dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            dbContext.Entry(patient).State = EntityState.Detached;
            ModelState.AddModelError(nameof(model.Input.CitizenId), "เลขบัตรประชาชนนี้ถูกบันทึกโดยรายการอื่นแล้ว");
            ViewData["DuplicateCitizenId"] = model.Input.CitizenId;
            return await ReloadDashboard(model, clinicCode, "พบข้อมูลซ้ำในระบบ");
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ImportPdf(IFormFile medicalRecordPdf)
    {
        if (medicalRecordPdf is null || medicalRecordPdf.Length == 0)
        {
            return BadRequest(new { success = false, error = "กรุณาเลือกไฟล์ PDF เวชระเบียน" });
        }

        if (!string.Equals(medicalRecordPdf.ContentType, "application/pdf", StringComparison.OrdinalIgnoreCase) &&
            !Path.GetExtension(medicalRecordPdf.FileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { success = false, error = "รองรับเฉพาะไฟล์ PDF เท่านั้น" });
        }

        try
        {
            using var stream = medicalRecordPdf.OpenReadStream();
            using var document = PdfDocument.Open(stream);
            var textBuilder = new StringBuilder();

            foreach (var page in document.GetPages())
            {
                textBuilder.AppendLine(page.Text);
            }

            var parsed = ParsePatientProfilePdf(textBuilder.ToString());

            if (string.IsNullOrWhiteSpace(parsed.CitizenId) &&
                string.IsNullOrWhiteSpace(parsed.FullName) &&
                string.IsNullOrWhiteSpace(parsed.Address))
            {
                return BadRequest(new { success = false, error = "ไม่พบข้อมูลผู้ป่วยใน PDF ที่อัปโหลด" });
            }

            return Ok(new
            {
                success = true,
                citizenId = parsed.CitizenId,
                fullName = parsed.FullName,
                birthDate = parsed.BirthDate,
                gender = parsed.Gender,
                address = parsed.Address,
                phoneNumber = parsed.PhoneNumber,
                source = "medical-record-pdf"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, error = $"อ่านข้อมูลจาก PDF ไม่สำเร็จ: {ex.Message}" });
        }
    }

    [HttpGet("~/Patients/Photo/{citizenId}")]
    public async Task<IActionResult> Photo(string citizenId)
    {
        var currentUser = await userManager.GetUserAsync(User);
        var clinicCode = currentUser?.ClinicCode ?? "SMARTCLINIC";

        var photo = await dbContext.Patients
            .Where(p => p.ClinicCode == clinicCode && p.CitizenId == citizenId)
            .Select(p => p.PhotoData)
            .FirstOrDefaultAsync();

        if (photo == null || photo.Length == 0)
            return NotFound();

        return File(photo, "image/jpeg");
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string citizenId)
    {
        var currentUser = await userManager.GetUserAsync(User);
        var clinicCode = currentUser?.ClinicCode ?? "SMARTCLINIC";

        var patient = await dbContext.Patients
            .FirstOrDefaultAsync(p => p.ClinicCode == clinicCode && p.CitizenId == citizenId);

        if (patient == null) return NotFound();

        var model = new PatientEditViewModel
        {
            CitizenId = patient.CitizenId,
            FullName = patient.FullName,
            Address = patient.Address,
            PhoneNumber = string.IsNullOrEmpty(patient.PhoneNumber) ? null : patient.PhoneNumber,
            BirthDate = patient.BirthDate,
            Gender = patient.Gender,
            HasPhoto = patient.PhotoData != null && patient.PhotoData.Length > 0,
            ClinicCode = clinicCode
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string citizenId, PatientEditViewModel model)
    {
        var currentUser = await userManager.GetUserAsync(User);
        var clinicCode = currentUser?.ClinicCode ?? "SMARTCLINIC";

        if (!ModelState.IsValid)
        {
            model.ClinicCode = clinicCode;
            return View(model);
        }

        var patient = await dbContext.Patients
            .FirstOrDefaultAsync(p => p.ClinicCode == clinicCode && p.CitizenId == citizenId);

        if (patient == null) return NotFound();

        patient.FullName = model.FullName;
        patient.Address = model.Address;
        patient.PhoneNumber = model.PhoneNumber ?? string.Empty;
        patient.BirthDate = model.BirthDate;
        patient.Gender = model.Gender;

        if (model.Photo is not null && model.Photo.Length > 0)
        {
            using var ms = new MemoryStream();
            await model.Photo.CopyToAsync(ms);
            patient.PhotoData = ms.ToArray();
        }
        else if (!string.IsNullOrWhiteSpace(model.PhotoBase64))
        {
            try { patient.PhotoData = Convert.FromBase64String(model.PhotoBase64); }
            catch { /* ignore invalid base64 */ }
        }

        await dbContext.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string citizenId)
    {
        var currentUser = await userManager.GetUserAsync(User);
        var clinicCode = currentUser?.ClinicCode ?? "SMARTCLINIC";

        var patient = await dbContext.Patients
            .FirstOrDefaultAsync(p => p.ClinicCode == clinicCode && p.CitizenId == citizenId);

        if (patient != null)
        {
            dbContext.Patients.Remove(patient);
            await dbContext.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> ReloadDashboard(PatientDashboardViewModel model, string clinicCode, string? statusMessage)
    {
        var patientsQuery = dbContext.Patients
            .Where(patient => patient.ClinicCode == clinicCode)
            .AsNoTracking();

        var totalItems = await patientsQuery.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalItems / (double)PatientDashboardViewModel.PageSize));
        var currentPage = Math.Clamp(1, 1, totalPages);

        var recentPatients = await patientsQuery
            .OrderByDescending(patient => patient.CreatedAtUtc)
            .Take(PatientDashboardViewModel.PageSize)
            .Select(patient => new PatientSummaryViewModel
            {
                CitizenId = patient.CitizenId,
                FullName = patient.FullName,
                Address = patient.Address,
                Gender = patient.Gender,
                CreatedAt = patient.CreatedAtUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm"),
                HasPhoto = patient.PhotoData != null && patient.PhotoData.Length > 0
            })
            .ToListAsync();

        model.Input.ClinicCode = clinicCode;
        model.RecentPatients = recentPatients;
        model.CurrentPage = currentPage;
        model.TotalItems = totalItems;
        model.TotalPages = totalPages;
        model.SearchTerm = string.Empty;
        model.StatusMessage = statusMessage;
        var clinic = await dbContext.Clinics.AsNoTracking().FirstOrDefaultAsync(x => x.ClinicCode == clinicCode);
        var user = await userManager.GetUserAsync(User);
        model.PatientLimit = clinic?.OpdRecordLimit ?? 30;
        model.IsUnlimited = clinic?.HasUnlimitedOpdRecords ?? User.IsInRole("SuperAdmin");
        model.ContactName = clinic?.FullName ?? user?.FullName ?? string.Empty;
        model.ContactPhone = clinic?.PhoneNumber ?? user?.PhoneNumber ?? string.Empty;
        return View("Index", model);
    }

    private static PatientPdfImportResult ParsePatientProfilePdf(string rawText)
    {
        var text = NormalizePdfText(rawText);
        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var nameLine = lines.FirstOrDefault(line => line.Contains("สกุล", StringComparison.OrdinalIgnoreCase) && line.Contains("เพศ", StringComparison.OrdinalIgnoreCase)) ?? string.Empty;
        var fullName = MatchValue(nameLine, @"สกุล\s*(?<value>.+?)\s*เพศ");
        var gender = InferGender(fullName, nameLine);

        var citizenId = string.Empty;
        var citizenMatch = Regex.Match(text, @"เลขบัตร[^\d]*(?<value>(?:\d\s*){13})", RegexOptions.IgnoreCase);
        if (citizenMatch.Success)
        {
            citizenId = DigitsOnly(citizenMatch.Groups["value"].Value);
        }
        else
        {
            var anyIdMatch = Regex.Match(text, @"(?<!\d)(?<value>(?:\d\s*){13})(?!\d)");
            if (anyIdMatch.Success)
            {
                citizenId = DigitsOnly(anyIdMatch.Groups["value"].Value);
            }
        }

        var birthDate = string.Empty;
        var birthMatch = Regex.Match(text, @"(?:เกิด|ปีเกิด)\s*(?<value>\d{1,2}\s*/\s*[^\s/]+\s*/\s*\d{4})", RegexOptions.IgnoreCase);
        if (birthMatch.Success)
        {
            birthDate = ParseThaiBirthDate(birthMatch.Groups["value"].Value);
        }

        var address = string.Empty;
        var addressMatch = Regex.Match(
            text,
            @"(?:ที่|ที)\s*อยู่\s*\(\s*ตามบัตรประชาชน\s*\)\s*(?<value>.+?)(?=(?:ที่|ที)\s*อยู่\s*\(\s*(?:ที่|ที)\s*สามารถติดต่อได้|\.*\s*เบอร์โทรศัพท์|โรคประจําตัว|โรงพยาบาล|$)",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);
        if (addressMatch.Success)
        {
            address = addressMatch.Groups["value"].Value;
        }

        var phoneNumber = string.Empty;
        var phoneMatch = Regex.Match(text, @"เบอร์โทรศัพท์\s*(?<value>0\d{8,9})", RegexOptions.IgnoreCase);
        if (phoneMatch.Success)
        {
            phoneNumber = phoneMatch.Groups["value"].Value;
        }

        return new PatientPdfImportResult(
            citizenId,
            CleanPdfValue(fullName),
            birthDate,
            string.IsNullOrWhiteSpace(gender) ? "ไม่ระบุ" : gender,
            CleanPdfValue(address),
            phoneNumber);
    }

    private static string NormalizePdfText(string value)
    {
        var normalized = (value ?? string.Empty)
            .Replace('\0', ' ')
            .Replace('\r', '\n');
        normalized = Regex.Replace(normalized, @"[ \t]+", " ");
        normalized = Regex.Replace(normalized, @"\n{2,}", "\n");
        return normalized.Trim();
    }

    private static string MatchValue(string value, string pattern)
    {
        var match = Regex.Match(value ?? string.Empty, pattern, RegexOptions.IgnoreCase);
        return match.Success ? match.Groups["value"].Value.Trim() : string.Empty;
    }

    private static string CleanPdfValue(string value)
    {
        var cleaned = Regex.Replace(value ?? string.Empty, @"\.{3,}.*$", string.Empty);
        cleaned = Regex.Replace(cleaned, @"\s+", " ");
        return cleaned.Trim();
    }

    private static string DigitsOnly(string value)
    {
        return new string((value ?? string.Empty).Where(char.IsDigit).ToArray());
    }

    private static string InferGender(string fullName, string nameLine)
    {
        var name = fullName?.Trim() ?? string.Empty;
        if (name.StartsWith("นาย", StringComparison.OrdinalIgnoreCase) || name.StartsWith("เด็กชาย", StringComparison.OrdinalIgnoreCase))
        {
            return "ชาย";
        }

        if (name.StartsWith("นาง", StringComparison.OrdinalIgnoreCase) ||
            name.StartsWith("นางสาว", StringComparison.OrdinalIgnoreCase) ||
            name.StartsWith("น.ส.", StringComparison.OrdinalIgnoreCase) ||
            name.StartsWith("เด็กหญิง", StringComparison.OrdinalIgnoreCase))
        {
            return "หญิง";
        }

        var genderMatch = Regex.Match(nameLine ?? string.Empty, @"เพศ\s*(?<value>ชาย|หญิง|อื่นๆ)", RegexOptions.IgnoreCase);
        return genderMatch.Success ? genderMatch.Groups["value"].Value : "ไม่ระบุ";
    }

    private static string ParseThaiBirthDate(string value)
    {
        var match = Regex.Match(value ?? string.Empty, @"(?<day>\d{1,2})\s*/\s*(?<month>[^/]+?)\s*/\s*(?<year>\d{4})");
        if (!match.Success)
        {
            return string.Empty;
        }

        var monthMap = new Dictionary<string, int>
        {
            ["มกราคม"] = 1,
            ["กุมภาพันธ์"] = 2,
            ["มีนาคม"] = 3,
            ["เมษายน"] = 4,
            ["พฤษภาคม"] = 5,
            ["มิถุนายน"] = 6,
            ["กรกฎาคม"] = 7,
            ["สิงหาคม"] = 8,
            ["กันยายน"] = 9,
            ["ตุลาคม"] = 10,
            ["พฤศจิกายน"] = 11,
            ["ธันวาคม"] = 12
        };

        if (!int.TryParse(match.Groups["day"].Value, out var day) ||
            !int.TryParse(match.Groups["year"].Value, out var year))
        {
            return string.Empty;
        }

        var monthText = Regex.Replace(match.Groups["month"].Value, @"\s+", string.Empty);
        if (!monthMap.TryGetValue(monthText, out var month))
        {
            return string.Empty;
        }

        if (year > 2400)
        {
            year -= 543;
        }

        return DateOnly.TryParse($"{year:D4}-{month:D2}-{day:D2}", out var date)
            ? date.ToString("yyyy-MM-dd")
            : string.Empty;
    }
}

public sealed record PatientPdfImportResult(
    string CitizenId,
    string FullName,
    string BirthDate,
    string Gender,
    string Address,
    string PhoneNumber);
