using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartClinic.Web.Data;
using SmartClinic.Web.Models;
using SmartClinic.Web.ViewModels;

namespace SmartClinic.Web.Controllers;

[Authorize(Roles = "SuperAdmin,AdminClinic,Nurse")]
public class SignaturesController : Controller
{
    private const long MaxSignatureFileSize = 5 * 1024 * 1024;
    private static readonly Regex CitizenIdRegex = new("^\\d{13}$", RegexOptions.Compiled);
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png",
        ".jpg",
        ".jpeg",
        ".webp"
    };

    private readonly ApplicationDbContext dbContext;
    private readonly UserManager<ApplicationUser> userManager;

    public SignaturesController(ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager)
    {
        this.dbContext = dbContext;
        this.userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? search, int page = 1)
    {
        var clinicCode = await GetClinicCode();
        var model = await BuildDashboard(
            clinicCode,
            "รองรับอัปโหลดลายเซ็นรายคนและหลายไฟล์",
            page: page,
            searchTerm: search);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadSingle(SignaturesDashboardViewModel model)
    {
        var clinicCode = await GetClinicCode();

        if (!CitizenIdRegex.IsMatch(model.Input.CitizenId))
        {
            ModelState.AddModelError(nameof(model.Input.CitizenId), "เลขบัตรประชาชนต้องเป็นตัวเลข 13 หลัก");
            return View("Index", await BuildDashboard(clinicCode, "รูปแบบเลขบัตรไม่ถูกต้อง", model));
        }

        if (model.Input.SignatureFile is null || model.Input.SignatureFile.Length == 0)
        {
            ModelState.AddModelError(nameof(model.Input.SignatureFile), "กรุณาเลือกไฟล์ลายเซ็น");
            return View("Index", await BuildDashboard(clinicCode, "ไม่พบไฟล์ลายเซ็น", model));
        }

        var extension = Path.GetExtension(model.Input.SignatureFile.FileName);
        if (!AllowedExtensions.Contains(extension))
        {
            ModelState.AddModelError(nameof(model.Input.SignatureFile), "รองรับเฉพาะไฟล์ .png .jpg .jpeg .webp");
            return View("Index", await BuildDashboard(clinicCode, "ชนิดไฟล์ไม่ถูกต้อง", model));
        }

        var fileName = $"{model.Input.CitizenId}{extension.ToLowerInvariant()}";
        await SaveSignature(clinicCode, model.Input.CitizenId, fileName, model.Input.SignatureFile);

        TempData["StatusModal"] = "signature-single-created";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadBatch(SignaturesDashboardViewModel model)
    {
        var clinicCode = await GetClinicCode();
        if (model.Input.BatchFiles.Count == 0)
        {
            ModelState.AddModelError(nameof(model.Input.BatchFiles), "กรุณาเลือกไฟล์อย่างน้อย 1 ไฟล์");
            return View("Index", await BuildDashboard(clinicCode, "ไม่พบไฟล์ที่ต้องการอัปโหลด", model));
        }

        var successCount = 0;
        var invalidFiles = new List<string>();

        foreach (var file in model.Input.BatchFiles)
        {
            if (file is null || file.Length == 0)
            {
                continue;
            }

            var extension = Path.GetExtension(file.FileName);
            var baseName = Path.GetFileNameWithoutExtension(file.FileName);
            if (!AllowedExtensions.Contains(extension) || !CitizenIdRegex.IsMatch(baseName))
            {
                invalidFiles.Add(file.FileName);
                continue;
            }

            await SaveSignature(clinicCode, baseName, $"{baseName}{extension.ToLowerInvariant()}", file);
            successCount++;
        }

        if (invalidFiles.Count > 0)
        {
            ModelState.AddModelError(string.Empty, $"ไฟล์ไม่ถูกต้อง: {string.Join(", ", invalidFiles)}");
            return View("Index", await BuildDashboard(clinicCode, $"อัปโหลดสำเร็จ {successCount} ไฟล์ และมีบางไฟล์ไม่ผ่านเงื่อนไข", model));
        }

        TempData["StatusModal"] = "signature-batch-created";
        TempData["BatchCount"] = successCount;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> ImageByCitizen(string citizenId)
    {
        var clinicCode = await GetClinicCode();
        var signature = await dbContext.SignImgs
            .AsNoTracking()
            .Where(x => x.ClinicCode == clinicCode && x.CitizenId == citizenId)
            .OrderByDescending(x => x.UploadedAtUtc)
            .FirstOrDefaultAsync();

        if (signature is null)
        {
            return NotFound();
        }

        return File(signature.ImageData, signature.ContentType);
    }

    [HttpGet]
    public async Task<IActionResult> Image(int id)
    {
        var clinicCode = await GetClinicCode();
        var signature = await dbContext.SignImgs
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.ClinicCode == clinicCode);

        return signature is null
            ? NotFound()
            : File(signature.ImageData, signature.ContentType);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Replace(int id, IFormFile signatureFile)
    {
        var clinicCode = await GetClinicCode();
        var signature = await dbContext.SignImgs
            .FirstOrDefaultAsync(x => x.Id == id && x.ClinicCode == clinicCode);
        if (signature is null)
        {
            return NotFound();
        }

        var validationError = ValidateSignatureFile(signatureFile);
        if (validationError is not null)
        {
            TempData["SignatureError"] = validationError;
            TempData["StatusModal"] = "signature-error";
            return RedirectToAction(nameof(Index));
        }

        await using var stream = new MemoryStream();
        await signatureFile.CopyToAsync(stream);
        var extension = Path.GetExtension(signatureFile.FileName).ToLowerInvariant();
        signature.FileName = $"{signature.CitizenId}{extension}";
        signature.ContentType = GetSafeContentType(extension);
        signature.ImageData = stream.ToArray();
        signature.UploadedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();

        TempData["StatusModal"] = "signature-replaced";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Capture(int? id, string citizenId, string signatureData)
    {
        var clinicCode = await GetClinicCode();
        citizenId = Regex.Replace(citizenId ?? string.Empty, @"\D", string.Empty);
        if (!CitizenIdRegex.IsMatch(citizenId))
        {
            TempData["SignatureError"] = "เลขบัตรประชาชนต้องเป็นตัวเลข 13 หลัก";
            TempData["StatusModal"] = "signature-error";
            return RedirectToAction(nameof(Index));
        }

        const string pngPrefix = "data:image/png;base64,";
        if (string.IsNullOrWhiteSpace(signatureData) ||
            !signatureData.StartsWith(pngPrefix, StringComparison.Ordinal))
        {
            TempData["SignatureError"] = "ไม่พบข้อมูลลายเซ็น PNG";
            TempData["StatusModal"] = "signature-error";
            return RedirectToAction(nameof(Index));
        }

        byte[] imageData;
        try
        {
            imageData = Convert.FromBase64String(signatureData[pngPrefix.Length..]);
        }
        catch (FormatException)
        {
            TempData["SignatureError"] = "ข้อมูลภาพลายเซ็นไม่ถูกต้อง";
            TempData["StatusModal"] = "signature-error";
            return RedirectToAction(nameof(Index));
        }

        var pngHeader = new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 };
        if (imageData.Length == 0 ||
            imageData.Length > MaxSignatureFileSize ||
            imageData.Length < pngHeader.Length ||
            !imageData.AsSpan(0, pngHeader.Length).SequenceEqual(pngHeader))
        {
            TempData["SignatureError"] = "ไฟล์ลายเซ็น PNG ไม่ถูกต้องหรือมีขนาดเกิน 5 MB";
            TempData["StatusModal"] = "signature-error";
            return RedirectToAction(nameof(Index));
        }

        SignImg? signature = null;
        if (id.HasValue)
        {
            signature = await dbContext.SignImgs
                .FirstOrDefaultAsync(x => x.Id == id.Value && x.ClinicCode == clinicCode);
            if (signature is null)
            {
                return NotFound();
            }

            citizenId = signature.CitizenId;
        }

        if (signature is null)
        {
            signature = new SignImg
            {
                ClinicCode = clinicCode,
                CitizenId = citizenId
            };
            dbContext.SignImgs.Add(signature);
        }

        signature.FileName = $"{citizenId}.png";
        signature.ContentType = "image/png";
        signature.ImageData = imageData;
        signature.UploadedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();

        TempData["StatusModal"] = id.HasValue ? "signature-replaced" : "signature-captured";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var clinicCode = await GetClinicCode();
        var signature = await dbContext.SignImgs
            .FirstOrDefaultAsync(x => x.Id == id && x.ClinicCode == clinicCode);
        if (signature is null)
        {
            return NotFound();
        }

        dbContext.SignImgs.Remove(signature);
        await dbContext.SaveChangesAsync();
        TempData["StatusModal"] = "signature-deleted";
        return RedirectToAction(nameof(Index));
    }

    private async Task SaveSignature(string clinicCode, string citizenId, string fileName, IFormFile file)
    {
        await using var stream = new MemoryStream();
        await file.CopyToAsync(stream);

        var signature = new SignImg
        {
            ClinicCode = clinicCode,
            CitizenId = citizenId,
            FileName = fileName,
            ContentType = string.IsNullOrWhiteSpace(file.ContentType) ? "image/png" : file.ContentType,
            ImageData = stream.ToArray(),
            UploadedAtUtc = DateTime.UtcNow
        };

        dbContext.SignImgs.Add(signature);
        await dbContext.SaveChangesAsync();
    }

    private static string? ValidateSignatureFile(IFormFile? file)
    {
        if (file is null || file.Length == 0)
        {
            return "กรุณาเลือกไฟล์ลายเซ็น";
        }

        if (file.Length > MaxSignatureFileSize)
        {
            return "ไฟล์ลายเซ็นต้องมีขนาดไม่เกิน 5 MB";
        }

        var extension = Path.GetExtension(file.FileName);
        return AllowedExtensions.Contains(extension)
            ? null
            : "รองรับเฉพาะไฟล์ .png .jpg .jpeg .webp";
    }

    private static string GetSafeContentType(string extension) => extension switch
    {
        ".jpg" or ".jpeg" => "image/jpeg",
        ".webp" => "image/webp",
        _ => "image/png"
    };

    private async Task<string> GetClinicCode()
    {
        var user = await userManager.GetUserAsync(User);
        return user?.ClinicCode ?? "SMARTCLINIC";
    }

    private async Task<SignaturesDashboardViewModel> BuildDashboard(
        string clinicCode,
        string statusMessage,
        SignaturesDashboardViewModel? existingModel = null,
        int page = 1,
        string? searchTerm = null)
    {
        var normalizedSearch = searchTerm?.Trim();
        var signaturesQuery = dbContext.SignImgs
            .AsNoTracking()
            .Where(x => x.ClinicCode == clinicCode);

        if (!string.IsNullOrWhiteSpace(normalizedSearch))
        {
            signaturesQuery = signaturesQuery.Where(signature =>
                signature.CitizenId.Contains(normalizedSearch) ||
                dbContext.Patients.Any(patient =>
                    patient.ClinicCode == clinicCode &&
                    patient.CitizenId == signature.CitizenId &&
                    patient.FullName.Contains(normalizedSearch)));
        }

        var totalItems = await signaturesQuery.CountAsync();

        var totalPages = Math.Max(1, (int)Math.Ceiling(totalItems / (double)SignaturesDashboardViewModel.PageSize));
        var requestedPage = existingModel?.CurrentPage > 0 ? existingModel.CurrentPage : page;
        var currentPage = Math.Clamp(requestedPage, 1, totalPages);
        var skip = (currentPage - 1) * SignaturesDashboardViewModel.PageSize;

        var signatures = await signaturesQuery
            .OrderByDescending(x => x.UploadedAtUtc)
            .Skip(skip)
            .Take(SignaturesDashboardViewModel.PageSize)
            .Select(x => new SignatureItemViewModel
            {
                Id = x.Id,
                CitizenId = x.CitizenId,
                FileName = x.FileName,
                UploadedAtText = x.UploadedAtUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm")
            })
            .ToListAsync();

        var citizenIds = signatures
            .Select(x => x.CitizenId)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .ToList();

        var patientMap = await dbContext.Patients
            .AsNoTracking()
            .Where(x => x.ClinicCode == clinicCode && citizenIds.Contains(x.CitizenId))
            .Select(x => new
            {
                x.CitizenId,
                x.FullName,
                HasPhoto = x.PhotoData != null && x.PhotoData.Length > 0
            })
            .ToDictionaryAsync(x => x.CitizenId, x => x);

        foreach (var item in signatures)
        {
            if (patientMap.TryGetValue(item.CitizenId, out var patient))
            {
                if (!string.IsNullOrWhiteSpace(patient.FullName))
                {
                    item.FullName = patient.FullName;
                }

                item.PatientPhotoUrl = patient.HasPhoto
                    ? Url.Action("Photo", "Patients", new { citizenId = item.CitizenId }) ?? "/img/photo-not-available.svg"
                    : "/img/photo-not-available.svg";
            }
            else
            {
                item.PatientPhotoUrl = "/img/photo-not-available.svg";
            }
        }

        return new SignaturesDashboardViewModel
        {
            Input = existingModel?.Input ?? new SignatureUploadViewModel(),
            Items = signatures,
            CurrentPage = currentPage,
            TotalItems = totalItems,
            TotalPages = totalPages,
            SearchTerm = normalizedSearch ?? string.Empty,
            StatusMessage = statusMessage
        };
    }
}
