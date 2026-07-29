using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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
public class MedicalRecordsController : Controller
{
    private readonly ApplicationDbContext dbContext;
    private readonly UserManager<ApplicationUser> userManager;

    public MedicalRecordsController(ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager)
    {
        this.dbContext = dbContext;
        this.userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? patientId = null)
    {
        var clinicCode = await GetClinicCode();
        var model = await BuildDashboard(clinicCode, "พร้อมอัปโหลด OPD Card และพรีวิว PDF", patientId);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> SearchPatients(string? q)
    {
        var clinicCode = await GetClinicCode();
        var term = (q ?? string.Empty).Trim();

        if (term.Length < 2)
        {
            return Ok(Array.Empty<PatientSearchResultViewModel>());
        }

        var patients = await dbContext.Patients
            .AsNoTracking()
            .Where(x => x.ClinicCode == clinicCode &&
                (x.FullName.Contains(term) || x.CitizenId.Contains(term)))
            .OrderBy(x => x.FullName)
            .Take(20)
            .Select(x => new PatientSearchResultViewModel
            {
                Id = x.Id,
                FullName = x.FullName,
                CitizenId = x.CitizenId
            })
            .ToListAsync();

        return Ok(patients);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ImportPdf(IFormFile opdPdfFile)
    {
        if (opdPdfFile is null || opdPdfFile.Length == 0)
        {
            return BadRequest(new { success = false, error = "กรุณาเลือกไฟล์ PDF" });
        }

        if (!string.Equals(opdPdfFile.ContentType, "application/pdf", StringComparison.OrdinalIgnoreCase) &&
            !Path.GetExtension(opdPdfFile.FileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { success = false, error = "รองรับเฉพาะไฟล์ PDF เท่านั้น" });
        }

        try
        {
            using var stream = opdPdfFile.OpenReadStream();
            using var document = PdfDocument.Open(stream);
            var textBuilder = new StringBuilder();

            foreach (var page in document.GetPages())
            {
                textBuilder.AppendLine(page.Text);
            }

            var parsed = ParseMedicalRecordPdf(textBuilder.ToString());
            return Ok(new { success = true, data = parsed });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, error = $"อ่านข้อมูลจาก PDF ไม่สำเร็จ: {ex.Message}" });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(MedicalRecordsDashboardViewModel model)
    {
        var clinicCode = await GetClinicCode();

        if (!ModelState.IsValid)
        {
            return View("Index", await BuildDashboard(clinicCode, "กรุณากรอกข้อมูลให้ครบถ้วน", model.Input.PatientId, model));
        }

        if (model.Input.OpdPdfFile is null || model.Input.OpdPdfFile.Length == 0)
        {
            ModelState.AddModelError(nameof(model.Input.OpdPdfFile), "กรุณาเลือกไฟล์ PDF");
            return View("Index", await BuildDashboard(clinicCode, "ไม่พบไฟล์ OPD Card", model.Input.PatientId, model));
        }

        if (!string.Equals(model.Input.OpdPdfFile.ContentType, "application/pdf", StringComparison.OrdinalIgnoreCase) &&
            !Path.GetExtension(model.Input.OpdPdfFile.FileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(nameof(model.Input.OpdPdfFile), "รองรับเฉพาะไฟล์ PDF เท่านั้น");
            return View("Index", await BuildDashboard(clinicCode, "ไฟล์ไม่ใช่ PDF", model.Input.PatientId, model));
        }

        var patient = await dbContext.Patients.FirstOrDefaultAsync(x => x.Id == model.Input.PatientId && x.ClinicCode == clinicCode);
        if (patient is null)
        {
            ModelState.AddModelError(nameof(model.Input.PatientId), "ไม่พบผู้ป่วยในคลินิก");
            return View("Index", await BuildDashboard(clinicCode, "ไม่พบข้อมูลผู้ป่วย", model.Input.PatientId, model));
        }

        var provider = await userManager.GetUserAsync(User);
        var providerName = string.IsNullOrWhiteSpace(provider?.FullName) ? provider?.UserName ?? string.Empty : provider.FullName;
        var providerTitle = provider?.ProfessionalTitle ?? string.Empty;
        var authenticationCode = model.Input.AuthenticationCode?.Trim() ?? string.Empty;

        await using var stream = new MemoryStream();
        await model.Input.OpdPdfFile.CopyToAsync(stream);

        var record = new TreatmentRecord
        {
            ClinicCode = clinicCode,
            PatientId = patient.Id,
            VisitDate = model.Input.VisitDate,
            ServiceRecipientId = model.Input.ServiceRecipientId?.Trim() ?? string.Empty,
            AuthenticationCode = authenticationCode,
            CitizenId = patient.CitizenId,
            Diagnosis = model.Input.Diagnosis,
            InitialDifferentialDiagnosis = model.Input.InitialDifferentialDiagnosis?.Trim() ?? string.Empty,
            ChiefComplaint = model.Input.ChiefComplaint?.Trim() ?? string.Empty,
            PresentIllness = model.Input.PresentIllness?.Trim() ?? string.Empty,
            PhysicalExam = model.Input.PhysicalExam?.Trim() ?? string.Empty,
            ProblemPhysicalExam = model.Input.ProblemPhysicalExam?.Trim() ?? string.Empty,
            TreatmentAndAdvice = model.Input.TreatmentAndAdvice?.Trim() ?? string.Empty,
            ReferralDetail = model.Input.ReferralDetail?.Trim() ?? string.Empty,
            FollowUpNone = model.Input.FollowUpNone,
            FollowUpPhone = model.Input.FollowUpPhone,
            FollowUpClinic = model.Input.FollowUpClinic,
            FollowUpClinicNote = model.Input.FollowUpClinic ? model.Input.FollowUpClinicNote : null,
            FollowUpOther = model.Input.FollowUpOther,
            FollowUpOtherNote = model.Input.FollowUpOther ? model.Input.FollowUpOtherNote : null,
            ServiceEndDateTime = model.Input.ServiceEndDateTime,
            IsChildCase = model.Input.IsChildCase,
            ChildGrowthStatus = model.Input.IsChildCase ? model.Input.ChildGrowthStatus : null,
            ChildDevelopmentStatus = model.Input.IsChildCase ? model.Input.ChildDevelopmentStatus : null,
            ChildVaccineStatus = model.Input.IsChildCase ? model.Input.ChildVaccineStatus : null,
            ChildVaccineNote = model.Input.IsChildCase ? model.Input.ChildVaccineNote : null,
            Note = model.Input.Note,
            OpdFileName = BuildOpdPdfFileName(patient.CitizenId, authenticationCode),
            OpdContentType = "application/pdf",
            OpdPdfData = stream.ToArray(),
            ProviderUserId = provider?.Id,
            ProviderName = providerName,
            ProviderProfessionalTitle = providerTitle
        };

        dbContext.TreatmentRecords.Add(record);
        await dbContext.SaveChangesAsync();

        TempData["StatusModal"] = "record-created";
        return RedirectToAction(nameof(Index), new { patientId = patient.Id });
    }

    [HttpGet]
    public async Task<IActionResult> ProviderSignature(string userId)
    {
        var clinicCode = await GetClinicCode();
        var provider = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == userId && (x.ClinicCode == clinicCode || User.IsInRole("SuperAdmin")));

        if (provider?.ProviderSignatureImageData is null || provider.ProviderSignatureImageData.Length == 0)
        {
            return NotFound();
        }

        return File(provider.ProviderSignatureImageData, provider.ProviderSignatureContentType ?? "image/png");
    }

    [HttpGet]
    public async Task<IActionResult> Preview(int id)
    {
        var clinicCode = await GetClinicCode();
        var record = await dbContext.TreatmentRecords.FirstOrDefaultAsync(x => x.Id == id && x.ClinicCode == clinicCode);
        if (record is null)
        {
            return NotFound();
        }

        return File(record.OpdPdfData, record.OpdContentType, enableRangeProcessing: true);
    }

    [HttpGet]
    public async Task<IActionResult> Report(int id)
    {
        var clinicCode = await GetClinicCode();
        var record = await dbContext.TreatmentRecords
            .AsNoTracking()
            .Include(x => x.Patient)
            .FirstOrDefaultAsync(x => x.Id == id && x.ClinicCode == clinicCode);

        if (record is null)
        {
            return NotFound();
        }

        var hasSignature = await dbContext.SignImgs
            .AsNoTracking()
            .AnyAsync(x => x.ClinicCode == clinicCode && x.CitizenId == record.Patient.CitizenId);

        var hasProviderSignature = false;
        if (!string.IsNullOrWhiteSpace(record.ProviderUserId))
        {
            hasProviderSignature = await dbContext.Users
                .AsNoTracking()
                .Where(x => x.Id == record.ProviderUserId)
                .Select(x => x.ProviderSignatureImageData != null)
                .FirstOrDefaultAsync();
        }

        var model = new MedicalRecordReportViewModel
        {
            RecordId = record.Id,
            ClinicCode = clinicCode,
            PatientName = record.Patient.FullName,
            CitizenId = record.Patient.CitizenId,
            Address = record.Patient.Address,
            PhoneNumber = record.Patient.PhoneNumber,
            Gender = record.Patient.Gender,
            AgeText = BuildAgeText(record.Patient.BirthDate, record.VisitDate),
            VisitDateText = record.VisitDate.ToString("dd/MM/yyyy HH:mm"),
            VisitDateShortText = record.VisitDate.ToString("d/M/yyyy"),
            ServiceRecipientId = record.ServiceRecipientId,
            AuthenticationCode = record.AuthenticationCode,
            Diagnosis = record.Diagnosis,
            InitialDifferentialDiagnosis = record.InitialDifferentialDiagnosis,
            ChiefComplaint = record.ChiefComplaint,
            PresentIllness = record.PresentIllness,
            PhysicalExam = record.PhysicalExam,
            ProblemPhysicalExam = record.ProblemPhysicalExam,
            TreatmentAndAdvice = record.TreatmentAndAdvice,
            ReferralDetail = record.ReferralDetail,
            FollowUpText = BuildFollowUpText(record),
            ServiceEndDateTimeText = record.ServiceEndDateTime?.ToString("dd/MM/yyyy HH:mm") ?? string.Empty,
            AuthenticationTimeText = (record.ServiceEndDateTime ?? record.VisitDate).ToString("HH:mm"),
            ProviderName = string.IsNullOrWhiteSpace(record.ProviderName) ? "-" : record.ProviderName,
            ProviderProfessionalTitle = record.ProviderProfessionalTitle,
            ProviderSignatureImageUrl = string.IsNullOrWhiteSpace(record.ProviderUserId)
                ? null
                : Url.Action(nameof(ProviderSignature), "MedicalRecords", new { userId = record.ProviderUserId }),
            HasProviderSignature = hasProviderSignature,
            BarcodeText = record.Patient.CitizenId,
            Note = record.Note,
            PreviewPdfUrl = Url.Action(nameof(Preview), "MedicalRecords", new { id = record.Id }) ?? string.Empty,
            SignatureImageUrl = Url.Action("ImageByCitizen", "Signatures", new { citizenId = record.Patient.CitizenId }),
            HasSignature = hasSignature,
            PatientPhotoUrl = Url.Action("Photo", "Patients", new { citizenId = record.Patient.CitizenId }),
            HasPatientPhoto = record.Patient.PhotoData is { Length: > 0 }
        };

        return View(model);
    }

    private async Task<string> GetClinicCode()
    {
        var user = await userManager.GetUserAsync(User);
        return user?.ClinicCode ?? "SMARTCLINIC";
    }

    private static string BuildOpdPdfFileName(string citizenId, string authenticationCode)
    {
        var cleanCitizenId = Regex.Replace(citizenId ?? string.Empty, @"\D", string.Empty);
        var cleanAuthCode = Regex.Replace(authenticationCode ?? string.Empty, @"[^A-Za-z0-9_-]", string.Empty);
        if (cleanCitizenId.Length != 13)
        {
            cleanCitizenId = string.IsNullOrWhiteSpace(cleanCitizenId) ? "UNKNOWN" : cleanCitizenId;
        }

        if (string.IsNullOrWhiteSpace(cleanAuthCode))
        {
            cleanAuthCode = "NOAUTH";
        }

        return $"{cleanCitizenId}-{cleanAuthCode}.pdf";
    }

    private static string BuildAgeText(DateOnly? birthDate, DateTime visitDate)
    {
        if (!birthDate.HasValue)
        {
            return "-";
        }

        var visit = DateOnly.FromDateTime(visitDate);
        var age = visit.Year - birthDate.Value.Year;
        if (birthDate.Value > visit.AddYears(-age))
        {
            age--;
        }

        return age >= 0 ? $"{age}" : "-";
    }

    private async Task<MedicalRecordsDashboardViewModel> BuildDashboard(
        string clinicCode,
        string statusMessage,
        int? selectedPatientId = null,
        MedicalRecordsDashboardViewModel? existingModel = null)
    {
        var patients = await dbContext.Patients
            .Where(x => x.ClinicCode == clinicCode)
            .OrderBy(x => x.FullName)
            .Select(x => new SelectListItem
            {
                Value = x.Id.ToString(),
                Text = $"{x.FullName} ({x.CitizenId})"
            })
            .ToListAsync();

        var query = dbContext.TreatmentRecords
            .AsNoTracking()
            .Include(x => x.Patient)
            .Where(x => x.ClinicCode == clinicCode);

        if (selectedPatientId.HasValue)
        {
            query = query.Where(x => x.PatientId == selectedPatientId.Value);
        }

        var records = await query
            .OrderByDescending(x => x.VisitDate)
            .ThenByDescending(x => x.CreatedAtUtc)
            .Take(30)
            .Select(x => new MedicalRecordItemViewModel
            {
                Id = x.Id,
                PatientId = x.PatientId,
                PatientName = x.Patient.FullName,
                CitizenId = x.Patient.CitizenId,
                VisitDateText = x.VisitDate.ToString("dd/MM/yyyy HH:mm"),
                ServiceRecipientId = x.ServiceRecipientId,
                AuthenticationCode = x.AuthenticationCode,
                Diagnosis = x.Diagnosis,
                FileName = x.OpdFileName,
                CreatedAtText = x.CreatedAtUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm")
            })
            .ToListAsync();

        var input = existingModel?.Input ?? new MedicalRecordCreateViewModel();
        if (selectedPatientId.HasValue && input.PatientId is null)
        {
            input.PatientId = selectedPatientId;
        }

        return new MedicalRecordsDashboardViewModel
        {
            Input = input,
            PatientOptions = patients,
            Records = records,
            StatusMessage = statusMessage
        };
    }

    private static MedicalRecordPdfImportViewModel ParseMedicalRecordPdf(string rawText)
    {
        var text = NormalizePdfText(rawText);
        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var patientName = ExtractPdfPatientName(text, lines);

        var serviceRecipientId = MatchValue(text, @"เลขประจําตัวผู้รับบริการ\s*(?<value>\d+)");
        var serviceDateText = MatchValue(text, @"วันที่รับบริการ\s*(?<value>\d{1,2}\s+[ก-๙]+\s+\d{4})");
        var visitDate = ParseThaiDateForInput(serviceDateText);
        var authenticationCode = MatchValue(text, @"Authentication\s+Code\s*(?<value>[A-Z0-9]+)");

        var chiefComplaint = ExtractInlineMedicalField(text,
            @"อาการสําคัญ\s*\(Chief\s+Complaint\)",
            @"ประวัติเจ็บ.*?\(Present\s+Illness\)|การรักษาก่อนหน้า|ยาที.?ใช้ในป.?จจุบัน|การตรวจร่างกาย");

        var presentIllness = ExtractInlineMedicalField(text,
            @"ประวัติเจ็บ.*?\(Present\s+Illness\)",
            @"การรักษาก่อนหน้า|ยาที.?ใช้ในป.?จจุบัน|การตรวจร่างกาย");

        var physicalExam = SectionBetween(text,
            @"การตรวจร่างกาย",
            @"การตรวจร่างกายตามปัญหาของผู้ป่วย");

        var problemPhysicalExam = SectionBetween(text,
            @"การตรวจร่างกายตามปัญหาของผู้ป่วย",
            @"แนบภาพถ่ายความผิดปกติ|กรณีเด็ก|การวินิจฉัยแยกอาการเบื้องต้น");

        var initialDifferentialDiagnosis = SectionBetween(text,
            @"การวินิจฉัยแยกอาการเบื้องต้น\s*\(Initial\s+differential\s+diagnosis\)",
            @"ปรับปรุงวันที่|สรุปวินิจฉัยแยกโรคเบื้องต้น|การรักษาเบื้องต้น");

        var diagnosis = SectionBetween(text,
            @"สรุปวินิจฉัยแยกโรคเบื้องต้น\s*\(Initial\s+diagnosis\)",
            @"การรักษาเบื้องต้นและการให้คําแนะนํา|การรักษาเบื้องต้น");

        if (string.IsNullOrWhiteSpace(diagnosis))
        {
            diagnosis = initialDifferentialDiagnosis;
        }

        var childSection = SectionBetween(text,
            @"กรณีเด็ก\s+อายุ\s+0\s*-\s*12\s+ปี",
            @"การวินิจฉัยแยกอาการเบื้องต้น|ปรับปรุงวันที่");

        var treatmentAndAdvice = SectionBetween(text,
            @"การรักษาเบื้องต้นและการให้คําแนะนํา",
            @"กรณีเกินศักยภาพของคลินิกได้ส่งต่อไปรับการรักษาที่|การติดตามผู้รับบริการ|เวลาสิ้นสุดการบริการ|รูปภาพเอกสาร");

        var referralDetail = SectionBetween(text,
            @"กรณีเกินศักยภาพของคลินิกได้ส่งต่อไปรับการรักษาที่",
            @"การติดตามผู้รับบริการ|เวลาสิ้นสุดการบริการ|รูปภาพเอกสาร");

        var followUpSection = SectionBetween(text,
            @"การติดตามผู้รับบริการ",
            @"เวลาสิ้นสุดการบริการ|รูปภาพเอกสาร|ปรับปรุงวันที่");

        var serviceEndDateTime = ExtractServiceEndDateTime(text);

        return new MedicalRecordPdfImportViewModel
        {
            PatientName = CleanPdfValue(patientName),
            ServiceRecipientId = serviceRecipientId,
            VisitDate = visitDate,
            AuthenticationCode = authenticationCode,
            ChiefComplaint = CleanPdfValue(chiefComplaint),
            PresentIllness = CleanPdfValue(presentIllness),
            PhysicalExam = CleanPdfValue(physicalExam),
            ProblemPhysicalExam = CleanPdfValue(problemPhysicalExam),
            TreatmentAndAdvice = CleanPdfValue(treatmentAndAdvice),
            ReferralDetail = CleanPdfValue(referralDetail),
            FollowUpNone = HasFollowUpChoice(followUpSection, "ไม่นัด"),
            FollowUpPhone = HasFollowUpChoice(followUpSection, "ติดตามทางโทรศัพท์"),
            FollowUpClinic = HasFollowUpChoice(followUpSection, "นัดตามที่คลินิก"),
            FollowUpClinicNote = ExtractFollowUpNote(followUpSection, "นัดตามที่คลินิก"),
            FollowUpOther = HasFollowUpChoice(followUpSection, "ติดตามทางช่องทางอื่น"),
            FollowUpOtherNote = ExtractFollowUpNote(followUpSection, "ติดตามทางช่องทางอื่น"),
            ServiceEndDateTime = serviceEndDateTime,
            InitialDifferentialDiagnosis = CleanPdfValue(initialDifferentialDiagnosis),
            Diagnosis = CleanPdfValue(diagnosis),
            IsChildCase = false,
            ChildGrowthStatus = ExtractChildChoice(childSection, "การเจริญเติบโต", "สมวัย", "ไม่สมวัย"),
            ChildDevelopmentStatus = ExtractChildChoice(childSection, "พัฒนาการ", "ปกติ", "ไม่ปกติ"),
            ChildVaccineStatus = ExtractChildChoice(childSection, "ประวัติการได้รับวัคซีน", "ครบ", "ไม่ครบ"),
            ChildVaccineNote = MatchValue(childSection, @"ระบุ\s*(?<value>[^\.]+)")
        };
    }

    private static string NormalizePdfText(string value)
    {
        var normalized = (value ?? string.Empty)
            .Replace('\0', ' ')
            .Replace('\r', '\n');

        normalized = normalized
            .Replace("ชื อ", "ชื่อ")
            .Replace("ที ตั ง", "ที่ตั้ง")
            .Replace("วันที รับบริการ", "วันที่รับบริการ")
            .Replace("เบื องต้น", "เบื้องต้น")
            .Replace("ป จจุบัน", "ปัจจุบัน")
            .Replace("ป ญหา", "ปัญหา")
            .Replace("ผู้ป วย", "ผู้ป่วย")
            .Replace("ป ", "ปี")
            .Replace("น าหนัก", "น้ำหนัก")
            .Replace("ครั ง", "ครั้ง")
            .Replace("ที ใช้", "ที่ใช้")
            .Replace("ที ใด", "ที่ใด");

        normalized = Regex.Replace(normalized, @"[ \t]+", " ");
        normalized = Regex.Replace(normalized, @"\n{2,}", "\n");
        return normalized.Trim();
    }

    private static string ExtractPdfPatientName(string text, string[] lines)
    {
        var nameLine = lines.FirstOrDefault(line =>
            line.Contains("สกุล", StringComparison.OrdinalIgnoreCase) &&
            line.Contains("เพศ", StringComparison.OrdinalIgnoreCase)) ?? string.Empty;

        var fullName = MatchValue(nameLine, @"สกุล\s*(?<value>.+?)\s*เพศ");
        if (!string.IsNullOrWhiteSpace(fullName))
        {
            return fullName;
        }

        foreach (var line in lines)
        {
            fullName = MatchValue(line,
                @"(?:ชื่อ\s*[-–]?\s*นามสกุล|ชื่อ\s*นามสกุล|ชื่อผู้รับบริการ|ชื่อผู้ป่วย|ผู้ป่วย)\s*(?<value>(?:นาย|นางสาว|นาง|เด็กชาย|เด็กหญิง|ด\.ช\.|ด\.ญ\.|น\.ส\.)\s*.+?)(?=\s*(?:เพศ|อายุ|เลข|วัน|HN|$))");
            if (!string.IsNullOrWhiteSpace(fullName))
            {
                return fullName;
            }
        }

        return MatchValue(text,
            @"(?:ชื่อ\s*[-–]?\s*นามสกุล|ชื่อ\s*นามสกุล|ชื่อผู้รับบริการ|ชื่อผู้ป่วย|ผู้ป่วย)\s*(?<value>(?:นาย|นางสาว|นาง|เด็กชาย|เด็กหญิง|ด\.ช\.|ด\.ญ\.|น\.ส\.)\s*.*?)(?=\s*(?:เพศ|อายุ|เลข|วัน|HN|Authentication|$))");
    }

    private static string MatchValue(string value, string pattern)
    {
        var match = Regex.Match(value ?? string.Empty, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
        return match.Success ? CleanPdfValue(match.Groups["value"].Value) : string.Empty;
    }

    private static string SectionBetween(string value, string startPattern, string endPattern)
    {
        var pattern = $"{startPattern}\\s*(?<value>.*?)(?={endPattern}|$)";
        return MatchValue(value, pattern);
    }

    private static string ExtractInlineMedicalField(string value, string startPattern, string endPattern)
    {
        var text = value ?? string.Empty;
        var pattern = $"{startPattern}\\s*(?<value>.*?)(?={endPattern}|$)";
        var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
        if (!match.Success)
        {
            return string.Empty;
        }

        var extracted = match.Groups["value"].Value;
        extracted = Regex.Replace(extracted, @"\.{5,}", " ");
        extracted = Regex.Replace(extracted, @"_{3,}", " ");
        extracted = Regex.Replace(extracted, @"\s+", " ");
        return extracted.Trim();
    }

    private static string ExtractServiceEndDateTime(string value)
    {
        var text = value ?? string.Empty;
        var dateTimePattern = @"(?<value>\d{1,2}\s+[ก-๙]+\s+\d{4}\s+\d{1,2}:\d{2})";
        var patterns = new[]
        {
            @$"เวลาสิ้นสุดการบริการ\s*{dateTimePattern}",
            @$"เวลาสินสุดการบริการ\s*{dateTimePattern}",
            @$"เวลา\s*ส.{{0,12}}?นสุด\s*การบริการ\s*{dateTimePattern}",
            @$"เวลา\s*ส.{{0,12}}?นสุด.*?{dateTimePattern}"
        };

        foreach (var pattern in patterns)
        {
            var dateText = MatchValue(text, pattern);
            var parsed = ParseThaiDateTimeForInput(dateText);
            if (!string.IsNullOrWhiteSpace(parsed))
            {
                return parsed;
            }
        }

        return string.Empty;
    }

    private static string CleanPdfValue(string value)
    {
        var cleaned = (value ?? string.Empty)
            .Replace("✓", " ")
            .Replace("\uFFFD", " ");
        cleaned = Regex.Replace(cleaned, @"\s+", " ");
        cleaned = Regex.Replace(cleaned, @"\.{5,}", string.Empty);
        return cleaned.Trim();
    }

    private static string ParseThaiDateForInput(string value)
    {
        var match = Regex.Match(value ?? string.Empty, @"(?<day>\d{1,2})\s+(?<month>[ก-๙]+)\s+(?<year>\d{4})");
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
            !int.TryParse(match.Groups["year"].Value, out var year) ||
            !monthMap.TryGetValue(match.Groups["month"].Value, out var month))
        {
            return string.Empty;
        }

        if (year > 2400)
        {
            year -= 543;
        }

        return DateTime.TryParseExact(
            $"{year:D4}-{month:D2}-{day:D2}T00:00",
            "yyyy-MM-dd'T'HH:mm",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var parsed)
            ? parsed.ToString("yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture)
            : string.Empty;
    }

    private static string ParseThaiDateTimeForInput(string value)
    {
        var match = Regex.Match(value ?? string.Empty, @"(?<day>\d{1,2})\s+(?<month>[ก-๙]+)\s+(?<year>\d{4})\s+(?<hour>\d{1,2}):(?<minute>\d{2})");
        if (!match.Success)
        {
            return string.Empty;
        }

        var datePart = ParseThaiDateForInput($"{match.Groups["day"].Value} {match.Groups["month"].Value} {match.Groups["year"].Value}");
        if (string.IsNullOrWhiteSpace(datePart) ||
            !int.TryParse(match.Groups["hour"].Value, out var hour) ||
            !int.TryParse(match.Groups["minute"].Value, out var minute))
        {
            return string.Empty;
        }

        return $"{datePart[..10]}T{hour:D2}:{minute:D2}";
    }

    private static bool HasFollowUpChoice(string followUpSection, string label)
    {
        if (string.IsNullOrWhiteSpace(followUpSection) || !followUpSection.Contains(label, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // PDF checkbox extraction may expose check marks away from the label.
        // Return false by default; users can confirm selected choices in the form.
        return false;
    }

    private static string ExtractFollowUpNote(string followUpSection, string label)
    {
        return MatchValue(followUpSection, $@"{Regex.Escape(label)}\s*ระบุ\s*(?<value>.*?)(?=ติดตามทาง|นัดตาม|ไม่นัด|$)");
    }

    private static string BuildFollowUpText(TreatmentRecord record)
    {
        var items = new List<string>();
        if (record.FollowUpNone) items.Add("ไม่นัด");
        if (record.FollowUpPhone) items.Add("ติดตามทางโทรศัพท์");
        if (record.FollowUpClinic)
        {
            items.Add(string.IsNullOrWhiteSpace(record.FollowUpClinicNote)
                ? "นัดตามที่คลินิก"
                : $"นัดตามที่คลินิก: {record.FollowUpClinicNote}");
        }
        if (record.FollowUpOther)
        {
            items.Add(string.IsNullOrWhiteSpace(record.FollowUpOtherNote)
                ? "ติดตามทางช่องทางอื่น"
                : $"ติดตามทางช่องทางอื่น: {record.FollowUpOtherNote}");
        }

        return string.Join(", ", items);
    }

    private static string ExtractChildChoice(string childSection, string label, string positive, string negative)
    {
        if (string.IsNullOrWhiteSpace(childSection) || !childSection.Contains(label, StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        // The source PDF exposes checkbox marks separately, so default to empty.
        // Users can explicitly choose the final value in the form.
        return string.Empty;
    }
}
