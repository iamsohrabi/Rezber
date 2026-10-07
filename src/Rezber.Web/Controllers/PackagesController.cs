using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rezber.Services.Features;
using Rezber.Services.Views.Packages;

namespace Rezber.Web.Controllers;

[ApiController]
[Route("api/packages")]
public sealed class PackagesController(IPackageApiService packageApi) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll([FromQuery] PackageFilter filter, CancellationToken ct)
    {
        var result = await packageApi.GetAllAsync(filter, ct);
        return result.Status == PackageWorkflowStatus.Unauthorized
            ? Unauthorized()
            : Ok(result.Value);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await packageApi.GetByIdAsync(id, ct);
        return result.Status == PackageWorkflowStatus.NotFound ? NotFound() : Ok(result.Value);
    }

    [HttpGet("{packageName}/{version}/download")]
    [AllowAnonymous]
    public async Task<IActionResult> Download(string packageName, string version, CancellationToken ct)
    {
        var result = await packageApi.DownloadAsync(packageName, version, ct);
        if (result.Status == PackageWorkflowStatus.NotFound)
            return result.Message == null ? NotFound() : NotFound(result.Message);

        return File(result.Value!, "application/octet-stream", $"{packageName}.{version}.nupkg");
    }

    [HttpGet("stats")]
    [AllowAnonymous]
    public async Task<IActionResult> GetStats(CancellationToken ct)
    {
        var result = await packageApi.GetStatsAsync(ct);
        return Ok(new { languages = result.Languages, totalDownloads = result.TotalDownloads });
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] PackageRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var result = await packageApi.CreateAsync(request, ct);
        return result.Status switch
        {
            PackageWorkflowStatus.Unauthorized => Unauthorized(),
            PackageWorkflowStatus.Conflict => Conflict(result.Message),
            _ => CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
        };
    }

    [HttpPut("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Update(Guid id, [FromBody] PackageRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var result = await packageApi.UpdateAsync(id, request, ct);
        return result.Status switch
        {
            PackageWorkflowStatus.NotFound => NotFound(),
            PackageWorkflowStatus.Forbidden => Forbid(),
            PackageWorkflowStatus.Unauthorized => Unauthorized(),
            _ => NoContent()
        };
    }

    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await packageApi.DeleteAsync(id, ct);
        return result.Status switch
        {
            PackageWorkflowStatus.NotFound => NotFound(),
            PackageWorkflowStatus.Forbidden => Forbid(),
            _ => NoContent()
        };
    }
}