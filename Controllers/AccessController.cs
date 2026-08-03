using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartClinic.Web.Data;
using SmartClinic.Web.Models;
using SmartClinic.Web.ViewModels;

namespace SmartClinic.Web.Controllers;

[AllowAnonymous]
public class AccessController(ApplicationDbContext db, UserManager<ApplicationUser> users) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Register()
    {
        return View(await WithAnnouncements(new PublicSignupViewModel()));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(PublicSignupViewModel model)
    {
        model.ClinicCode = model.ClinicCode.Trim().ToUpperInvariant();
        model.ClinicName = model.ClinicName.Trim();
        model.FullName = model.FullName.Trim();
        model.PhoneNumber = model.PhoneNumber.Trim();
        model.Email = model.Email.Trim();
        model.Address = model.Address.Trim();

        if (await db.Clinics.AnyAsync(x => x.ClinicCode == model.ClinicCode) ||
            await users.FindByNameAsync(model.ClinicCode) is not null)
            ModelState.AddModelError(nameof(model.ClinicCode), "รหัสคลินิกนี้มีบัญชีใช้งานแล้ว");

        if (!ModelState.IsValid) return View(await WithAnnouncements(model));

        await using var transaction = await db.Database.BeginTransactionAsync();
        var clinic = new Clinic
        {
            ClinicCode = model.ClinicCode,
            ClinicName = model.ClinicName,
            FullName = model.FullName,
            PhoneNumber = model.PhoneNumber,
            Email = model.Email,
            Address = model.Address,
            OpeningHours = "กรุณากำหนดเวลาทำการในหน้าข้อมูลคลินิก",
            RegisteredBy = model.FullName,
            OpdRecordLimit = 30,
            HasUnlimitedOpdRecords = false
        };
        db.Clinics.Add(clinic);
        await db.SaveChangesAsync();

        var password = CreateTemporaryPassword();
        var user = new ApplicationUser
        {
            UserName = model.ClinicCode,
            Email = model.Email,
            PhoneNumber = model.PhoneNumber,
            FullName = model.FullName,
            ClinicCode = model.ClinicCode,
            MustChangePassword = true
        };
        var result = await users.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            await transaction.RollbackAsync();
            foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description);
            return View(await WithAnnouncements(model));
        }

        await users.AddToRoleAsync(user, "AdminClinic");
        await transaction.CommitAsync();
        return View("Success", new SignupSuccessViewModel
        {
            ClinicName = clinic.ClinicName,
            UserName = user.UserName!,
            TemporaryPassword = password,
            PatientLimit = clinic.OpdRecordLimit
        });
    }

    private async Task<PublicSignupViewModel> WithAnnouncements(PublicSignupViewModel model)
    {
        model.Announcements = await db.PublicAnnouncements.AsNoTracking()
            .Where(x => x.IsPublished).OrderBy(x => x.DisplayOrder).ThenByDescending(x => x.PublishedAtUtc).Take(6).ToListAsync();
        var now = DateTime.UtcNow;
        model.PromotionalMedia = await db.PromotionalMedia.AsNoTracking()
            .Where(x => x.IsPublished && (!x.StartsAtUtc.HasValue || x.StartsAtUtc <= now) && (!x.EndsAtUtc.HasValue || x.EndsAtUtc >= now))
            .OrderBy(x => x.DisplayOrder).ThenByDescending(x => x.CreatedAtUtc).ToListAsync();
        return model;
    }

    private static string CreateTemporaryPassword()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789@#$!";
        return string.Concat(RandomNumberGenerator.GetItems<char>(chars.AsSpan(), 14));
    }
}
