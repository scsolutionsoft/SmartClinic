using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartClinic.Web.Data;
using SmartClinic.Web.Models;
using SmartClinic.Web.ViewModels;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;

namespace SmartClinic.Web.Controllers;

[Authorize(Roles = "SuperAdmin,AdminClinic,Nurse")]
public class PatientMedicalProfilesController : Controller
{
    private static readonly CultureInfo ThaiCulture = new("th-TH");
    private readonly ApplicationDbContext dbContext;
    private readonly UserManager<ApplicationUser> userManager;

    public PatientMedicalProfilesController(ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager)
    {
        this.dbContext = dbContext;
        this.userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? search)
    {
        var clinicCode = await GetClinicCode();
        return View(await BuildDashboard(
            clinicCode,
            "พร้อมอัปโหลดและอ่านเวชระเบียนผู้รับบริการ",
            searchTerm: search));
    }

    [HttpGet]
    public async Task<IActionResult> SearchPatients(string? q)
    {
        var clinicCode = await GetClinicCode();
        var term = Clean(q);

        if (term.Length < 2)
        {
            return Ok(Array.Empty<object>());
        }

        var patients = await dbContext.Patients
            .AsNoTracking()
            .Where(x => x.ClinicCode == clinicCode &&
                (x.FullName.Contains(term) || x.CitizenId.Contains(term)))
            .OrderBy(x => x.FullName)
            .Take(20)
            .Select(x => new
            {
                x.Id,
                x.FullName,
                x.CitizenId,
                x.BirthDate,
                x.Gender,
                x.Address,
                x.PhoneNumber
            })
            .ToListAsync();

        var patientIds = patients.Select(x => x.Id).ToArray();
        var profiles = await dbContext.PatientMedicalProfiles
            .AsNoTracking()
            .Where(x => x.ClinicCode == clinicCode && patientIds.Contains(x.PatientId))
            .Select(x => new
            {
                x.Id,
                x.PatientId,
                x.ServiceRecipientId,
                x.PatientName,
                x.Gender,
                x.BirthDate,
                x.Race,
                x.Nationality,
                x.MaritalStatus,
                x.RegisteredAddress,
                x.ContactAddress,
                x.PhoneNumber,
                x.PrimaryHospital,
                x.UnderlyingDisease,
                x.PastHistory,
                x.FamilyHistory,
                x.AllergyHistory,
                x.MedicalBenefit,
                x.EmergencyContactName,
                x.EmergencyContactPhone,
                x.InformationGivenDate
            })
            .ToListAsync();

        var profileByPatient = profiles.ToDictionary(x => x.PatientId);
        var latestServiceRecipientIds = await dbContext.TreatmentRecords
            .AsNoTracking()
            .Where(x => x.ClinicCode == clinicCode &&
                patientIds.Contains(x.PatientId) &&
                x.ServiceRecipientId != string.Empty)
            .GroupBy(x => x.PatientId)
            .Select(g => new
            {
                PatientId = g.Key,
                ServiceRecipientId = g
                    .OrderByDescending(x => x.VisitDate)
                    .ThenByDescending(x => x.CreatedAtUtc)
                    .Select(x => x.ServiceRecipientId)
                    .FirstOrDefault()
            })
            .ToDictionaryAsync(x => x.PatientId, x => x.ServiceRecipientId ?? string.Empty);

        var results = patients.Select(patient =>
        {
            profileByPatient.TryGetValue(patient.Id, out var profile);
            latestServiceRecipientIds.TryGetValue(patient.Id, out var serviceRecipientId);
            return new
            {
                id = patient.Id,
                fullName = patient.FullName,
                citizenId = patient.CitizenId,
                serviceRecipientId = profile?.ServiceRecipientId ?? serviceRecipientId ?? string.Empty,
                birthDate = ToDateInput(patient.BirthDate),
                gender = patient.Gender,
                address = patient.Address,
                phoneNumber = patient.PhoneNumber,
                hasProfile = profile is not null,
                profileId = profile?.Id,
                profile = profile is null ? null : new
                {
                    profile.ServiceRecipientId,
                    profile.PatientName,
                    profile.Gender,
                    BirthDate = ToDateInput(profile.BirthDate),
                    profile.Race,
                    profile.Nationality,
                    profile.MaritalStatus,
                    profile.RegisteredAddress,
                    profile.ContactAddress,
                    profile.PhoneNumber,
                    profile.PrimaryHospital,
                    profile.UnderlyingDisease,
                    profile.PastHistory,
                    profile.FamilyHistory,
                    profile.AllergyHistory,
                    profile.MedicalBenefit,
                    profile.EmergencyContactName,
                    profile.EmergencyContactPhone,
                    InformationGivenDate = ToDateInput(profile.InformationGivenDate)
                }
            };
        });

        return Ok(results);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ImportPdf(IFormFile sourcePdfFile)
    {
        if (sourcePdfFile is null || sourcePdfFile.Length == 0)
        {
            return BadRequest(new { success = false, error = "กรุณาเลือกไฟล์ PDF" });
        }

        if (!IsPdf(sourcePdfFile))
        {
            return BadRequest(new { success = false, error = "รองรับเฉพาะไฟล์ PDF เท่านั้น" });
        }

        try
        {
            var text = ReadPdfText(sourcePdfFile);
            var parsed = ParsePatientMedicalProfilePdf(text);
            return Ok(new { success = true, data = parsed });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, error = $"อ่านข้อมูลจาก PDF ไม่สำเร็จ: {ex.Message}" });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PatientMedicalProfilesDashboardViewModel model)
    {
        var clinicCode = await GetClinicCode();
        var clinic = await dbContext.Clinics
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ClinicCode == clinicCode);

