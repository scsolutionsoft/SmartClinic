using Microsoft.EntityFrameworkCore;
using SmartClinic.Web.Models;

namespace SmartClinic.Web.Data;

public static class NhssoClinicSeed
{
    public static async Task SeedAsync(ApplicationDbContext dbContext)
    {
        if (await dbContext.NhssoClinicMasters.AnyAsync())
        {
            return;
        }

        var masters = new[]
        {
            new NhssoClinicMaster
            {
                ClinicCode = "AB12CD34EF",
                ClinicName = "คลินิกเวชกรรมตัวอย่าง 1",
                Address = "99/1 ถนนสุขภาพ แขวงจตุจักร เขตจตุจักร กรุงเทพมหานคร",
                ContactPhone = "021234567",
                ContactEmail = "clinic1@example.com",
                IsActive = true
            },
            new NhssoClinicMaster
            {
                ClinicCode = "GH56IJ78KL",
                ClinicName = "คลินิกเวชกรรมตัวอย่าง 2",
                Address = "88/8 ถนนสุขภาพ ตำบลในเมือง อำเภอเมือง ขอนแก่น",
                ContactPhone = "043123456",
                ContactEmail = "clinic2@example.com",
                IsActive = true
            }
        };

        dbContext.NhssoClinicMasters.AddRange(masters);
        await dbContext.SaveChangesAsync();
    }
}