using Microsoft.AspNetCore.Identity;
using SmartClinic.Web.Models;

namespace SmartClinic.Web.Data;

public static class IdentitySeed
{
    private static readonly string[] RequiredRoles =
    {
        "SuperAdmin",
        "AdminClinic",
        "Nurse",
        "User"
    };

    public static async Task SeedRolesAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        foreach (var role in RequiredRoles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }

    public static async Task SeedSuperAdminAsync(IServiceProvider services)
    {
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var superAdminUserName = "SUPERADMIN";

        var existingUser = await userManager.FindByNameAsync(superAdminUserName);
        if (existingUser is null)
        {
            var superAdmin = new ApplicationUser
            {
                UserName = superAdminUserName,
                Email = "superadmin@smartclinic.local",
                PhoneNumber = "0999999999",
                FullName = "SmartClinic Super Admin",
                MustChangePassword = true
            };

            var createResult = await userManager.CreateAsync(superAdmin, "0999999999");
            if (!createResult.Succeeded)
            {
                return;
            }

            await userManager.AddToRoleAsync(superAdmin, "SuperAdmin");
            return;
        }

        if (!await userManager.IsInRoleAsync(existingUser, "SuperAdmin"))
        {
            await userManager.AddToRoleAsync(existingUser, "SuperAdmin");
        }
    }
}