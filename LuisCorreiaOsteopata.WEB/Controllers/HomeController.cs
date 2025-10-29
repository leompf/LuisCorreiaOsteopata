using System.Diagnostics;
using AspNetCoreGeneratedDocument;
using LuisCorreiaOsteopata.WEB.Models;
using Microsoft.AspNetCore.Mvc;

namespace LuisCorreiaOsteopata.WEB.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        if (User.Identity.IsAuthenticated)
        {
            _logger.LogInformation("User {User} is authenticated, redirecting to Homepage for logged users at Account/Index.", User.Identity.Name);
            return RedirectToAction("Index", "Account");
        }

        _logger.LogInformation("Homepage accessed by Anonymous user");
        return View();
    }

    public IActionResult Price()
    {
        _logger.LogInformation("Pricing page accessed by Anonymous user");
        return View();
    }


    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    [Route("Error/404")]
    public IActionResult Error404()
    {
        return View();
    }
}