        if (model.Input.SourcePdfFile is not null && model.Input.SourcePdfFile.Length > 0 && !IsPdf(model.Input.SourcePdfFile))
        {
            ModelState.AddModelError(nameof(model.Input.SourcePdfFile), "รองรับเฉพาะไฟล์ PDF เท่านั้น");
        }

        var citizenId = DigitsOnly(model.Input.CitizenId ?? string.Empty);
        if (citizenId.Length != 13)
        {
            ModelState.AddModelError(nameof(model.Input.CitizenId), "กรุณาระบุเลขบัตรประชาชน 13 หลัก");
        }

        if (string.IsNullOrWhiteSpace(model.Input.PatientName))
        {
            ModelState.AddModelError(nameof(model.Input.PatientName), "กรุณาระบุชื่อ-สกุล");
        }

        var duplicateProfile = citizenId.Length == 13 && await dbContext.PatientMedicalProfiles
            .AsNoTracking()
            .AnyAsync(x => x.ClinicCode == clinicCode && x.CitizenId == citizenId);
        if (duplicateProfile)
        {
            ModelState.AddModelError(nameof(model.Input.CitizenId), "ผู้ป่วยรายนี้มีเวชระเบียนผู้รับบริการอยู่แล้ว ระบบอนุญาตเพียง 1 รายงานต่อ 1 คน");
        }

        if (!ModelState.IsValid)
        {
            return View("Index", await BuildDashboard(clinicCode, "กรุณาตรวจข้อมูลก่อนบันทึก", model));
        }

        var patient = await ResolvePatient(clinicCode, model.Input, citizenId);

        byte[]? sourcePdfData = null;
        string? sourcePdfFileName = null;
        string? sourcePdfContentType = null;
        if (model.Input.SourcePdfFile is not null && model.Input.SourcePdfFile.Length > 0)
        {
            await using var stream = new MemoryStream();
            await model.Input.SourcePdfFile.CopyToAsync(stream);
            sourcePdfData = stream.ToArray();
            sourcePdfFileName = BuildSourceFileName(patient.CitizenId, model.Input.InformationGivenDate);
            sourcePdfContentType = "application/pdf";
        }

        var profile = new PatientMedicalProfile
        {
            ClinicCode = clinicCode,
            PatientId = patient.Id,
            CitizenId = patient.CitizenId,
            ServiceRecipientId = Clean(model.Input.ServiceRecipientId),
            ClinicName = Clean(clinic?.ClinicName) is { Length: > 0 } clinicName ? clinicName : Clean(model.Input.ClinicName),
            ClinicAddress = Clean(clinic?.Address) is { Length: > 0 } clinicAddress ? clinicAddress : Clean(model.Input.ClinicAddress),
            PatientName = Clean(model.Input.PatientName),
            Gender = Clean(model.Input.Gender),
            BirthDate = model.Input.BirthDate,
            Race = Clean(model.Input.Race),
            Nationality = Clean(model.Input.Nationality),
            MaritalStatus = Clean(model.Input.MaritalStatus),
            RegisteredAddress = Clean(model.Input.RegisteredAddress),
            ContactAddress = Clean(model.Input.ContactAddress),
            PhoneNumber = Clean(model.Input.PhoneNumber),
            PrimaryHospital = Clean(model.Input.PrimaryHospital),
            UnderlyingDisease = Clean(model.Input.UnderlyingDisease),
            PastHistory = Clean(model.Input.PastHistory),
            FamilyHistory = Clean(model.Input.FamilyHistory),
            AllergyHistory = Clean(model.Input.AllergyHistory),
            MedicalBenefit = Clean(model.Input.MedicalBenefit),
            EmergencyContactName = Clean(model.Input.EmergencyContactName),
            EmergencyContactPhone = Clean(model.Input.EmergencyContactPhone),
            InformationGivenDate = model.Input.InformationGivenDate,
            SourcePdfFileName = sourcePdfFileName,
            SourcePdfContentType = sourcePdfContentType,
            SourcePdfData = sourcePdfData
        };

