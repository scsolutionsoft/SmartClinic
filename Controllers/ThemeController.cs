using Microsoft.AspNetCore.Mvc;

namespace SmartClinic.Web.Controllers;

public class ThemeController : Controller
{
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Set(string theme, string? returnUrl = null)
    {
        var allowedThemes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
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

        var normalizedTheme = allowedThemes.Contains(theme) ? theme.ToLowerInvariant() : "lux";
        Response.Cookies.Append(
            "smartclinic.theme",
            normalizedTheme,
            new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                IsEssential = true,
                SameSite = SameSiteMode.Lax
            });

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return RedirectToAction("Index", "Home");
    }
}
