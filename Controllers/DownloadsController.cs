using Microsoft.AspNetCore.Mvc;

namespace SmartClinic.Web.Controllers;

public class DownloadsController : Controller
{
    private const string BridgePackageFileName = "SmartClinic-CardReader-Bridge-v1.0.4.zip";

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult DownloadsGuide()
    {
        return PhysicalFile(Path.Combine(Directory.GetCurrentDirectory(), "DOWNLOADS.md"), "text/markdown", "DOWNLOADS.md");
    }

    public IActionResult InstallationGuide()
    {
        return PhysicalFile(Path.Combine(Directory.GetCurrentDirectory(), "INSTALLATION.md"), "text/markdown", "INSTALLATION.md");
    }

    public IActionResult QuickStartGuide()
    {
        return PhysicalFile(Path.Combine(Directory.GetCurrentDirectory(), "QUICKSTART.md"), "text/markdown", "QUICKSTART.md");
    }

    public IActionResult Readme()
    {
        return PhysicalFile(Path.Combine(Directory.GetCurrentDirectory(), "README.md"), "text/markdown", "README.md");
    }

    public IActionResult DeploymentGuide()
    {
        return PhysicalFile(Path.Combine(Directory.GetCurrentDirectory(), "DEPLOYMENT.md"), "text/markdown", "DEPLOYMENT.md");
    }

    public IActionResult ProductInfo()
    {
        return PhysicalFile(Path.Combine(Directory.GetCurrentDirectory(), "PRODUCT_INFO.md"), "text/markdown", "PRODUCT_INFO.md");
    }

    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public IActionResult BridgePackage()
    {
        var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "downloads", BridgePackageFileName);
        if (!System.IO.File.Exists(path))
        {
            return NotFound("ไม่พบไฟล์ติดตั้ง Smart Card Bridge");
        }

        return PhysicalFile(path, "application/zip", BridgePackageFileName, enableRangeProcessing: true);
    }
}
