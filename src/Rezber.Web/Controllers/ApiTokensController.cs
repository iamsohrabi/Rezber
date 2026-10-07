using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rezber.Services.Features;

namespace Rezber.Web.Controllers;

[ApiController]
[Authorize]
[Route("api/tokens")]
public sealed class ApiTokensController(IApiTokenManagementService tokenManagement) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var result = await tokenManagement.GetAsync(ct);
        return result.Status == PackageWorkflowStatus.Unauthorized
            ? Unauthorized()
            : Ok(result.Value);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateApiTokenRequest request, CancellationToken ct)
    {
        var result = await tokenManagement.CreateAsync(request.Name, ct);
        return result.Status == PackageWorkflowStatus.Unauthorized
            ? Unauthorized()
            : Ok(result.Value);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Revoke(Guid id, CancellationToken ct)
    {
        var result = await tokenManagement.RevokeAsync(id, ct);
        return result.Status switch
        {
            PackageWorkflowStatus.Unauthorized => Unauthorized(),
            PackageWorkflowStatus.NotFound => NotFound(),
            _ => NoContent()
        };
    }
}

public sealed class CreateApiTokenRequest
{
    public string? Name { get; set; }
}