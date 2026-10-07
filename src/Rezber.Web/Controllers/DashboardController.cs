using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rezber.Services.Features;

namespace Rezber.Web.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly IUserDashboardService _dashboardService;

    public DashboardController(IUserDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        return View(await _dashboardService.GetAsync(ct));
    }
}


