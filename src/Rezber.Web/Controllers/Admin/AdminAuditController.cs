using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rezber.Domain.Identity;
using Rezber.Services.Features;
using Rezber.Web.Services;

namespace Rezber.Web.Controllers.Admin;

[Authorize(Roles = Roles.Admin)]
[Route("admin/audit")]
public class AdminAuditController : Controller
{
    private readonly IAdminOperationsService _adminOperations;
    private readonly ICurrentUserAccessor _currentUser;

    public AdminAuditController(
        IAdminOperationsService adminOperations,
        ICurrentUserAccessor currentUser)
    {
        _adminOperations = adminOperations;
        _currentUser = currentUser;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var logs = await _adminOperations.GetAuditLogsAsync(ct);
        return View("~/Views/Admin/Audit.cshtml", logs);
    }

    [HttpPost("delete-old")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteOldLogs(CancellationToken ct)
    {
        TempData["Success"] = await _adminOperations.DeleteOldAuditLogsAsync(
            await _currentUser.GetAsync(ct), GetIp(), ct);
        return RedirectToAction(nameof(Index));
    }

    private string GetIp() =>
        HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}
