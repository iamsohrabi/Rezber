using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Rezber.Web.Models;

namespace Rezber.Web.Controllers;

public class HomeController : Controller
{
    [HttpGet]
    public IActionResult Index() =>
        RedirectToAction("Index", "PackageBrowser");

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View();
}
