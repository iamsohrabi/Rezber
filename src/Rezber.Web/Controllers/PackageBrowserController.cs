using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rezber.Services.Features;
using Rezber.Services.Views.Packages;

namespace Rezber.Web.Controllers;

[AllowAnonymous]
public sealed class PackageBrowserController : Controller
{
    private const string IndexView = "~/Views/Packages/Index.cshtml";
    private const string DetailsView = "~/Views/Packages/Details.cshtml";
    private const string EditView = "~/Views/Packages/Edit.cshtml";
    private const string CreateView = "~/Views/Packages/Create.cshtml";

    private readonly IPackageBrowserService _browserService;
    private readonly IPackageManagementService _managementService;

    public PackageBrowserController(
        IPackageBrowserService browserService,
        IPackageManagementService managementService)
    {
        _browserService = browserService;
        _managementService = managementService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? q,
        string? filter,
        string? license,
        string? sort,
        string? source,
        int page,
        CancellationToken ct)
    {
        var result = await _browserService.BrowseAsync(
            q, filter, license, sort, source, page, ct);
        if (result.RequiresAuthentication)
            return Challenge();

        ViewData["SearchQuery"] = q;
        return View(IndexView, result.Model);
    }

    [HttpGet]
    public async Task<IActionResult> Image(string id, CancellationToken ct)
    {
        var image = await _browserService.GetImageAsync(id, ct);
        return image == null ? NotFound() : File(image, "image/webp");
    }

    [HttpGet]
    public async Task<IActionResult> Details(string id, CancellationToken ct)
    {
        var model = await _browserService.GetDetailsAsync(id, ct);
        return model == null ? NotFound() : View(DetailsView, model);
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Edit(string id, CancellationToken ct)
    {
        var result = await _managementService.GetEditAsync(id, ct);
        return result.Status switch
        {
            PackageWorkflowStatus.Success => View(EditView, result.Value),
            PackageWorkflowStatus.NotFound => NotFound(),
            PackageWorkflowStatus.Forbidden => Forbid(),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        string id,
        PackageEditDto model,
        IFormFile? imageFile,
        CancellationToken ct)
    {
        var editAccess = await _managementService.GetEditAsync(id, ct);
        if (editAccess.Status == PackageWorkflowStatus.NotFound)
            return NotFound();
        if (editAccess.Status == PackageWorkflowStatus.Forbidden)
            return Forbid();
        model.HasImage = editAccess.Value!.HasImage;
        if (!ModelState.IsValid)
            return View(EditView, model);

        await using var imageStream = imageFile?.OpenReadStream();
        var result = await _managementService.UpdateAsync(
            id, model, imageStream, imageFile?.Length ?? 0, ct);

        if (result.Status == PackageWorkflowStatus.Success)
        {
            TempData["Success"] = $"Package {result.Value!.Name} updated.";
            return RedirectToAction(nameof(Details), new { id = result.Value.Id });
        }
        if (result.Status == PackageWorkflowStatus.NotFound)
            return NotFound();
        if (result.Status == PackageWorkflowStatus.Forbidden)
            return Forbid();
        if (result.Status == PackageWorkflowStatus.Unauthorized)
            return Unauthorized();

        model.HasImage = result.Value?.HasImage ?? model.HasImage;
        AddWorkflowErrors(result.Errors);
        return View(EditView, model);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var result = await _managementService.DeleteAsync(id, ct);
        if (result.Status == PackageWorkflowStatus.NotFound)
            return NotFound();
        if (result.Status == PackageWorkflowStatus.Forbidden)
            return Forbid();

        TempData["Success"] = $"Package {result.Value!.Name} removed from Rezber.";
        return RedirectToAction("Index", "Dashboard");
    }

    [HttpGet]
    [Authorize]
    public IActionResult Create() => View(CreateView, new PackageFormDto());

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        PackageFormDto model,
        IFormFile? packageFile,
        CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return View(CreateView, model);

        await using var packageStream = packageFile?.OpenReadStream();
        var result = await _managementService.CreateAsync(
            model,
            packageStream,
            packageFile?.Length ?? 0,
            packageFile?.FileName ?? string.Empty,
            ct);

        if (result.Status == PackageWorkflowStatus.Success)
            return RedirectToAction(nameof(Details), new { id = result.Value!.Id });
        if (result.Status == PackageWorkflowStatus.Unauthorized)
            return Unauthorized();

        AddWorkflowErrors(result.Errors);
        return View(CreateView, model);
    }

    private void AddWorkflowErrors(IEnumerable<PackageWorkflowError>? errors)
    {
        if (errors == null)
            return;

        foreach (var error in errors)
            ModelState.AddModelError(error.Key, error.Message);
    }
}