        dbContext.PatientMedicalProfiles.Add(profile);
        await dbContext.SaveChangesAsync();

        TempData["PatientMedicalProfileStatus"] = "created";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Preview(int id)
    {
        var clinicCode = await GetClinicCode();
        var profile = await dbContext.PatientMedicalProfiles.FirstOrDefaultAsync(x => x.Id == id && x.ClinicCode == clinicCode);
        if (profile is null)
        {
            return NotFound();
        }

        if (profile.SourcePdfData is null || profile.SourcePdfData.Length == 0)
        {
            return NotFound("ไม่พบไฟล์ PDF ต้นฉบับ");
        }

        return File(profile.SourcePdfData, profile.SourcePdfContentType ?? "application/pdf", enableRangeProcessing: true);
    }

    [HttpGet]
    public async Task<IActionResult> Report(int id)
    {
        var clinicCode = await GetClinicCode();
        var profile = await dbContext.PatientMedicalProfiles
            .AsNoTracking()
            .Include(x => x.Patient)
            .FirstOrDefaultAsync(x => x.Id == id && x.ClinicCode == clinicCode);

        if (profile is null)
        {
            return NotFound();
        }

        var hasPatientSignature = await dbContext.SignImgs
            .AsNoTracking()
            .AnyAsync(x => x.ClinicCode == clinicCode && x.CitizenId == profile.CitizenId);
        var hasPatientPhoto = profile.Patient.PhotoData is { Length: > 0 };
        var clinicLogoPath = await dbContext.Clinics
            .AsNoTracking()
            .Where(x => x.ClinicCode == clinicCode)
            .Select(x => x.LogoPath)
            .FirstOrDefaultAsync();
        var adminUsers = await userManager.GetUsersInRoleAsync("AdminClinic");
        var clinicAdmin = adminUsers
            .Where(x => x.ClinicCode == clinicCode)
            .OrderBy(x => x.UserName)
            .FirstOrDefault();

        ViewBag.LogoUrl = string.IsNullOrWhiteSpace(clinicLogoPath)
            ? Url.Content("~/img/community-clinic-logo.svg")
            : clinicLogoPath;
        ViewBag.PatientPhotoUrl = hasPatientPhoto
            ? Url.Action("Photo", "Patients", new { citizenId = profile.CitizenId })
            : Url.Content("~/img/photo-not-available.svg");
        ViewBag.PatientSignatureUrl = hasPatientSignature
            ? Url.Action("ImageByCitizen", "Signatures", new { citizenId = profile.CitizenId })
            : null;
        ViewBag.HasPatientSignature = hasPatientSignature;
        ViewBag.ProviderName = string.IsNullOrWhiteSpace(clinicAdmin?.FullName) ? clinicAdmin?.UserName ?? string.Empty : clinicAdmin.FullName;
        ViewBag.ProviderTitle = clinicAdmin?.ProfessionalTitle ?? string.Empty;
        ViewBag.ProviderSignatureUrl = clinicAdmin?.ProviderSignatureImageData is { Length: > 0 }
            ? Url.Action("ProviderSignature", "MedicalRecords", new { userId = clinicAdmin.Id })
            : null;
        ViewBag.HasProviderSignature = clinicAdmin?.ProviderSignatureImageData is { Length: > 0 };

        return View(profile);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var clinicCode = await GetClinicCode();
        var profile = await dbContext.PatientMedicalProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.ClinicCode == clinicCode);

        if (profile is null)
        {
            return NotFound();
        }

        return View(MapEditModel(profile));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(PatientMedicalProfileEditViewModel model)
    {
        var clinicCode = await GetClinicCode();
        var clinic = await dbContext.Clinics
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ClinicCode == clinicCode);
        var profile = await dbContext.PatientMedicalProfiles
            .Include(x => x.Patient)
            .FirstOrDefaultAsync(x => x.Id == model.Id && x.ClinicCode == clinicCode);

        if (profile is null)
        {
            return NotFound();
        }

        model.CurrentPdfFileName = profile.SourcePdfFileName ?? string.Empty;
        model.HasPdfFile = profile.SourcePdfData is { Length: > 0 };
        var citizenId = DigitsOnly(model.CitizenId ?? string.Empty);
        if (citizenId.Length != 13)
        {
            ModelState.AddModelError(nameof(model.CitizenId), "กรุณาระบุเลขบัตรประชาชน 13 หลัก");
        }

