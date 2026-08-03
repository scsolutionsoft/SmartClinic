using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartClinic.Web.Data;
using SmartClinic.Web.Models;
using SmartClinic.Web.ViewModels;

namespace SmartClinic.Web.Controllers;

[Authorize]
public class SubscriptionsController(ApplicationDbContext db, UserManager<ApplicationUser> users, IWebHostEnvironment environment) : Controller
{
    [HttpPost, ValidateAntiForgeryToken]
    [RequestSizeLimit(12 * 1024 * 1024)]
    public async Task<IActionResult> SubmitSlip(PaymentSlipInputViewModel model)
    {
        var user = await users.GetUserAsync(User); var clinicCode = user?.ClinicCode;
        if (string.IsNullOrWhiteSpace(clinicCode)) return Forbid();
        var file = model.SlipFile;
        var extension = file is null ? string.Empty : Path.GetExtension(file.FileName).ToLowerInvariant();
        var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp", ".pdf" };
        if (file is null || file.Length == 0) ModelState.AddModelError(nameof(model.SlipFile), "กรุณาแนบสลิปโอนเงิน");
        else if (file.Length > 10 * 1024 * 1024 || !allowed.Contains(extension)) ModelState.AddModelError(nameof(model.SlipFile), "สลิปต้องเป็น JPG, PNG, WebP หรือ PDF ขนาดไม่เกิน 10 MB");
        if (!model.RequestUnlimited && model.RequestedCredits < 1) ModelState.AddModelError(nameof(model.RequestedCredits), "กรุณาระบุจำนวนเครดิต");
        if (!ModelState.IsValid) { TempData["SlipError"] = string.Join(" ", ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage)); return RedirectToAction("Index", "MedicalRecords"); }
        await using var stream = new MemoryStream(); await file!.CopyToAsync(stream);
        db.PaymentSlips.Add(new PaymentSlip
        {
            ClinicCode = clinicCode, ContactName = model.ContactName.Trim(), PhoneNumber = model.PhoneNumber.Trim(), Amount = model.Amount,
            RequestedCredits = model.RequestUnlimited ? 0 : model.RequestedCredits, RequestUnlimited = model.RequestUnlimited,
            TransferDateTime = model.TransferDateTime, TransferBank = model.TransferBank.Trim(), TransferReference = model.TransferReference?.Trim() ?? string.Empty,
            FileName = Path.GetFileName(file.FileName), ContentType = extension == ".pdf" ? "application/pdf" : file.ContentType,
            SlipData = stream.ToArray(), ClinicNote = model.Note?.Trim() ?? string.Empty
        });
        await db.SaveChangesAsync(); TempData["SlipSubmitted"] = "true"; return RedirectToAction("Index", "MedicalRecords");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SendMessage(AdminMessageInputViewModel model)
    {
        var user = await users.GetUserAsync(User); var clinicCode = user?.ClinicCode;
        if (string.IsNullOrWhiteSpace(clinicCode)) return Forbid();
        if (!ModelState.IsValid) { TempData["MessageError"] = "กรุณากรอกหัวข้อ ข้อความ และข้อมูลติดต่อให้ครบ"; return RedirectToAction("Index", "MedicalRecords"); }
        db.AdminMessages.Add(new AdminMessage { ClinicCode = clinicCode, Subject = model.Subject.Trim(), Message = model.Message.Trim(), ContactName = model.ContactName.Trim(), PhoneNumber = model.PhoneNumber.Trim() });
        await db.SaveChangesAsync(); TempData["MessageSent"] = "true"; return RedirectToAction("Index", "MedicalRecords");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestCredit(CreditRequestInputViewModel model)
    {
        var user = await users.GetUserAsync(User);
        var clinicCode = user?.ClinicCode;
        if (string.IsNullOrWhiteSpace(clinicCode)) return Forbid();
        if (!model.RequestUnlimited && !model.RequestedCredits.HasValue)
            ModelState.AddModelError(nameof(model.RequestedCredits), "กรุณาระบุจำนวนเครดิตหรือเลือก Unlimited");
        if (!ModelState.IsValid)
        {
            TempData["QuotaError"] = "กรุณาตรวจสอบชื่อ เบอร์โทร และจำนวนเครดิต";
            return RedirectToAction("Index", "MedicalRecords");
        }

        var hasPending = await db.CreditRequests.AnyAsync(x => x.ClinicCode == clinicCode && x.Status == "Pending");
        if (!hasPending)
        {
            db.CreditRequests.Add(new CreditRequest
            {
                ClinicCode = clinicCode,
                ContactName = model.ContactName.Trim(),
                PhoneNumber = model.PhoneNumber.Trim(),
                RequestedCredits = model.RequestUnlimited ? null : model.RequestedCredits,
                RequestUnlimited = model.RequestUnlimited,
                Note = model.Note?.Trim() ?? string.Empty
            });
            await db.SaveChangesAsync();
        }
        TempData["QuotaRequestSent"] = "true";
        return RedirectToAction("Index", "MedicalRecords");
    }

    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> Admin()
    {
        var counts = await db.TreatmentRecords.AsNoTracking().GroupBy(x => x.ClinicCode)
            .Select(x => new { ClinicCode = x.Key, Count = x.Count() }).ToDictionaryAsync(x => x.ClinicCode, x => x.Count);
        var clinics = await db.Clinics.AsNoTracking().OrderBy(x => x.ClinicName).ToListAsync();
        return View(new SubscriptionAdminViewModel
        {
            Clinics = clinics.Select(x => new ClinicQuotaItemViewModel
            {
                ClinicCode = x.ClinicCode, ClinicName = x.ClinicName, PatientLimit = x.OpdRecordLimit,
                IsUnlimited = x.HasUnlimitedOpdRecords, UsedPatients = counts.GetValueOrDefault(x.ClinicCode)
            }).ToList(),
            Requests = await db.CreditRequests.AsNoTracking().OrderBy(x => x.Status == "Pending" ? 0 : 1).ThenByDescending(x => x.CreatedAtUtc).Take(100).ToListAsync(),
            Announcements = await db.PublicAnnouncements.AsNoTracking().OrderBy(x => x.DisplayOrder).ThenByDescending(x => x.PublishedAtUtc).ToListAsync()
            ,PromotionalMedia = await db.PromotionalMedia.AsNoTracking().OrderBy(x => x.Placement).ThenBy(x => x.DisplayOrder).ToListAsync()
        });
    }

    [HttpPost, Authorize(Roles = "SuperAdmin"), ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateQuota(QuotaUpdateViewModel model)
    {
        if (!ModelState.IsValid) return RedirectToAction(nameof(Admin));
        var clinic = await db.Clinics.FirstOrDefaultAsync(x => x.ClinicCode == model.ClinicCode);
        var user = await users.GetUserAsync(User);
        if (clinic is null || user is null) return NotFound();
        var oldLimit = clinic.OpdRecordLimit;
        var oldUnlimited = clinic.HasUnlimitedOpdRecords;
        clinic.OpdRecordLimit = model.PatientLimit;
        clinic.HasUnlimitedOpdRecords = model.IsUnlimited;
        db.QuotaTransactions.Add(new QuotaTransaction
        {
            ClinicCode = clinic.ClinicCode, PreviousLimit = oldLimit, Adjustment = model.PatientLimit - oldLimit,
            NewLimit = model.PatientLimit, PreviousUnlimited = oldUnlimited, NewUnlimited = model.IsUnlimited,
            Reason = model.Reason.Trim(), CreatedByUserId = user.Id
        });
        await db.SaveChangesAsync();
        TempData["AdminStatus"] = "quota-updated";
        return RedirectToAction(nameof(Admin));
    }

    [HttpPost, Authorize(Roles = "SuperAdmin"), ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateRequest(int id, string status, string? adminNote)
    {
        var allowed = new[] { "Pending", "Contacting", "Completed", "Cancelled" };
        var request = await db.CreditRequests.FindAsync(id);
        var user = await users.GetUserAsync(User);
        if (request is null || user is null) return NotFound();
        request.Status = allowed.Contains(status) ? status : "Pending";
        request.AdminNote = adminNote?.Trim() ?? string.Empty;
        request.UpdatedAtUtc = DateTime.UtcNow;
        request.CompletedByUserId = request.Status is "Completed" or "Cancelled" ? user.Id : null;
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Admin));
    }

    [HttpPost, Authorize(Roles = "SuperAdmin"), ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveAnnouncement(AnnouncementInputViewModel model)
    {
        if (!ModelState.IsValid) return RedirectToAction(nameof(Admin));
        var item = model.Id.HasValue ? await db.PublicAnnouncements.FindAsync(model.Id.Value) : null;
        if (item is null) { item = new PublicAnnouncement(); db.PublicAnnouncements.Add(item); }
        item.Title = model.Title.Trim(); item.Summary = model.Summary.Trim(); item.BadgeText = model.BadgeText.Trim();
        item.LinkUrl = model.LinkUrl?.Trim() ?? string.Empty; item.DisplayOrder = model.DisplayOrder; item.IsPublished = model.IsPublished;
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Admin));
    }

