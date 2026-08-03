using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartClinic.Web.Data;
using SmartClinic.Web.Models;
using SmartClinic.Web.ViewModels;

namespace SmartClinic.Web.Controllers;

[Authorize(Roles = "SuperAdmin,AdminClinic")]
public class ClinicsController : Controller
{
    private const long MaximumLogoSize = 5 * 1024 * 1024;

    private static readonly HashSet<string> AllowedLogoContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp"
    };

    private static readonly Dictionary<string, string> LogoExtensionsByContentType = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp"
    };

    private static readonly HashSet<string> AllowedThemes = new(StringComparer.OrdinalIgnoreCase)
    {
        "lux",
        "flatly",
        "minty",
        "journal",
        "materia",
        "morph",
        "cosmo",
        "litera",
        "pulse",
        "sandstone",
        "simplex",
        "spacelab",
        "united",
        "yeti",
        "zephyr"
    };

    private readonly ApplicationDbContext dbContext;
    private readonly UserManager<ApplicationUser> userManager;
    private readonly RoleManager<IdentityRole> roleManager;
    private readonly IWebHostEnvironment webHostEnvironment;
    private readonly ILogger<ClinicsController> logger;

    public ClinicsController(
        ApplicationDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IWebHostEnvironment webHostEnvironment,
        ILogger<ClinicsController> logger)
    {
        this.dbContext = dbContext;
        this.userManager = userManager;
        this.roleManager = roleManager;
        this.webHostEnvironment = webHostEnvironment;
        this.logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Register()
    {
        var currentUser = await userManager.GetUserAsync(User);
        if (!User.IsInRole("SuperAdmin"))
        {
            var clinic = await GetCurrentUserClinic(currentUser);
            if (clinic is null)
            {
                return NotFound("ไม่พบข้อมูลคลินิกของผู้ใช้งาน");
            }

            return View(await BuildRegistrationViewModel(MapClinicToModel(clinic, canCreateClinic: false)));
        }

        return View(await BuildRegistrationViewModel(new ClinicRegistrationViewModel
        {
            CanCreateClinic = true
        }));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(ClinicRegistrationViewModel model)
    {
        var currentUser = await userManager.GetUserAsync(User);
        if (!User.IsInRole("SuperAdmin"))
        {
            var currentClinic = await GetCurrentUserClinic(currentUser);
            if (currentClinic is null)
            {
                return NotFound("ไม่พบข้อมูลคลินิกของผู้ใช้งาน");
            }

            model.SelectedClinicCode = currentClinic.ClinicCode;
            model.ClinicName = model.ClinicName?.Trim() ?? string.Empty;
            model.Address = model.Address?.Trim() ?? string.Empty;
            model.OpeningHours = model.OpeningHours?.Trim() ?? string.Empty;
            model.IsEditMode = true;
            model.CanCreateClinic = false;
            model.CurrentLogoPath = currentClinic.LogoPath;
            ValidateLogo(model.Logo);

            if (!ModelState.IsValid)
            {
                return View(await BuildRegistrationViewModel(model));
            }

            currentClinic.ClinicName = model.ClinicName;
            currentClinic.FullName = model.FullName.Trim();
            currentClinic.PhoneNumber = model.PhoneNumber.Trim();
            currentClinic.Email = model.Email.Trim();
            currentClinic.Address = model.Address;
            currentClinic.OpeningHours = model.OpeningHours;
            currentClinic.Theme = AllowedThemes.Contains(model.Theme) ? model.Theme.ToLowerInvariant() : currentClinic.Theme;

            string? logoPath = null;
            try
            {
                logoPath = await SaveLogo(model.Logo);
                if (!string.IsNullOrWhiteSpace(logoPath))
                {
                    currentClinic.LogoPath = logoPath;
                }

                await dbContext.SaveChangesAsync();
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or DbUpdateException)
            {
                DeleteNewLogo(logoPath);
                logger.LogError(
                    exception,
                    "Unable to update clinic {ClinicCode} logo/profile.",
                    currentClinic.ClinicCode);
                ModelState.AddModelError(
                    nameof(model.Logo),
                    "ไม่สามารถบันทึกโลโก้ได้ กรุณาตรวจสอบไฟล์แล้วลองอีกครั้ง");
                ViewData["ClinicError"] = "ไม่สามารถบันทึกโลโก้ได้ กรุณาใช้ไฟล์ JPG, PNG หรือ WEBP ขนาดไม่เกิน 5 MB แล้วลองอีกครั้ง";
                model.CurrentLogoPath = currentClinic.LogoPath;
                return View(await BuildRegistrationViewModel(model));
            }

            TempData["ClinicStatus"] = "updated";
            return RedirectToAction(nameof(Register));
        }

        var selectedMaster = await dbContext.NhssoClinicMasters
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.IsActive && x.ClinicCode == model.SelectedClinicCode);

        if (selectedMaster is null)
        {
            ModelState.AddModelError(nameof(model.SelectedClinicCode), "กรุณาเลือกคลินิกจากทะเบียน สปสช.");
        }

        model.ClinicName = selectedMaster?.ClinicName ?? string.Empty;
        model.Address = selectedMaster?.Address ?? string.Empty;
        model.CanCreateClinic = true;
        ValidateLogo(model.Logo);

        if (!ModelState.IsValid)
        {
            var viewModel = await BuildRegistrationViewModel(model);
            return View(viewModel);
        }

        var existingClinic = await dbContext.Clinics.AnyAsync(x => x.ClinicCode == model.SelectedClinicCode);
        if (existingClinic)
        {
            ModelState.AddModelError(nameof(model.SelectedClinicCode), "รหัสคลินิกนี้มีอยู่แล้ว");
            var viewModel = await BuildRegistrationViewModel(model);
            return View(viewModel);
        }

        var clinic = new Clinic
        {
            ClinicCode = model.SelectedClinicCode,
            ClinicName = selectedMaster!.ClinicName,
            FullName = model.FullName,
            PhoneNumber = model.PhoneNumber,
            Address = selectedMaster.Address,
            OpeningHours = model.OpeningHours.Trim(),
            Email = model.Email,
            RegisteredBy = model.FullName,
            Theme = AllowedThemes.Contains(model.Theme) ? model.Theme.ToLowerInvariant() : "lux"
        };

        var savedLogoPath = await SaveLogo(model.Logo);
        if (!string.IsNullOrWhiteSpace(savedLogoPath))
        {
            clinic.LogoPath = savedLogoPath;
        }

        dbContext.Clinics.Add(clinic);
        await dbContext.SaveChangesAsync();

        if (!await roleManager.RoleExistsAsync("AdminClinic"))
        {
            await roleManager.CreateAsync(new IdentityRole("AdminClinic"));
        }

        var user = new ApplicationUser
        {
            UserName = model.SelectedClinicCode,
            Email = model.Email,
            PhoneNumber = model.PhoneNumber,
            FullName = model.FullName,
            ClinicCode = model.SelectedClinicCode,
            MustChangePassword = true
        };

        var createResult = await userManager.CreateAsync(user, model.PhoneNumber);
        if (!createResult.Succeeded)
        {
            foreach (var error in createResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        await userManager.AddToRoleAsync(user, "AdminClinic");
        ViewBag.ClinicCode = model.SelectedClinicCode;
        ViewBag.InitialPassword = model.PhoneNumber;
        return View("RegisterSuccess", clinic);
    }

    private async Task<Clinic?> GetCurrentUserClinic(ApplicationUser? currentUser)
    {
        if (currentUser is null)
        {
            return null;
        }

        var clinicCode = currentUser.ClinicCode?.Trim();
        if (!string.IsNullOrWhiteSpace(clinicCode))
        {
            var clinic = await dbContext.Clinics.FirstOrDefaultAsync(x => x.ClinicCode == clinicCode);
            if (clinic is not null)
            {
                return clinic;
            }
        }

        var userNameClinicCode = currentUser.UserName?.Trim();
        if (string.IsNullOrWhiteSpace(userNameClinicCode))
        {
            return null;
        }

        return await dbContext.Clinics.FirstOrDefaultAsync(x => x.ClinicCode == userNameClinicCode);
    }

    private static ClinicRegistrationViewModel MapClinicToModel(Clinic clinic, bool canCreateClinic)
    {
        return new ClinicRegistrationViewModel
        {
            SelectedClinicCode = clinic.ClinicCode,
            ClinicName = clinic.ClinicName,
            FullName = clinic.FullName,
            PhoneNumber = clinic.PhoneNumber,
            Email = clinic.Email,
            Address = clinic.Address,
            OpeningHours = clinic.OpeningHours,
            Theme = clinic.Theme,
            IsEditMode = true,
            CanCreateClinic = canCreateClinic,
            CurrentLogoPath = clinic.LogoPath
        };
    }

    private void ValidateLogo(IFormFile? logo)
    {
        if (logo is null || logo.Length == 0)
        {
            return;
        }

        if (logo.Length > MaximumLogoSize)
        {
            ModelState.AddModelError(nameof(ClinicRegistrationViewModel.Logo), "ไฟล์โลโก้ต้องมีขนาดไม่เกิน 5 MB");
        }

        if (!AllowedLogoContentTypes.Contains(logo.ContentType))
        {
            ModelState.AddModelError(nameof(ClinicRegistrationViewModel.Logo), "รองรับโลโก้เฉพาะไฟล์ JPG, PNG และ WEBP");
        }
    }

    private async Task<string?> SaveLogo(IFormFile? logo)
    {
        if (logo is null || logo.Length == 0)
        {
            return null;
        }

        if (!LogoExtensionsByContentType.TryGetValue(logo.ContentType, out var extension))
        {
            throw new IOException($"Unsupported logo content type: {logo.ContentType}");
        }

        var webRootPath = string.IsNullOrWhiteSpace(webHostEnvironment.WebRootPath)
            ? Path.Combine(webHostEnvironment.ContentRootPath, "wwwroot")
            : webHostEnvironment.WebRootPath;
        var uploadsFolder = Path.Combine(webRootPath, "uploads", "logos");
        Directory.CreateDirectory(uploadsFolder);
        var fileName = $"{Guid.NewGuid():N}{extension}";
        var filePath = Path.Combine(uploadsFolder, fileName);
        var temporaryPath = $"{filePath}.uploading";

        try
        {
            await using (var stream = new FileStream(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81920,
                useAsync: true))
            {
                await logo.CopyToAsync(stream);
            }

            System.IO.File.Move(temporaryPath, filePath);
        }
        finally
        {
            if (System.IO.File.Exists(temporaryPath))
            {
                System.IO.File.Delete(temporaryPath);
            }
        }

        return $"/uploads/logos/{fileName}";
    }

    private void DeleteNewLogo(string? logoPath)
    {
        if (string.IsNullOrWhiteSpace(logoPath))
        {
            return;
        }

        var webRootPath = string.IsNullOrWhiteSpace(webHostEnvironment.WebRootPath)
            ? Path.Combine(webHostEnvironment.ContentRootPath, "wwwroot")
            : webHostEnvironment.WebRootPath;
        var relativePath = logoPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.GetFullPath(Path.Combine(webRootPath, relativePath));
        var logoFolder = Path.GetFullPath(Path.Combine(webRootPath, "uploads", "logos"));

        if (fullPath.StartsWith(logoFolder, StringComparison.OrdinalIgnoreCase)
            && System.IO.File.Exists(fullPath))
        {
            System.IO.File.Delete(fullPath);
        }
    }

    private async Task<ClinicRegistrationViewModel> BuildRegistrationViewModel(ClinicRegistrationViewModel? model = null)
    {
        var nhssoClinics = await dbContext.NhssoClinicMasters
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.ClinicCode)
            .Select(x => new NhssoClinicOptionViewModel
            {
                ClinicCode = x.ClinicCode,
                ClinicName = x.ClinicName,
                Address = x.Address
            })
            .ToListAsync();

        var vm = model ?? new ClinicRegistrationViewModel();
        vm.NhssoClinics = nhssoClinics;

        if (!vm.IsEditMode && !string.IsNullOrWhiteSpace(vm.SelectedClinicCode))
        {
            var selected = nhssoClinics.FirstOrDefault(x => x.ClinicCode == vm.SelectedClinicCode);
            if (selected is not null)
            {
                vm.ClinicName = selected.ClinicName;
                vm.Address = selected.Address;
            }
        }

        return vm;
    }
}
