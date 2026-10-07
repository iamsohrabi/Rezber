using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rezber.Services.Features;

namespace Rezber.Web.Controllers;

[ApiController]
[Authorize]
[Route("api/nexus/packages")]
public sealed class NexusCatalogController(INexusCatalogService catalog) : ControllerBase
{
    [HttpGet]
    public Task<NexusCatalogPage> Search([FromQuery] NexusCatalogQuery query, CancellationToken ct) =>
        catalog.SearchAsync(query, ct);
}