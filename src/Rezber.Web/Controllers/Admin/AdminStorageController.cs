using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rezber.Domain.Identity;
using Rezber.Services.Features;

namespace Rezber.Web.Controllers.Admin;

[Authorize(Roles = Roles.Admin)]
[Route("admin/storage")]
public class AdminStorageController : Controller
{
    private readonly IStorageService _storageService;

    public AdminStorageController(IStorageService storageService)
    {
        _storageService = storageService;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var snapshot = await _storageService.GetSnapshotAsync(ct);
        return View("~/Views/Admin/Storage.cshtml", snapshot);
    }
}
