using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SmartClinic.Web.Data;
using SmartClinic.Web.Models;
using SmartClinic.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient();
builder.Services.AddScoped<IWoundCareOcrService, WoundCareOcrService>();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequireDigit = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 6;
        options.User.RequireUniqueEmail = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/Login";
    options.SlidingExpiration = true;
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var dbContext = services.GetRequiredService<ApplicationDbContext>();
    await dbContext.Database.MigrateAsync();

    await IdentitySeed.SeedRolesAsync(services);
    await IdentitySeed.SeedSuperAdminAsync(services);
    await NhssoClinicSeed.SeedAsync(dbContext);
    await PromotionalMediaSeed.SeedAsync(dbContext);
    await Icd10Seed.SeedAsync(dbContext, app.Environment.ContentRootPath);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.Use(async (context, next) =>
{
    if (context.User.Identity?.IsAuthenticated == true)
    {
        var path = context.Request.Path;
        var isAllowedPath = path.StartsWithSegments("/Account/ChangePassword")
            || path.StartsWithSegments("/Account/Logout")
            || path.StartsWithSegments("/Account/Suspended")
            || path.StartsWithSegments("/Theme/Set");

        if (!isAllowedPath && !Path.HasExtension(path))
        {
            var userManager = context.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await userManager.GetUserAsync(context.User);
            if (!string.IsNullOrWhiteSpace(user?.ClinicCode))
            {
                var db = context.RequestServices.GetRequiredService<ApplicationDbContext>();
                var clinicStatus = await db.Clinics.AsNoTracking().Where(x => x.ClinicCode == user.ClinicCode).Select(x => x.Status).FirstOrDefaultAsync();
                if (!string.IsNullOrWhiteSpace(clinicStatus) && clinicStatus != "Active")
                {
                    await context.RequestServices.GetRequiredService<SignInManager<ApplicationUser>>().SignOutAsync();
                    context.Response.Redirect("/Account/Suspended");
                    return;
                }
            }
            if (user?.MustChangePassword == true)
            {
                context.Response.Redirect("/Account/ChangePassword");
                return;
            }
        }
    }

    await next();
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
