using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rezber.Domain.Identity;
using Rezber.Services.Features;
using Rezber.Web.Services;
using Rezber.Services.Views.Admin;

namespace Rezber.Web.Controllers.Admin;

[Authorize(Roles = Roles.Admin)]
[Route("admin/cleanup")]
public class AdminCleanupController : Controller
{
    private readonly IAdminOperationsService _adminOperations;
    private readonly ICurrentUserAccessor _currentUser;

    public AdminCleanupController(
        IAdminOperationsService adminOperations,
        ICurrentUserAccessor currentUser)
    {
        _adminOperations = adminOperations;
        _currentUser = currentUser;
    }

    [HttpGet("")]
    public IActionResult Index() => View("~/Views/Admin/Cleanup.cshtml");

    [HttpPost("run")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RunCleanup(CleanupFormDto form, CancellationToken ct)
    {
        TempData["Success"] = await _adminOperations.RunCleanupAsync(
            form, await _currentUser.GetAsync(ct), GetIp(), ct);
        return RedirectToAction(nameof(Index));
    }

    private string GetIp() => HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}
