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

    public ClinicsController(ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        this.dbContext = dbContext;
        this.userManager = userManager;
        this.roleManager = roleManager;
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
            model.ClinicName = currentClinic.ClinicName;
            model.Address = model.Address?.Trim() ?? string.Empty;
            model.IsEditMode = true;
            model.CanCreateClinic = false;
            model.CurrentLogoPath = currentClinic.LogoPath;

            if (!ModelState.IsValid)
            {
                return View(await BuildRegistrationViewModel(model));
            }

            currentClinic.FullName = model.FullName.Trim();
            currentClinic.PhoneNumber = model.PhoneNumber.Trim();
            currentClinic.Email = model.Email.Trim();
            currentClinic.Address = model.Address;
            currentClinic.Theme = AllowedThemes.Contains(model.Theme) ? model.Theme.ToLowerInvariant() : currentClinic.Theme;

            var logoPath = await SaveLogo(model.Logo);
            if (!string.IsNullOrWhiteSpace(logoPath))
            {
                currentClinic.LogoPath = logoPath;
            }

            await dbContext.SaveChangesAsync();
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
        if (string.IsNullOrWhiteSpace(currentUser?.ClinicCode))
        {
            return null;
        }

        return await dbContext.Clinics.FirstOrDefaultAsync(x => x.ClinicCode == currentUser.ClinicCode);
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
            Theme = clinic.Theme,
            IsEditMode = true,
            CanCreateClinic = canCreateClinic,
            CurrentLogoPath = clinic.LogoPath
        };
    }

    private static async Task<string?> SaveLogo(IFormFile? logo)
    {
        if (logo is null || logo.Length == 0)
        {
            return null;
        }

        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "logos");
        Directory.CreateDirectory(uploadsFolder);
        var fileName = $"{Guid.NewGuid():N}{Path.GetExtension(logo.FileName)}";
        var filePath = Path.Combine(uploadsFolder, fileName);

        await using var stream = System.IO.File.Create(filePath);
        await logo.CopyToAsync(stream);
        return $"/uploads/logos/{fileName}";
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
