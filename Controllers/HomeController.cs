using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SmartClinic.Web.Models;

namespace SmartClinic.Web.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    [Microsoft.AspNetCore.Authorization.Authorize]
    public IActionResult Phase1()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
