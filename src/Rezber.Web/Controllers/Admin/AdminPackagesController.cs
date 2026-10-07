
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rezber.Domain.Identity;
using Rezber.Services.Features;
using Rezber.Web.Services;

namespace Rezber.Web.Controllers.Admin;

[Authorize(Roles = Roles.Admin)]
[Route("admin/packages")]
public class AdminPackagesController : Controller
{
    private readonly IAdminOperationsService _adminOperations;
    private readonly ICurrentUserAccessor _currentUser;

    public AdminPackagesController(
        IAdminOperationsService adminOperations,
        ICurrentUserAccessor currentUser)
    {
        _adminOperations = adminOperations;
        _currentUser = currentUser;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var packages = await _adminOperations.GetPackagesAsync(ct);
        return View("~/Views/Admin/Packages.cshtml", packages);
    }

    [HttpPost("delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeletePackage(string id, CancellationToken ct)
    {
        var current = await _currentUser.GetAsync(ct);
        var result = await _adminOperations.DeletePackageAsync(id, current, GetIp(), ct);
        if (result.Status == PackageWorkflowStatus.NotFound)
            return NotFound();

        TempData["Success"] = $"Package {result.Value!.Name} deleted.";
        return RedirectToAction(nameof(Index));
    }

    private string GetIp() =>
        HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}
