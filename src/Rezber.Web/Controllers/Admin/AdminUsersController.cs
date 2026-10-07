using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rezber.Domain.Identity;
using Rezber.Services.Features;
using Rezber.Web.Services;

namespace Rezber.Web.Controllers.Admin;

[Authorize(Roles = Roles.Admin)]
[Route("admin/users")]
public class AdminUsersController : Controller
{
    private readonly IAdminUserService _adminUserService;
    private readonly ICurrentUserAccessor _currentUser;

    public AdminUsersController(IAdminUserService adminUserService, ICurrentUserAccessor currentUser)
    {
        _adminUserService = adminUserService;
        _currentUser = currentUser;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(string? status, string? role, CancellationToken ct)
    {
        var users = await _adminUserService.GetUsersAsync(status, role, ct);
        ViewBag.StatusFilter = status;
        ViewBag.RoleFilter = role;
        return View("~/Views/Admin/Users.cshtml", users);
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateUser(
        string displayName,
        string email,
        string jobTitle,
        string password,
        CancellationToken ct)
    {
        var result = await _adminUserService.CreateAsync(
            displayName, email, jobTitle, password, await _currentUser.GetAsync(ct), GetIp(), ct);
        TempData[result.Succeeded ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("update")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateUser(
        Guid id,
        string displayName,
        string jobTitle,
        bool isActive,
        CancellationToken ct)
    {
        var result = await _adminUserService.UpdateAsync(
            id, displayName, jobTitle, isActive, await _currentUser.GetAsync(ct), GetIp(), ct);
        if (result.NotFound)
            return NotFound();

        TempData[result.Succeeded ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteUser(Guid id, CancellationToken ct)
    {
        var result = await _adminUserService.DeleteAsync(
            id, await _currentUser.GetAsync(ct), GetIp(), ct);
        if (result.NotFound)
            return NotFound();

        TempData[result.Succeeded ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    private string GetIp() =>
        HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}