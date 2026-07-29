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
    private static readonly string[] DefaultRoleOptions = { "Nurse", "User" };
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
    public async Task<IActionResult> Edit(
        string id,
        string fullName,
        string phoneNumber,
        string? email,
        string roleName,
        string? professionalTitle,
        string? licenseNo)
    {
        var currentUser = await userManager.GetUserAsync(User);
        var target = await userManager.FindByIdAsync(id);
        if (target is null || !CanManageUser(currentUser, target))
        {
            return NotFound();
        }

        var allowedRoles = User.IsInRole("SuperAdmin") ? new[] { "AdminClinic", "Nurse", "User" } : DefaultRoleOptions;
        if (!allowedRoles.Contains(roleName, StringComparer.Ordinal))
        {
            TempData["StatusMessage"] = "บทบาทที่เลือกไม่ถูกต้อง";
            return RedirectToAction(nameof(Index));
        }

        target.FullName = fullName?.Trim();
        target.PhoneNumber = phoneNumber?.Trim();
        target.Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
        target.ProfessionalTitle = professionalTitle?.Trim();
        target.LicenseNo = licenseNo?.Trim();

        var updateResult = await userManager.UpdateAsync(target);
        if (!updateResult.Succeeded)
        {
            TempData["StatusMessage"] = string.Join(", ", updateResult.Errors.Select(x => x.Description));
            return RedirectToAction(nameof(Index));
        }

        var roles = await userManager.GetRolesAsync(target);
        await userManager.RemoveFromRolesAsync(target, roles);
        await userManager.AddToRoleAsync(target, roleName);

        TempData["StatusMessage"] = "แก้ไขข้อมูลผู้ใช้งานเรียบร้อย";
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
            TempData["StatusMessage"] = "กรุณาเลือกไฟล์ลายเซ็น";
            return RedirectToAction(nameof(Index));
        }

        var extension = Path.GetExtension(signatureFile.FileName);
        if (!AllowedSignatureExtensions.Contains(extension))
        {
            TempData["StatusMessage"] = "รองรับเฉพาะไฟล์ .png .jpg .jpeg .webp";
            return RedirectToAction(nameof(Index));
        }

        await using var stream = new MemoryStream();
        await signatureFile.CopyToAsync(stream);
        target.ProviderSignatureFileName = $"{target.UserName}{extension.ToLowerInvariant()}";
        target.ProviderSignatureContentType = string.IsNullOrWhiteSpace(signatureFile.ContentType) ? "image/png" : signatureFile.ContentType;
        target.ProviderSignatureImageData = stream.ToArray();
        target.ProviderSignatureUploadedAtUtc = DateTime.UtcNow;

        await userManager.UpdateAsync(target);
        TempData["StatusMessage"] = "อัปโหลดลายเซ็นผู้ให้บริการเรียบร้อย";
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
}
