using Microsoft.AspNetCore.Mvc;
using Rezber.Services.Features;

namespace Rezber.Web.Controllers;

[ApiController]
[Route("api/packages/push")]
public sealed class PackagePushController(IPackagePushService packagePush) : ControllerBase
{
    [HttpPut("{packageName}/{version}/{fileName}")]
    [RequestSizeLimit(100_000_000)]
    public async Task<IActionResult> Push(
        string packageName,
        string version,
        string fileName,
        CancellationToken ct)
    {
        var plainTextToken = Request.Headers["X-Rezber-ApiKey"].FirstOrDefault()
            ?? Request.Headers["X-Nexora-ApiKey"].FirstOrDefault()
            ?? Request.Headers["X-Api-Key"].FirstOrDefault()
            ?? string.Empty;
        var result = await packagePush.PushAsync(
            plainTextToken, packageName, version, fileName, Request.Body, Request.ContentLength, ct);

        return result.Status switch
        {
            PackageWorkflowStatus.Unauthorized => Unauthorized(result.Message),
            PackageWorkflowStatus.Invalid => BadRequest(result.Message),
            PackageWorkflowStatus.Conflict => Conflict(result.Message),
            _ => NoContent()
        };
    }
}