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
    public async Task<IActionResult> Index()
    {
        var clinicCode = await GetClinicCode();
        var model = await BuildDashboard(clinicCode, "รองรับอัปโหลดลายเซ็นรายคนและหลายไฟล์");
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

    private async Task<string> GetClinicCode()
    {
        var user = await userManager.GetUserAsync(User);
        return user?.ClinicCode ?? "SMARTCLINIC";
    }

    private async Task<SignaturesDashboardViewModel> BuildDashboard(string clinicCode, string statusMessage, SignaturesDashboardViewModel? existingModel = null)
    {
        var items = await dbContext.SignImgs
            .AsNoTracking()
            .Where(x => x.ClinicCode == clinicCode)
            .OrderByDescending(x => x.UploadedAtUtc)
            .Take(40)
            .Select(x => new SignatureItemViewModel
            {
                Id = x.Id,
                CitizenId = x.CitizenId,
                FileName = x.FileName,
                UploadedAtText = x.UploadedAtUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm")
            })
            .ToListAsync();

        return new SignaturesDashboardViewModel
        {
            Input = existingModel?.Input ?? new SignatureUploadViewModel(),
            Items = items,
            StatusMessage = statusMessage
        };
    }
}