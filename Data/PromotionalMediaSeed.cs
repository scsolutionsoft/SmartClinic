using Microsoft.EntityFrameworkCore;
using SmartClinic.Web.Models;

namespace SmartClinic.Web.Data;

public static class PromotionalMediaSeed
{
    public static async Task SeedAsync(ApplicationDbContext db)
    {
        if (await db.PromotionalMedia.AnyAsync()) return;
        db.PromotionalMedia.AddRange(
            new PromotionalMedia
            {
                Title = "SmartClinic ในทุกจังหวะการดูแล", Description = "ภาพรวมระบบสำหรับหน้า Hero",
                MediaType = "Video", Placement = "Hero", MediaUrl = "/videos/smartclinic-hero-loop.mp4",
                PosterUrl = "/img/smartclinic-signup-hero.png", AutoPlay = true, Loop = true, DisplayOrder = 0
            },
            new PromotionalMedia
            {
                Title = "SmartClinic Product Tour", Description = "ตั้งแต่ข้อมูลผู้ป่วยจนถึงการเติบโตแบบ Unlimited",
                MediaType = "Video", Placement = "Feature", MediaUrl = "/videos/smartclinic-product-tour.mp4",
                PosterUrl = "/img/promo-scenes/clinic-dashboard.png", AutoPlay = false, Loop = false, DisplayOrder = 0
            });
        await db.SaveChangesAsync();
    }
}