    [HttpPost, Authorize(Roles = "SuperAdmin"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAnnouncement(int id)
    {
        var item = await db.PublicAnnouncements.FindAsync(id);
        if (item is not null) { db.PublicAnnouncements.Remove(item); await db.SaveChangesAsync(); }
        return RedirectToAction(nameof(Admin));
    }

    [HttpPost, Authorize(Roles = "SuperAdmin"), ValidateAntiForgeryToken]
    [RequestSizeLimit(90 * 1024 * 1024)]
    public async Task<IActionResult> SaveMedia(PromotionalMediaInputViewModel model)
    {
        var existing = model.Id.HasValue ? await db.PromotionalMedia.FindAsync(model.Id.Value) : null;
        if (model.Id.HasValue && existing is null) return NotFound();
        var allowedTypes = new[] { "Video", "YouTube" };
        var allowedPlacements = new[] { "Hero", "Feature", "Board" };
        if (!allowedTypes.Contains(model.MediaType) || !allowedPlacements.Contains(model.Placement))
            ModelState.AddModelError(string.Empty, "ประเภทหรือตำแหน่งสื่อไม่ถูกต้อง");

        string mediaUrl = string.Empty;
        if (model.MediaType == "YouTube")
        {
            mediaUrl = NormalizeYouTubeUrl(model.YouTubeUrl);
            if (string.IsNullOrWhiteSpace(mediaUrl)) ModelState.AddModelError(nameof(model.YouTubeUrl), "กรุณาระบุลิงก์ YouTube ที่ถูกต้อง");
        }
        else
        {
            if ((model.VideoFile is null || model.VideoFile.Length == 0) &&
                (existing is null || existing.MediaType != "Video" || string.IsNullOrWhiteSpace(existing.MediaUrl)))
                ModelState.AddModelError(nameof(model.VideoFile), "กรุณาเลือกไฟล์วิดีโอ");
            else if (model.VideoFile is { Length: > 80 * 1024 * 1024 }) ModelState.AddModelError(nameof(model.VideoFile), "วิดีโอต้องมีขนาดไม่เกิน 80 MB");
            else if (model.VideoFile is { Length: > 0 } videoFile &&
                    (!new[] { ".mp4", ".webm" }.Contains(Path.GetExtension(videoFile.FileName).ToLowerInvariant()) ||
                     !new[] { "video/mp4", "video/webm", "application/octet-stream" }.Contains(videoFile.ContentType.ToLowerInvariant())))
                ModelState.AddModelError(nameof(model.VideoFile), "รองรับเฉพาะ MP4 และ WebM");
        }
        if (model.PosterFile is { Length: > 5 * 1024 * 1024 }) ModelState.AddModelError(nameof(model.PosterFile), "ภาพ Poster ต้องไม่เกิน 5 MB");
        if (model.PosterFile is { Length: > 0 } poster &&
            (!new[] { ".jpg", ".jpeg", ".png", ".webp" }.Contains(Path.GetExtension(poster.FileName).ToLowerInvariant()) ||
             !new[] { "image/jpeg", "image/png", "image/webp", "application/octet-stream" }.Contains(poster.ContentType.ToLowerInvariant())))
            ModelState.AddModelError(nameof(model.PosterFile), "รองรับ Poster เฉพาะ JPG, PNG และ WebP");
        if (!ModelState.IsValid) { TempData["AdminMediaError"] = string.Join(" ", ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage)); return RedirectToAction(nameof(Admin)); }

        var directory = Path.Combine(environment.WebRootPath, "uploads", "promotional");
        Directory.CreateDirectory(directory);
        if (model.MediaType == "Video")
        {
            if (model.VideoFile is { Length: > 0 })
            {
                mediaUrl = await SaveUpload(model.VideoFile, directory, new[] { ".mp4", ".webm" });
                if (existing is not null && existing.MediaUrl != mediaUrl) DeleteLocalMedia(existing.MediaUrl);
            }
            else mediaUrl = existing!.MediaUrl;
        }
        var posterUrl = model.PosterFile is { Length: > 0 }
            ? await SaveUpload(model.PosterFile, directory, new[] { ".jpg", ".jpeg", ".png", ".webp" })
            : existing?.PosterUrl ?? string.Empty;
        if (model.PosterFile is { Length: > 0 } && existing is not null && existing.PosterUrl != posterUrl) DeleteLocalMedia(existing.PosterUrl);
        if (existing is not null && existing.MediaType == "Video" && model.MediaType == "YouTube") DeleteLocalMedia(existing.MediaUrl);

        var item = existing ?? new PromotionalMedia();
        if (existing is null) db.PromotionalMedia.Add(item);
        item.Title = model.Title.Trim(); item.Description = model.Description?.Trim() ?? string.Empty;
        item.MediaType = model.MediaType; item.Placement = model.Placement; item.MediaUrl = mediaUrl; item.PosterUrl = posterUrl;
        item.AutoPlay = model.Placement == "Hero" && model.AutoPlay; item.Loop = model.Loop;
        item.IsPublished = model.IsPublished; item.DisplayOrder = model.DisplayOrder;
        item.StartsAtUtc = model.StartsAt?.ToUniversalTime(); item.EndsAtUtc = model.EndsAt?.ToUniversalTime();
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Admin));
    }

    [HttpPost, Authorize(Roles = "SuperAdmin"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteMedia(int id)
    {
        var item = await db.PromotionalMedia.FindAsync(id);
        if (item is not null)
        {
            DeleteLocalMedia(item.MediaUrl); DeleteLocalMedia(item.PosterUrl);
            db.PromotionalMedia.Remove(item); await db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Admin));
    }

    private async Task<string> SaveUpload(IFormFile file, string directory, IReadOnlyCollection<string> allowedExtensions)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension)) throw new InvalidOperationException("Unsupported file type");
        var fileName = $"{Guid.NewGuid():N}{extension}";
        await using var output = System.IO.File.Create(Path.Combine(directory, fileName));
        await file.CopyToAsync(output);
        return $"/uploads/promotional/{fileName}";
    }

    private void DeleteLocalMedia(string url)
    {
        if (!url.StartsWith("/uploads/promotional/", StringComparison.OrdinalIgnoreCase)) return;
        var fileName = Path.GetFileName(url);
        var path = Path.Combine(environment.WebRootPath, "uploads", "promotional", fileName);
        if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
    }

    private static string NormalizeYouTubeUrl(string? url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return string.Empty;
        string? id = null;
        if (uri.Host.EndsWith("youtu.be", StringComparison.OrdinalIgnoreCase)) id = uri.AbsolutePath.Trim('/').Split('/')[0];
        else if (uri.Host.EndsWith("youtube.com", StringComparison.OrdinalIgnoreCase))
        {
            if (uri.AbsolutePath.StartsWith("/embed/")) id = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries).ElementAtOrDefault(1);
            else id = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(uri.Query).GetValueOrDefault("v").FirstOrDefault();
        }
        return !string.IsNullOrWhiteSpace(id) && System.Text.RegularExpressions.Regex.IsMatch(id, "^[A-Za-z0-9_-]{6,20}$")
            ? $"https://www.youtube-nocookie.com/embed/{id}" : string.Empty;
    }
}
