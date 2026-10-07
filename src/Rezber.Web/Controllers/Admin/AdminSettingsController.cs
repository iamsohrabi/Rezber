using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rezber.Domain.Identity;
using Rezber.Services.Features;

namespace Rezber.Web.Controllers.Admin;

[Authorize(Roles = Roles.Admin)]
[Route("admin/settings")]
public class AdminSettingsController : Controller
{
    private readonly IAdminNexusSettingsService _settingsService;

    public AdminSettingsController(IAdminNexusSettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken ct) =>
        View("~/Views/Admin/Settings.cshtml", await _settingsService.GetAsync(ct));


}
