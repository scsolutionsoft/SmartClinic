using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartClinic.Web.Models;
using SmartClinic.Web.ViewModels;

namespace SmartClinic.Web.Controllers;

public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> signInManager;
    private readonly UserManager<ApplicationUser> userManager;

    public AccountController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager)
    {
        this.signInManager = signInManager;
        this.userManager = userManager;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await signInManager.PasswordSignInAsync(model.UserName, model.Password, model.RememberMe, lockoutOnFailure: false);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, "เข้าสู่ระบบไม่สำเร็จ");
            return View(model);
        }

        var user = await userManager.FindByNameAsync(model.UserName);
        if (!string.IsNullOrWhiteSpace(user?.ClinicCode))
        {
            var db = HttpContext.RequestServices.GetRequiredService<SmartClinic.Web.Data.ApplicationDbContext>();
            var clinic = await db.Clinics.AsNoTracking().FirstOrDefaultAsync(x => x.ClinicCode == user.ClinicCode);
            if (clinic is not null && clinic.Status != "Active")
            {
                await signInManager.SignOutAsync();
                return RedirectToAction(nameof(Suspended));
            }
        }
        if (user?.MustChangePassword == true)
        {
            return RedirectToAction(nameof(ChangePassword));
        }

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Home");
    }

    [HttpGet, AllowAnonymous]
    public IActionResult Suspended() => View();

    [HttpGet]
    [Authorize]
    public IActionResult ChangePassword()
    {
        return View(new ChangePasswordViewModel());
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return RedirectToAction(nameof(Login));
        }

        var changeResult = await userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        if (!changeResult.Succeeded)
        {
            foreach (var error in changeResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        user.MustChangePassword = false;
        await userManager.UpdateAsync(user);
        await signInManager.RefreshSignInAsync(user);
        TempData["StatusModal"] = "password-changed";
        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }
}