        if (string.IsNullOrWhiteSpace(model.PatientName))
        {
            ModelState.AddModelError(nameof(model.PatientName), "กรุณาระบุชื่อ-สกุล");
        }

        if (model.SourcePdfFile is not null && model.SourcePdfFile.Length > 0 && !IsPdf(model.SourcePdfFile))
        {
            ModelState.AddModelError(nameof(model.SourcePdfFile), "รองรับเฉพาะไฟล์ PDF เท่านั้น");
        }

        var duplicatePatient = citizenId.Length == 13 && await dbContext.Patients
            .AsNoTracking()
            .AnyAsync(x => x.ClinicCode == clinicCode && x.CitizenId == citizenId && x.Id != profile.PatientId);
        if (duplicatePatient)
        {
            ModelState.AddModelError(nameof(model.CitizenId), "เลขบัตรประชาชนนี้มีอยู่กับผู้ป่วยรายอื่นแล้ว");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        profile.Patient.CitizenId = citizenId;
        profile.Patient.FullName = Clean(model.PatientName);
        profile.Patient.Gender = string.IsNullOrWhiteSpace(model.Gender) ? "ไม่ระบุ" : Clean(model.Gender);
        profile.Patient.BirthDate = model.BirthDate;
        profile.Patient.Address = Clean(model.RegisteredAddress);
        profile.Patient.PhoneNumber = Clean(model.PhoneNumber);

        profile.CitizenId = citizenId;
        profile.ServiceRecipientId = Clean(model.ServiceRecipientId);
        profile.ClinicName = Clean(clinic?.ClinicName) is { Length: > 0 } clinicName ? clinicName : profile.ClinicName;
        profile.ClinicAddress = Clean(clinic?.Address) is { Length: > 0 } clinicAddress ? clinicAddress : profile.ClinicAddress;
        profile.PatientName = Clean(model.PatientName);
        profile.Gender = Clean(model.Gender);
        profile.BirthDate = model.BirthDate;
        profile.Race = Clean(model.Race);
        profile.Nationality = Clean(model.Nationality);
        profile.MaritalStatus = Clean(model.MaritalStatus);
        profile.RegisteredAddress = Clean(model.RegisteredAddress);
        profile.ContactAddress = Clean(model.ContactAddress);
        profile.PhoneNumber = Clean(model.PhoneNumber);
        profile.PrimaryHospital = Clean(model.PrimaryHospital);
        profile.UnderlyingDisease = Clean(model.UnderlyingDisease);
        profile.PastHistory = Clean(model.PastHistory);
        profile.FamilyHistory = Clean(model.FamilyHistory);
        profile.AllergyHistory = Clean(model.AllergyHistory);
        profile.MedicalBenefit = Clean(model.MedicalBenefit);
        profile.EmergencyContactName = Clean(model.EmergencyContactName);
        profile.EmergencyContactPhone = Clean(model.EmergencyContactPhone);
        profile.InformationGivenDate = model.InformationGivenDate;
        profile.UpdatedAtUtc = DateTime.UtcNow;

        if (model.SourcePdfFile is not null && model.SourcePdfFile.Length > 0)
        {
            await using var stream = new MemoryStream();
            await model.SourcePdfFile.CopyToAsync(stream);
            profile.SourcePdfFileName = BuildSourceFileName(citizenId, model.InformationGivenDate);
            profile.SourcePdfContentType = "application/pdf";
            profile.SourcePdfData = stream.ToArray();
        }

        await dbContext.SaveChangesAsync();
        TempData["PatientMedicalProfileStatus"] = "updated";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadPdf(int id, IFormFile? sourcePdfFile)
    {
        var clinicCode = await GetClinicCode();
        var profile = await dbContext.PatientMedicalProfiles
            .FirstOrDefaultAsync(x => x.Id == id && x.ClinicCode == clinicCode);
        if (profile is null) return NotFound(new { success = false, error = "ไม่พบเวชระเบียนผู้รับบริการ" });
        if (sourcePdfFile is null || sourcePdfFile.Length == 0)
            return BadRequest(new { success = false, error = "กรุณาเลือกไฟล์ PDF" });
        if (!IsPdf(sourcePdfFile))
            return BadRequest(new { success = false, error = "รองรับเฉพาะไฟล์ PDF เท่านั้น" });
        if (sourcePdfFile.Length > 20 * 1024 * 1024)
            return BadRequest(new { success = false, error = "ไฟล์ PDF ต้องมีขนาดไม่เกิน 20 MB" });

        await using var stream = new MemoryStream();
        await sourcePdfFile.CopyToAsync(stream);
        profile.SourcePdfFileName = BuildSourceFileName(profile.CitizenId, profile.InformationGivenDate);
        profile.SourcePdfContentType = "application/pdf";
        profile.SourcePdfData = stream.ToArray();
        profile.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();

        return Ok(new {
            success = true,
            fileName = profile.SourcePdfFileName,
            previewUrl = Url.Action(nameof(Preview), new { id = profile.Id })
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var clinicCode = await GetClinicCode();
        var profile = await dbContext.PatientMedicalProfiles
            .FirstOrDefaultAsync(x => x.Id == id && x.ClinicCode == clinicCode);

        if (profile is null)
        {
            return NotFound();
        }

        dbContext.PatientMedicalProfiles.Remove(profile);
        await dbContext.SaveChangesAsync();
        TempData["PatientMedicalProfileStatus"] = "deleted";
        return RedirectToAction(nameof(Index));
    }

    private async Task<Patient> ResolvePatient(string clinicCode, PatientMedicalProfileCreateViewModel input, string citizenId)
    {
        Patient? patient = null;
        if (input.PatientId.HasValue)
        {
            patient = await dbContext.Patients.FirstOrDefaultAsync(x => x.Id == input.PatientId.Value && x.ClinicCode == clinicCode);
        }

        patient ??= await dbContext.Patients.FirstOrDefaultAsync(x => x.ClinicCode == clinicCode && x.CitizenId == citizenId);

        if (patient is null)
        {
            patient = new Patient
            {
                ClinicCode = clinicCode,
                CitizenId = citizenId,
                FullName = Clean(input.PatientName),
                Address = Clean(input.RegisteredAddress),
                PhoneNumber = Clean(input.PhoneNumber),
                BirthDate = input.BirthDate,
                Gender = string.IsNullOrWhiteSpace(input.Gender) ? "ไม่ระบุ" : Clean(input.Gender)
            };
            dbContext.Patients.Add(patient);
            await dbContext.SaveChangesAsync();
            return patient;
        }

        patient.FullName = Clean(input.PatientName);
        patient.Address = Clean(input.RegisteredAddress);
        patient.PhoneNumber = Clean(input.PhoneNumber);
        patient.BirthDate = input.BirthDate;
        patient.Gender = string.IsNullOrWhiteSpace(input.Gender) ? patient.Gender : Clean(input.Gender);
        await dbContext.SaveChangesAsync();
        return patient;
    }

    private static PatientMedicalProfileEditViewModel MapEditModel(PatientMedicalProfile profile)
    {
        return new PatientMedicalProfileEditViewModel
        {
            Id = profile.Id,
            ServiceRecipientId = profile.ServiceRecipientId,
            CitizenId = profile.CitizenId,
            PatientName = profile.PatientName,
            Gender = profile.Gender,
            BirthDate = profile.BirthDate,
            Race = profile.Race,
            Nationality = profile.Nationality,
            MaritalStatus = profile.MaritalStatus,
            RegisteredAddress = profile.RegisteredAddress,
            ContactAddress = profile.ContactAddress,
            PhoneNumber = profile.PhoneNumber,
            PrimaryHospital = profile.PrimaryHospital,
            UnderlyingDisease = profile.UnderlyingDisease,
            PastHistory = string.IsNullOrWhiteSpace(profile.PastHistory) ? "ปฏิเสธ" : profile.PastHistory,
            FamilyHistory = string.IsNullOrWhiteSpace(profile.FamilyHistory) ? "ปฏิเสธ" : profile.FamilyHistory,
            AllergyHistory = string.IsNullOrWhiteSpace(profile.AllergyHistory) ? "ปฏิเสธ" : profile.AllergyHistory,
            MedicalBenefit = profile.MedicalBenefit,
            EmergencyContactName = profile.EmergencyContactName,
            EmergencyContactPhone = profile.EmergencyContactPhone,
            InformationGivenDate = profile.InformationGivenDate,
            CurrentPdfFileName = profile.SourcePdfFileName ?? string.Empty,
            HasPdfFile = profile.SourcePdfData is { Length: > 0 }
        };
    }

    private async Task<PatientMedicalProfilesDashboardViewModel> BuildDashboard(
        string clinicCode,
        string statusMessage,
        PatientMedicalProfilesDashboardViewModel? existingModel = null,
        string? searchTerm = null)
    {
        var normalizedSearch = searchTerm?.Trim();
        var profilesQuery = dbContext.PatientMedicalProfiles
            .AsNoTracking()
            .Where(x => x.ClinicCode == clinicCode);

        if (!string.IsNullOrWhiteSpace(normalizedSearch))
        {
            profilesQuery = profilesQuery.Where(x =>
                x.PatientName.Contains(normalizedSearch) ||
                x.CitizenId.Contains(normalizedSearch));
        }

        var profileRows = await profilesQuery
            .OrderByDescending(x => x.InformationGivenDate)
            .ThenByDescending(x => x.CreatedAtUtc)
            .Take(50)
            .Select(x => new
            {
                x.Id,
                x.PatientName,
                x.CitizenId,
                x.ServiceRecipientId,
                x.InformationGivenDate,
                x.PhoneNumber,
                x.MedicalBenefit,
                x.SourcePdfFileName,
                HasPdfFile = x.SourcePdfData != null && x.SourcePdfData.Length > 0,
                x.CreatedAtUtc
            })
            .ToListAsync();

        var profiles = profileRows
            .Select(x => new PatientMedicalProfileItemViewModel
            {
                Id = x.Id,
                PatientName = x.PatientName,
                CitizenId = x.CitizenId,
                ServiceRecipientId = x.ServiceRecipientId,
                InformationGivenDateText = x.InformationGivenDate.HasValue ? x.InformationGivenDate.Value.ToString("dd MMMM yyyy", ThaiCulture) : "-",
                PhoneNumber = x.PhoneNumber,
                MedicalBenefit = x.MedicalBenefit,
                FileName = x.SourcePdfFileName ?? string.Empty,
                HasPdfFile = x.HasPdfFile,
                CreatedAtText = x.CreatedAtUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm", ThaiCulture)
            })
            .ToList();

        var model = existingModel ?? new PatientMedicalProfilesDashboardViewModel();
        model.Profiles = profiles;
        model.SearchTerm = normalizedSearch ?? string.Empty;
        model.StatusMessage = statusMessage;
        return model;
    }

    private async Task<string> GetClinicCode()
    {
        var user = await userManager.GetUserAsync(User);
        return user?.ClinicCode ?? "SMARTCLINIC";
    }

    private static bool IsPdf(IFormFile file)
    {
        return string.Equals(file.ContentType, "application/pdf", StringComparison.OrdinalIgnoreCase) ||
            Path.GetExtension(file.FileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase);
    }

    private static string ReadPdfText(IFormFile file)
    {
        using var stream = file.OpenReadStream();
        using var document = PdfDocument.Open(stream);
        var textBuilder = new StringBuilder();
        foreach (var page in document.GetPages())
        {
            textBuilder.AppendLine(page.Text);
        }

        return textBuilder.ToString();
    }

    private static PatientMedicalProfilePdfImportViewModel ParsePatientMedicalProfilePdf(string rawText)
    {
        var text = NormalizePdfText(rawText);

        var nameLine = Regex.Match(text, @"(?:ชื่อ|ชื.อ)-สกุล\s*:?\s*(?<value>.+?)\s*เพศ", RegexOptions.Singleline);
        var patientName = CleanPdfValue(nameLine.Success ? nameLine.Groups["value"].Value : string.Empty);
        var gender = InferGender(patientName, text);

        return new PatientMedicalProfilePdfImportViewModel
        {
            ServiceRecipientId = FirstValue(text,
                @"เลขประจําตัวผู้รับบริการ\s*:?\s*(?<value>[0-9\-]+)",
                @"เลขประจำตัวผู้รับบริการ\s*:?\s*(?<value>[0-9\-]+)"),
            ClinicName = FirstValue(text, @"ชื่อคลินิก\s*(?<value>.+?)(?=(?:ที่|ที).?ตั.?ง|รหัสคลินิก|พื้นที่|เวชระเบียน|ข้อมูลและประวัติ)"),
            ClinicAddress = FirstValue(text, @"(?:ที่|ที).?ตั.?ง\s*(?<value>.+?)(?=ข้อมูลและประวัติ|เวชระเบียน|ชื่อ-สกุล|ชื.อ-สกุล)"),
            PatientName = patientName,
            Gender = gender,
            BirthDate = ParseThaiDateForInput(FirstValue(text, @"(?:วัน/เดือน/ป.?เกิด|วัน/เดือน/ปีเกิด)\s*:?\s*(?<value>\d{1,2}\s*/\s*[^ \n/]+\s*/\s*\d{4})")),
            Race = FirstValue(text, @"เชื้อชาติ\s*:?\s*(?<value>ไทย|[^\s]+)"),
            Nationality = FirstValue(text, @"สัญชาติ\s*:?\s*(?<value>ไทย|[^\s]+)"),
            CitizenId = DigitsOnly(FirstValue(text, @"เลขบัตรประจําตัวประชาชน\s*:?\s*(?<value>(?:\d\s*){13})", @"เลขบัตรประจำตัวประชาชน\s*:?\s*(?<value>(?:\d\s*){13})", @"(?<value>(?:\d\s*){13})")),
            MaritalStatus = FirstValue(text, @"สถานภาพ\s*:?\s*(?<value>โสด|สมรส|หม้าย|หย่า|[^\s]+)"),
            RegisteredAddress = FirstValue(text, @"(?:ที่|ที).?อยู่\s*\(\s*ตาม(?:บัตร|บัต).*?ประชาชน\s*\)\s*:?\s*(?<value>.+?)(?=(?:ที่|ที).?อยู่\s*\(\s*(?:ที่|ที).?สามารถ|เบอร์โทร|โรคประจํา|โรคประจำ|โรงพยาบาล|$)"),
            ContactAddress = FirstValue(text, @"(?:ที่|ที).?อยู่\s*\(\s*(?:ที่|ที).?สามารถติดต่อได้\s*\)\s*:?\s*(?<value>.+?)(?=(?:\.*\s*)?เบอร์โทร|โรคประจํา|โรคประจำ|โรงพยาบาล|$)"),
            PhoneNumber = FirstValue(text, @"เบอร์โทร(?:ศัพท์)?\s*:?\s*(?<value>0\d{8,9})", @"โทร\s*(?<value>0\d{8,9})"),
            UnderlyingDisease = FirstValue(text, @"โรคประจํา(?:ตัว)?\s*:?\s*(?<value>.+?)(?=โรงพยาบาล|หน่วยบริการ|ประวัติเจ็บ|ประวัติการเจ็บ|$)"),
            PrimaryHospital = FirstValue(text, @"(?:โรงพยาบาล\s*/\s*หน่วยบริการประจํา|โรงพยาบาล/หน่วยบริการประจํา|หน่วยบริการประจำ)\s*:?\s*(?<value>.+?)(?=ประวัติเจ็บ|ประวัติการเจ็บ|$)"),
            PastHistory = DefaultDenied(FirstValue(text, @"(?:ประวัติเจ็บป.?วยในอดีต|ประวัติการเจ็บป.?วยในอดีต).*?\s*:?\s*(?<value>.+?)(?=ประวัติครอบครัว|Family History|$)")),
            FamilyHistory = DefaultDenied(FirstValue(text, @"ประวัติครอบครัว.*?\s*:?\s*(?<value>.+?)(?=ประวัติการแพ้|สิทธิการรักษา|$)")),
            AllergyHistory = DefaultDenied(FirstValue(text, @"ประวัติการแพ้ยา\s*/\s*อาหาร\s*/\s*สารเคมี\s*:?\s*(?<value>.+?)(?=สิทธิการรักษา|ในกรณีฉุกเฉิน|$)")),
            MedicalBenefit = NormalizeMedicalBenefit(FirstValue(text, @"สิทธิการรักษาพยาบาล\s*:?\s*(?<value>.+?)(?=ในกรณีฉุกเฉิน|บุคคลที่สามารถ|วัน/เดือน|$)")),
            EmergencyContactName = BlankPlaceholder(FirstValue(text, @"บุคคล(?:ที่|ที).?สามารถติดต่อได้(?:ชื่อ-สกุล)?\s*:?\s*(?<value>.+?)(?=เบอร์โทร|วัน/เดือน|$)")),
            EmergencyContactPhone = BlankPlaceholder(FirstValue(text, @"ในกรณีฉุกเฉิน.+?เบอร์โทร(?:ศัพท์)?\s*:?\s*(?<value>0\d{8,9})")),
            InformationGivenDate = ParseThaiDateForInput(FirstValue(text, @"(?:วัน/เดือน/ป.?|วัน.?/เดือน/ปี)\s*(?:ที่|ที).?ให้ข้อมูล\s*:?\s*(?<value>\d{1,2}\s*/\s*[^ \n/]+\s*/\s*\d{4})", @"(?:วัน.?/เดือน/ปี)\s*(?:ที่|ที).?ให้ข้อมูล\s*:?\s*(?<value>\d{1,2}\s+\S+\s+\d{4})"))
        };
    }

    private static string FirstValue(string text, params string[] patterns)
    {
        foreach (var pattern in patterns)
        {
            var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (match.Success)
            {
                return CleanPdfValue(match.Groups["value"].Value);
            }
        }

        return string.Empty;
    }

    private static string NormalizePdfText(string value)
    {
        var normalized = (value ?? string.Empty)
            .Replace('\0', ' ')
            .Replace('\r', '\n')
            .Replace("\u00c1", "เ")
            .Replace("\u0085", "ข")
            .Replace("ผ\u00bco", "ผู้")
            .Replace("บริ\u0084าร", "บริการ");
        normalized = Regex.Replace(normalized, @"[ \t]+", " ");
        normalized = Regex.Replace(normalized, @"\n{2,}", "\n");
        return normalized.Trim();
    }

    private static string CleanPdfValue(string value)
    {
        var cleaned = Regex.Replace(value ?? string.Empty, @"\.{3,}.*?(?=\s|$)", string.Empty);
        cleaned = Regex.Replace(cleaned, @"[✓□]+", string.Empty);
        cleaned = Regex.Replace(cleaned, @"\s+", " ");
        return cleaned.Trim(' ', ':', '-', '.', '\t', '\n');
    }

    private static string Clean(string? value)
    {
        return Regex.Replace(value ?? string.Empty, @"\s+", " ").Trim();
    }

    private static string DefaultDenied(string value)
    {
        var text = BlankPlaceholder(value);
        return string.IsNullOrWhiteSpace(text) ? "ปฏิเสธ" : text;
    }

    private static string BlankPlaceholder(string value)
    {
        var text = Clean(value);
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var markerless = Regex.Replace(text, @"[\.\-_/\\\s]+", string.Empty);
        return string.IsNullOrWhiteSpace(markerless) ? string.Empty : text;
    }

    private static string NormalizeMedicalBenefit(string value)
    {
        var text = Clean(value);
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        if (text.Contains("ถ้วนหน้า", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("30", StringComparison.OrdinalIgnoreCase))
        {
            return "ประกันสุขภาพถ้วนหน้า (30 บาท)";
        }

        if (text.Contains("ข้าราชการ", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("เบิกจ่ายตรง", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("รัฐวิสาหกิจ", StringComparison.OrdinalIgnoreCase))
        {
            return "เบิกจ่ายตรง-ข้าราชการ";
        }

        if (text.Contains("ประกันสังคม", StringComparison.OrdinalIgnoreCase))
        {
            return "ประกันสังคม";
        }

        if (text.Contains("จ่ายเงินเอง", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("ประสงค์จ่าย", StringComparison.OrdinalIgnoreCase))
        {
            return "ทราบสิทธิแต่ประสงค์จ่ายเงินเอง";
        }

        return "อื่นๆ";
    }

    private static string DigitsOnly(string value)
    {
        return new string((value ?? string.Empty).Where(char.IsDigit).ToArray());
    }

    private static string InferGender(string patientName, string text)
    {
        var name = patientName.Trim();
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

        var gender = FirstValue(text, @"เพศ\s*:?\s*(?<value>ชาย|หญิง|อื่นๆ|อื่น)");
        return string.IsNullOrWhiteSpace(gender) ? "ไม่ระบุ" : gender;
    }

    private static string ParseThaiDateForInput(string value)
    {
        var text = CleanPdfValue(value);
        var slash = Regex.Match(text, @"(?<day>\d{1,2})\s*/\s*(?<month>[^/]+?)\s*/\s*(?<year>\d{4})");
        var spaced = Regex.Match(text, @"(?<day>\d{1,2})\s+(?<month>[ก-๙]+)\s+(?<year>\d{4})");
        var match = slash.Success ? slash : spaced;
        if (!match.Success || !int.TryParse(match.Groups["day"].Value, out var day) || !int.TryParse(match.Groups["year"].Value, out var year))
        {
            return string.Empty;
        }

        var month = ParseThaiMonth(match.Groups["month"].Value);
        if (month < 1)
        {
            return string.Empty;
        }

        if (year > 2400)
        {
            year -= 543;
        }

        return DateOnly.TryParseExact($"{year:D4}-{month:D2}-{day:D2}", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
            ? parsed.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            : string.Empty;
    }

    private static int ParseThaiMonth(string value)
    {
        var monthText = CleanPdfValue(value);
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

        if (int.TryParse(monthText, out var numericMonth))
        {
            return numericMonth;
        }

        return monthMap.FirstOrDefault(x => monthText.Contains(x.Key, StringComparison.OrdinalIgnoreCase)).Value;
    }

    private static string BuildSourceFileName(string citizenId, DateOnly? informationDate)
    {
        var suffix = informationDate.HasValue
            ? informationDate.Value.ToString("yyyyMMdd", CultureInfo.InvariantCulture)
            : DateTime.Now.ToString("yyyyMMddHHmm", CultureInfo.InvariantCulture);
        return $"{citizenId}-{suffix}.pdf";
    }

    private static string ToDateInput(DateOnly? date)
    {
        return date.HasValue ? date.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : string.Empty;
    }
}
