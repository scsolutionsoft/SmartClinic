using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartClinic.Web.Data;
using SmartClinic.Web.Models;
using SmartClinic.Web.ViewModels;

namespace SmartClinic.Web.Controllers;

[Authorize(Roles = "SuperAdmin,AdminClinic")]
public class ClinicUsersController : Controller
{
    private const long MaxSignatureFileSize = 5 * 1024 * 1024;
    private static readonly string[] DefaultRoleOptions = { "AdminClinic", "Nurse", "User" };
    private static readonly HashSet<string> AllowedSignatureExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png",
        ".jpg",
        ".jpeg",
        ".webp"
    };

    private readonly ApplicationDbContext dbContext;
    private readonly UserManager<ApplicationUser> userManager;

    public ClinicUsersController(ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager)
    {
        this.dbContext = dbContext;
        this.userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var model = await BuildDashboard("พร้อมจัดการบัญชีพยาบาลและเจ้าหน้าที่");
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ClinicUsersDashboardViewModel model)
    {
        var currentUser = await userManager.GetUserAsync(User);
        var isSuperAdmin = User.IsInRole("SuperAdmin");
        var clinicCode = isSuperAdmin ? model.Input.ClinicCode : currentUser?.ClinicCode;

        if (string.IsNullOrWhiteSpace(clinicCode))
        {
            ModelState.AddModelError(nameof(model.Input.ClinicCode), "ไม่พบรหัสคลินิกสำหรับการสร้างบัญชี");
        }

        var allowedRoles = isSuperAdmin ? new[] { "AdminClinic", "Nurse", "User" } : DefaultRoleOptions;
        if (!allowedRoles.Contains(model.Input.RoleName, StringComparer.Ordinal))
        {
            ModelState.AddModelError(nameof(model.Input.RoleName), "บทบาทที่เลือกไม่ถูกต้อง");
        }

        if (!ModelState.IsValid)
        {
            return View("Index", await BuildDashboard("ข้อมูลไม่ครบถ้วน", model));
        }

        var existing = await userManager.FindByNameAsync(model.Input.UserName);
        if (existing is not null)
        {
            ModelState.AddModelError(nameof(model.Input.UserName), "ชื่อผู้ใช้นี้มีอยู่แล้ว");
            return View("Index", await BuildDashboard("ชื่อผู้ใช้ซ้ำ", model));
        }

        var newUser = new ApplicationUser
        {
            UserName = model.Input.UserName.Trim(),
            NamePrefix = model.Input.NamePrefix.Trim(),
            FullName = model.Input.FullName.Trim(),
            PhoneNumber = model.Input.PhoneNumber.Trim(),
            Email = string.IsNullOrWhiteSpace(model.Input.Email) ? null : model.Input.Email.Trim(),
            ClinicCode = clinicCode,
            ProfessionalTitle = model.Input.ProfessionalTitle?.Trim(),
            LicenseNo = model.Input.LicenseNo?.Trim(),
            MustChangePassword = true
        };

        var createResult = await userManager.CreateAsync(newUser, model.Input.PhoneNumber.Trim());
        if (!createResult.Succeeded)
        {
            foreach (var error in createResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View("Index", await BuildDashboard("ไม่สามารถสร้างบัญชีได้", model));
        }

        await userManager.AddToRoleAsync(newUser, model.Input.RoleName);
        TempData["StatusModal"] = "clinic-user-created";
        TempData["NewUserName"] = newUser.UserName;
        TempData["NewUserRole"] = model.Input.RoleName;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditClinicUserViewModel model)
    {
        var currentUser = await userManager.GetUserAsync(User);
        var target = await userManager.FindByIdAsync(model.Id);
        if (target is null || !CanManageUser(currentUser, target))
        {
            return NotFound();
        }

        var allowedRoles = User.IsInRole("SuperAdmin") ? new[] { "AdminClinic", "Nurse", "User" } : DefaultRoleOptions;
        if (!allowedRoles.Contains(model.RoleName, StringComparer.Ordinal))
        {
            ModelState.AddModelError(nameof(model.RoleName), "บทบาทที่เลือกไม่ถูกต้อง");
        }

        if (!ModelState.IsValid)
        {
            SetEditStatus(false, string.Join(" ", ModelState.Values
                .SelectMany(x => x.Errors)
                .Select(x => string.IsNullOrWhiteSpace(x.ErrorMessage) ? "ข้อมูลไม่ถูกต้อง" : x.ErrorMessage)
                .Distinct()));
            return RedirectToAction(nameof(Index));
        }

        target.NamePrefix = model.NamePrefix.Trim();
        target.FullName = model.FullName.Trim();
        target.PhoneNumber = model.PhoneNumber.Trim();
        target.Email = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email.Trim();
        target.ProfessionalTitle = string.IsNullOrWhiteSpace(model.ProfessionalTitle) ? null : model.ProfessionalTitle.Trim();
        target.LicenseNo = string.IsNullOrWhiteSpace(model.LicenseNo) ? null : model.LicenseNo.Trim();

        var updateResult = await userManager.UpdateAsync(target);
        if (!updateResult.Succeeded)
        {
            SetEditStatus(false, string.Join(", ", updateResult.Errors.Select(x => x.Description)));
            return RedirectToAction(nameof(Index));
        }

        var roles = await userManager.GetRolesAsync(target);
        if (!roles.Contains(model.RoleName, StringComparer.Ordinal))
        {
            var addRoleResult = await userManager.AddToRoleAsync(target, model.RoleName);
            if (!addRoleResult.Succeeded)
            {
                SetEditStatus(false, string.Join(", ", addRoleResult.Errors.Select(x => x.Description)));
                return RedirectToAction(nameof(Index));
            }

            var obsoleteRoles = roles.Where(x => !string.Equals(x, model.RoleName, StringComparison.Ordinal)).ToArray();
            if (obsoleteRoles.Length > 0)
            {
                var removeRoleResult = await userManager.RemoveFromRolesAsync(target, obsoleteRoles);
                if (!removeRoleResult.Succeeded)
                {
                    SetEditStatus(false, string.Join(", ", removeRoleResult.Errors.Select(x => x.Description)));
                    return RedirectToAction(nameof(Index));
                }
            }
        }

        SetEditStatus(true, "แก้ไขข้อมูลผู้ใช้งานเรียบร้อย");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        var currentUser = await userManager.GetUserAsync(User);
        var target = await userManager.FindByIdAsync(id);
        if (target is null || !CanManageUser(currentUser, target) || target.Id == currentUser?.Id || await userManager.IsInRoleAsync(target, "SuperAdmin"))
        {
            return NotFound();
        }

        var result = await userManager.DeleteAsync(target);
        TempData["StatusMessage"] = result.Succeeded
            ? "ลบผู้ใช้งานเรียบร้อย"
            : string.Join(", ", result.Errors.Select(x => x.Description));
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadProviderSignature(string id, IFormFile signatureFile)
    {
        var currentUser = await userManager.GetUserAsync(User);
        var target = await userManager.FindByIdAsync(id);
        if (target is null || !CanManageUser(currentUser, target))
        {
            return NotFound();
        }

        if (signatureFile is null || signatureFile.Length == 0)
        {
            SetProviderSignatureStatus(false, "กรุณาเลือกไฟล์ลายเซ็น");
            return RedirectToAction(nameof(Index));
        }

        if (signatureFile.Length > MaxSignatureFileSize)
        {
            SetProviderSignatureStatus(false, "ไฟล์ลายเซ็นต้องมีขนาดไม่เกิน 5 MB");
            return RedirectToAction(nameof(Index));
        }

        var extension = Path.GetExtension(signatureFile.FileName);
        if (!AllowedSignatureExtensions.Contains(extension))
        {
            SetProviderSignatureStatus(false, "รองรับเฉพาะไฟล์ .png .jpg .jpeg .webp");
            return RedirectToAction(nameof(Index));
        }

        await using var stream = new MemoryStream();
        await signatureFile.CopyToAsync(stream);
        target.ProviderSignatureFileName = $"{target.UserName}{extension.ToLowerInvariant()}";
        target.ProviderSignatureContentType = string.IsNullOrWhiteSpace(signatureFile.ContentType) ? "image/png" : signatureFile.ContentType;
        target.ProviderSignatureImageData = stream.ToArray();
        target.ProviderSignatureUploadedAtUtc = DateTime.UtcNow;

        var updateResult = await userManager.UpdateAsync(target);
        SetProviderSignatureStatus(
            updateResult.Succeeded,
            updateResult.Succeeded
                ? "อัปโหลดลายเซ็นผู้ให้บริการเรียบร้อย"
                : string.Join(", ", updateResult.Errors.Select(x => x.Description)));
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CaptureProviderSignature(string id, string signatureData)
    {
        var currentUser = await userManager.GetUserAsync(User);
        var target = await userManager.FindByIdAsync(id);
        if (target is null || !CanManageUser(currentUser, target))
        {
            return NotFound();
        }

        const string pngPrefix = "data:image/png;base64,";
        if (string.IsNullOrWhiteSpace(signatureData) ||
            !signatureData.StartsWith(pngPrefix, StringComparison.Ordinal))
        {
            SetProviderSignatureStatus(false, "ไม่พบข้อมูลลายเซ็น PNG");
            return RedirectToAction(nameof(Index));
        }

        byte[] imageData;
        try
        {
            imageData = Convert.FromBase64String(signatureData[pngPrefix.Length..]);
        }
        catch (FormatException)
        {
            SetProviderSignatureStatus(false, "ข้อมูลภาพลายเซ็นไม่ถูกต้อง");
            return RedirectToAction(nameof(Index));
        }

        var pngHeader = new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 };
        if (imageData.Length == 0 ||
            imageData.Length > MaxSignatureFileSize ||
            imageData.Length < pngHeader.Length ||
            !imageData.AsSpan(0, pngHeader.Length).SequenceEqual(pngHeader))
        {
            SetProviderSignatureStatus(false, "ไฟล์ลายเซ็น PNG ไม่ถูกต้องหรือมีขนาดเกิน 5 MB");
            return RedirectToAction(nameof(Index));
        }

        target.ProviderSignatureFileName = $"{target.UserName}.png";
        target.ProviderSignatureContentType = "image/png";
        target.ProviderSignatureImageData = imageData;
        target.ProviderSignatureUploadedAtUtc = DateTime.UtcNow;
        var result = await userManager.UpdateAsync(target);
        SetProviderSignatureStatus(
            result.Succeeded,
            result.Succeeded
                ? "บันทึกลายเซ็นผู้ให้บริการเรียบร้อย"
                : string.Join(", ", result.Errors.Select(x => x.Description)));
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> ProviderSignature(string id)
    {
        var currentUser = await userManager.GetUserAsync(User);
        var target = await userManager.FindByIdAsync(id);
        if (target is null || !CanManageUser(currentUser, target) || target.ProviderSignatureImageData is null || target.ProviderSignatureImageData.Length == 0)
        {
            return NotFound();
        }

        return File(target.ProviderSignatureImageData, target.ProviderSignatureContentType ?? "image/png");
    }

    private async Task<ClinicUsersDashboardViewModel> BuildDashboard(string statusMessage, ClinicUsersDashboardViewModel? existing = null)
    {
        var currentUser = await userManager.GetUserAsync(User);
        var isSuperAdmin = User.IsInRole("SuperAdmin");
        var scopeClinicCode = isSuperAdmin ? null : currentUser?.ClinicCode;

        var query = userManager.Users.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(scopeClinicCode))
        {
            query = query.Where(x => x.ClinicCode == scopeClinicCode);
        }

        var users = await query
            .OrderBy(x => x.ClinicCode)
            .ThenBy(x => x.UserName)
            .Take(200)
            .ToListAsync();

        var items = new List<ClinicUserItemViewModel>();
        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user);
            items.Add(new ClinicUserItemViewModel
            {
                Id = user.Id,
                UserName = user.UserName ?? string.Empty,
                NamePrefix = user.NamePrefix ?? string.Empty,
                FullName = user.FullName ?? string.Empty,
                ClinicCode = user.ClinicCode ?? "-",
                PhoneNumber = user.PhoneNumber ?? "-",
                Email = user.Email ?? string.Empty,
                RoleName = roles.FirstOrDefault() ?? "-",
                ProfessionalTitle = user.ProfessionalTitle ?? string.Empty,
                LicenseNo = user.LicenseNo ?? string.Empty,
                HasProviderSignature = user.ProviderSignatureImageData is { Length: > 0 },
                CanDelete = user.Id != currentUser?.Id && !roles.Contains("SuperAdmin"),
                MustChangePassword = user.MustChangePassword
            });
        }

        var clinicOptions = isSuperAdmin
            ? await dbContext.Clinics.AsNoTracking().OrderBy(x => x.ClinicCode).Select(x => x.ClinicCode).ToListAsync()
            : new List<string> { scopeClinicCode ?? string.Empty };

        return new ClinicUsersDashboardViewModel
        {
            Input = existing?.Input ?? new CreateClinicUserViewModel { ClinicCode = scopeClinicCode },
            RoleOptions = isSuperAdmin ? new[] { "AdminClinic", "Nurse", "User" } : DefaultRoleOptions,
            ClinicOptions = clinicOptions,
            Users = items,
            StatusMessage = statusMessage
        };
    }

    private bool CanManageUser(ApplicationUser? currentUser, ApplicationUser target)
    {
        if (currentUser is null)
        {
            return false;
        }

        if (User.IsInRole("SuperAdmin"))
        {
            return true;
        }

        return !string.IsNullOrWhiteSpace(currentUser.ClinicCode) &&
            string.Equals(currentUser.ClinicCode, target.ClinicCode, StringComparison.OrdinalIgnoreCase);
    }

    private void SetEditStatus(bool succeeded, string message)
    {
        TempData["StatusModal"] = succeeded ? "clinic-user-edited" : "clinic-user-edit-error";
        TempData["StatusMessage"] = message;
    }

    private void SetProviderSignatureStatus(bool succeeded, string message)
    {
        TempData["StatusModal"] = succeeded ? "provider-signature-saved" : "provider-signature-error";
        TempData["StatusMessage"] = message;
    }
}
