using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SmartClinic.Web.Data;
using SmartClinic.Web.Models;
using SmartClinic.Web.Services;
using SmartClinic.Web.ViewModels;

namespace SmartClinic.Web.Controllers;

[Authorize(Roles = "SuperAdmin,AdminClinic,Nurse")]
public class WoundCareController : Controller
{
    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase) { ".png", ".jpg", ".jpeg", ".webp" };
    private const long MaxImageSize = 5 * 1024 * 1024;
    private readonly ApplicationDbContext db;
    private readonly UserManager<ApplicationUser> users;
    private readonly IWoundCareOcrService ocr;
    public WoundCareController(ApplicationDbContext db, UserManager<ApplicationUser> users, IWoundCareOcrService ocr)
        => (this.db, this.users, this.ocr) = (db, users, ocr);

    [HttpGet]
    public async Task<IActionResult> Index() => View(await Dashboard(new()));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AnalyzeDocument(IFormFile sourceDocument)
    {
        if (sourceDocument is null || sourceDocument.Length == 0) return BadRequest(new { success = false, error = "กรุณาเลือกไฟล์ภาพหรือ PDF" });
        try { return Ok(new { success = true, data = await ocr.AnalyzeAsync(sourceDocument) }); }
        catch (Exception ex) { return BadRequest(new { success = false, error = $"OCR ไม่สำเร็จ: {ex.Message}" }); }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(WoundCareDashboardViewModel model)
    {
        var clinic = await ClinicCode();
        var input = model.Input;
        var patient = await db.Patients.FirstOrDefaultAsync(x => x.Id == input.PatientId && x.ClinicCode == clinic);
        if (patient is null) ModelState.AddModelError("Input.PatientId", "ไม่พบผู้ป่วยในคลินิก");
        if (input.WoundPhotos.Count > 4) ModelState.AddModelError("Input.WoundPhotos", "อัปโหลดรูปแผลได้ไม่เกิน 4 รูป");
        foreach (var photo in input.WoundPhotos.Where(x => x.Length > 0))
            if (photo.Length > MaxImageSize || !ImageExtensions.Contains(Path.GetExtension(photo.FileName)))
                ModelState.AddModelError("Input.WoundPhotos", $"ไฟล์ {photo.FileName} ไม่ถูกต้องหรือเกิน 5 MB");
        if (!string.IsNullOrWhiteSpace(input.AuthenticationCode) &&
            await db.WoundCareRecords.AnyAsync(x => x.ClinicCode == clinic && x.AuthenticationCode == input.AuthenticationCode.Trim() && x.Id != input.Id))
            ModelState.AddModelError("Input.AuthenticationCode", "Authentication Code นี้ถูกใช้แล้ว");
        if (!ModelState.IsValid) return View("Index", await Dashboard(input));

        var provider = await users.GetUserAsync(User);
        WoundCareRecord record;
        if (input.Id.HasValue)
        {
            record = await db.WoundCareRecords.Include(x => x.Photos).FirstOrDefaultAsync(x => x.Id == input.Id && x.ClinicCode == clinic) ?? throw new InvalidOperationException();
        }
        else
        {
            record = new WoundCareRecord { ClinicCode = clinic };
            db.WoundCareRecords.Add(record);
        }
        Map(record, input, patient!, provider);
        if (input.SourceDocument is { Length: > 0 } source)
        {
            await using var stream = new MemoryStream(); await source.CopyToAsync(stream);
            record.OriginalFileName = Path.GetFileName(source.FileName); record.OriginalContentType = source.ContentType; record.OriginalDocumentData = stream.ToArray();
            try { record.OcrRawText = (await ocr.AnalyzeAsync(source)).RawText; } catch { }
        }
        if (input.WoundPhotos.Any(x => x.Length > 0))
        {
            db.WoundCarePhotos.RemoveRange(record.Photos);
            record.Photos.Clear();
            byte sequence = 1;
            foreach (var file in input.WoundPhotos.Where(x => x.Length > 0))
            {
                await using var stream = new MemoryStream(); await file.CopyToAsync(stream);
                record.Photos.Add(new WoundCarePhoto { SequenceNo = sequence++, FileName = Path.GetFileName(file.FileName), ContentType = file.ContentType, ImageData = stream.ToArray() });
            }
        }
        await db.SaveChangesAsync();
        TempData["StatusModal"] = "wound-saved";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var clinic = await ClinicCode();
        var record = await db.WoundCareRecords.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.ClinicCode == clinic);
        return record is null ? NotFound() : View("Index", await Dashboard(ToInput(record)));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var clinic = await ClinicCode();
        var record = await db.WoundCareRecords.FirstOrDefaultAsync(x => x.Id == id && x.ClinicCode == clinic);
        if (record is null) return NotFound();
        db.Remove(record); await db.SaveChangesAsync(); TempData["StatusModal"] = "wound-deleted";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Report(int id)
    {
        var clinic = await ClinicCode();
        var record = await db.WoundCareRecords.AsNoTracking().Include(x => x.Patient).Include(x => x.Photos).FirstOrDefaultAsync(x => x.Id == id && x.ClinicCode == clinic);
        return record is null ? NotFound() : View(record);
    }

    [HttpGet]
    public async Task<IActionResult> Photo(int id)
    {
        var clinic = await ClinicCode();
        var photo = await db.WoundCarePhotos.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.WoundCareRecord.ClinicCode == clinic);
        return photo is null ? NotFound() : File(photo.ImageData, photo.ContentType);
    }

    private async Task<string> ClinicCode() => (await users.GetUserAsync(User))?.ClinicCode ?? "SMARTCLINIC";
    private async Task<WoundCareDashboardViewModel> Dashboard(WoundCareInputViewModel input)
    {
        var clinic = await ClinicCode();
        return new() {
            Input = input,
            Patients = await db.Patients.AsNoTracking().Where(x => x.ClinicCode == clinic).OrderBy(x => x.FullName).Select(x => new SelectListItem($"{x.FullName} ({x.CitizenId})", x.Id.ToString())).ToListAsync(),
            Records = await db.WoundCareRecords.AsNoTracking().Include(x => x.Patient).Include(x => x.Photos).Where(x => x.ClinicCode == clinic).OrderByDescending(x => x.VisitDate).Take(100).ToListAsync()
        };
    }
    private static void Map(WoundCareRecord r, WoundCareInputViewModel x, Patient p, ApplicationUser? u)
    {
        r.PatientId=p.Id;r.CitizenId=p.CitizenId;r.VisitDate=x.VisitDate;r.ServiceRecipientId=x.ServiceRecipientId?.Trim()??"";r.AuthenticationCode=x.AuthenticationCode?.Trim()??"";
        r.WoundCause=x.WoundCause.Trim();r.WoundLocation=x.WoundLocation.Trim();r.IsBruiseOrSutured=x.IsBruiseOrSutured;r.IsOpenInfected=x.IsOpenInfected;r.IsLargeOpenWound=x.IsLargeOpenWound;
        r.WoundSize=x.WoundSize?.Trim()??"";r.DressingSequence=x.DressingSequence;r.WoundProgress=x.WoundProgress??"";r.WoundProgressNote=x.WoundProgressNote??"";
        r.TemperatureCelsius=x.TemperatureCelsius;r.PulseRate=x.PulseRate;r.RespiratoryRate=x.RespiratoryRate;r.SystolicPressure=x.SystolicPressure;r.DiastolicPressure=x.DiastolicPressure;
        r.PostDressingResult=x.PostDressingResult??"";r.FollowUpNone=x.FollowUpNone;r.FollowUpPhone=x.FollowUpPhone;r.FollowUpClinic=x.FollowUpClinic;r.FollowUpNote=x.FollowUpNote??"";
        r.FollowUpOther=x.FollowUpOther;r.WoundAssessment=x.WoundAssessment??"";r.PatientAdvice=x.PatientAdvice??"";r.ProviderUserId=u?.Id??"";r.ProviderName=u?.FullName??u?.UserName??"";r.ProviderTitle=u?.ProfessionalTitle??"";r.UpdatedAtUtc=DateTime.UtcNow;
    }
    private static WoundCareInputViewModel ToInput(WoundCareRecord r) => new() { Id=r.Id,PatientId=r.PatientId,VisitDate=r.VisitDate,ServiceRecipientId=r.ServiceRecipientId,AuthenticationCode=r.AuthenticationCode,WoundCause=r.WoundCause,WoundLocation=r.WoundLocation,IsBruiseOrSutured=r.IsBruiseOrSutured,IsOpenInfected=r.IsOpenInfected,IsLargeOpenWound=r.IsLargeOpenWound,WoundSize=r.WoundSize,DressingSequence=r.DressingSequence,WoundProgress=r.WoundProgress,WoundProgressNote=r.WoundProgressNote,TemperatureCelsius=r.TemperatureCelsius,PulseRate=r.PulseRate,RespiratoryRate=r.RespiratoryRate,SystolicPressure=r.SystolicPressure,DiastolicPressure=r.DiastolicPressure,PostDressingResult=r.PostDressingResult,FollowUpNone=r.FollowUpNone,FollowUpPhone=r.FollowUpPhone,FollowUpClinic=r.FollowUpClinic,FollowUpNote=r.FollowUpNote,FollowUpOther=r.FollowUpOther,WoundAssessment=r.WoundAssessment,PatientAdvice=r.PatientAdvice};
}
