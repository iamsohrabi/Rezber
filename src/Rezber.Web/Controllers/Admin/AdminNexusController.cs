using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rezber.Domain.Identity;
using Rezber.Services.Features;
using Rezber.Services.Views.Admin;

namespace Rezber.Web.Controllers.Admin;

[Authorize(Roles = Roles.Admin)]
[Route("admin/nexus")]
public sealed class AdminNexusController : Controller
{
    private readonly INexusRepositoryClient _nexus;
    private readonly IAdminNexusService _adminNexus;

    public AdminNexusController(INexusRepositoryClient nexus, IAdminNexusService adminNexus)
    {
        _nexus = nexus;
        _adminNexus = adminNexus;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(
        string? q,
        string? searchType,
        string? repository,
        string? format,
        string? name,
        string? version,
        string? group,
        string? repositoryName,
        string? componentId,
        string? assetId,
        CancellationToken ct)
    {
        var page = await _adminNexus.GetPageAsync(new NexusAdminQuery
        {
            Query = q,
            SearchType = searchType,
            Repository = repository,
            Format = format,
            Name = name,
            Version = version,
            Group = group,
            RepositoryName = repositoryName,
            ComponentId = componentId,
            AssetId = assetId
        }, ct);
        return View("~/Views/Admin/Nexus.cshtml", page);
    }

    [HttpGet("status")]
    public Task<NexusStatus> Status(CancellationToken ct) => _nexus.GetStatusAsync(ct);

    [HttpGet("writable")]
    public async Task<IActionResult> Writable(CancellationToken ct) =>
        Ok(new { writable = await _nexus.IsWritableAsync(ct) });

    [HttpGet("repositories")]
    public Task<IReadOnlyCollection<NexusRepository>> Repositories(CancellationToken ct) =>
        _nexus.GetRepositoriesAsync(ct);

    [HttpGet("repositories/{repositoryName}")]
    public Task<NexusRepository> Repository(string repositoryName, CancellationToken ct) =>
        _nexus.GetRepositoryAsync(repositoryName, ct);

    [HttpGet("search")]
    public Task<NexusSearchResult> Search([FromQuery] NexusSearchQuery query, CancellationToken ct) =>
        _nexus.SearchAsync(query, ct);

    [HttpGet("search/assets")]
    public Task<NexusSearchResult> SearchAssets([FromQuery] NexusSearchQuery query, CancellationToken ct) =>
        _nexus.SearchAssetsAsync(query, ct);

    [HttpPost("catalog/sync")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SyncCatalog(CancellationToken ct)
    {
        TempData["Success"] = await _adminNexus.SyncCatalogAsync(GetIp(), ct);
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("components/{id}")]
    public Task<NexusComponent> Component(string id, CancellationToken ct) =>
        _nexus.GetComponentAsync(id, ct);

    [HttpDelete("components/{id}")]
    public async Task<IActionResult> DeleteComponent(string id, CancellationToken ct)
    {
        await _nexus.DeleteComponentAsync(id, ct);
        return NoContent();
    }

    [HttpGet("assets/{id}")]
    public Task<NexusAsset> Asset(string id, CancellationToken ct) => _nexus.GetAssetAsync(id, ct);

    [HttpDelete("assets/{id}")]
    public async Task<IActionResult> DeleteAsset(string id, CancellationToken ct)
    {
        await _nexus.DeleteAssetAsync(id, ct);
        return NoContent();
    }

    [HttpPost("repositories/{repositoryName}/health-check")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RunHealthCheck(string repositoryName, CancellationToken ct)
    {
        TempData["Success"] = await _adminNexus.RunHealthCheckAsync(repositoryName, GetIp(), ct);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("repositories/{repositoryName}/health-check/stop")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StopHealthCheck(string repositoryName, CancellationToken ct)
    {
        TempData["Success"] = await _adminNexus.StopHealthCheckAsync(repositoryName, GetIp(), ct);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("assets/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAssetFromUi(string id, string? q, CancellationToken ct)
    {
        if (!await _adminNexus.DeleteAssetFromUiAsync(id, GetIp(), ct))
            return BadRequest();

        TempData["Success"] = "Nexus asset deleted.";
        return RedirectToAction(nameof(Index), new { q });
    }

    [HttpPost("components/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteComponentFromUi(string id, CancellationToken ct)
    {
        if (!await _adminNexus.DeleteComponentFromUiAsync(id, GetIp(), ct))
            return BadRequest();

        TempData["Success"] = "Nexus component deleted.";
        return RedirectToAction(nameof(Index));
    }

    private string GetIp() => HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}