using Microsoft.Extensions.DependencyInjection;
using Rezber.Core.Attributs;
using Rezber.Domain.Models;
using Rezber.Services.Views.Packages;

namespace Rezber.Services.Features;

public sealed record PackageApiStats(Dictionary<string, int> Languages, int TotalDownloads);

public interface IPackageApiService
{
    Task<PackageWorkflowResult<IReadOnlyCollection<PackageDto>>> GetAllAsync(PackageFilter filter, CancellationToken ct = default);
    Task<PackageWorkflowResult<PackageDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PackageWorkflowResult<byte[]>> DownloadAsync(string packageName, string version, CancellationToken ct = default);
    Task<PackageApiStats> GetStatsAsync(CancellationToken ct = default);
    Task<PackageWorkflowResult<PackageDto>> CreateAsync(PackageRequest request, CancellationToken ct = default);
    Task<PackageWorkflowResult<string>> UpdateAsync(Guid id, PackageRequest request, CancellationToken ct = default);
    Task<PackageWorkflowResult<string>> DeleteAsync(Guid id, CancellationToken ct = default);
}

[AutoInject(ServiceLifetime.Scoped)]
public sealed class PackageApiService : IPackageApiService
{
    private readonly IPackageService _packageService;
    private readonly IUserService _userService;
    private readonly INexusRepositoryClient _nexus;

    public PackageApiService(IPackageService packageService, IUserService userService, INexusRepositoryClient nexus)
    {
        _packageService = packageService;
        _userService = userService;
        _nexus = nexus;
    }

    public async Task<PackageWorkflowResult<IReadOnlyCollection<PackageDto>>> GetAllAsync(
        PackageFilter filter,
        CancellationToken ct = default)
    {
        if (filter.OnlyMine)
        {
            var user = await _userService.GetCurrentUserAsync(ct);
            if (!user.IsActive)
                return new(PackageWorkflowStatus.Unauthorized);
            filter.CurrentUser = user.Email;
        }

        return new(PackageWorkflowStatus.Success, await _packageService.FilterAsync(filter));
    }

    public async Task<PackageWorkflowResult<PackageDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var package = await _packageService.GetByIdAsync(id);
        return package == null
            ? new(PackageWorkflowStatus.NotFound)
            : new(PackageWorkflowStatus.Success, package);
    }

    public async Task<PackageWorkflowResult<byte[]>> DownloadAsync(
        string packageName,
        string version,
        CancellationToken ct = default)
    {
        if (!await _packageService.ExistsAsync(packageName, version))
            return new(PackageWorkflowStatus.NotFound);

        var content = await _nexus.DownloadPackageAsync(packageName, version, ct);
        return content == null
            ? new(PackageWorkflowStatus.NotFound, Message: "The package file was not found in the repository.")
            : new(PackageWorkflowStatus.Success, content);
    }

    public async Task<PackageApiStats> GetStatsAsync(CancellationToken ct = default) => new(
        await _packageService.GetLangCountsAsync(),
        await _packageService.GetTotalDownloadsAsync());

    public async Task<PackageWorkflowResult<PackageDto>> CreateAsync(PackageRequest request, CancellationToken ct = default)
    {
        var currentUser = await _userService.GetCurrentUserAsync(ct);
        if (!Guid.TryParse(currentUser.Id, out var createdBy))
            return new(PackageWorkflowStatus.Unauthorized);

        var name = request.Name.Trim();
        var version = request.Version.Trim();
        if (await _packageService.ExistsAsync(name, version))
            return new(PackageWorkflowStatus.Conflict, Message: "This package version is already registered.");

        var package = request.ToPackage(currentUser.Email);
        var created = await _packageService.CreateNewPackagesync(package, createdBy);
        await _userService.RefreshUserStatsAsync(createdBy, ct);
        return new(PackageWorkflowStatus.Success, created);
    }

    public async Task<PackageWorkflowResult<string>> UpdateAsync(
        Guid id,
        PackageRequest request,
        CancellationToken ct = default)
    {
        var currentUser = await _userService.GetCurrentUserAsync(ct);
        var existing = await _packageService.GetByIdAsync(id.ToString(), ct);
        if (existing == null)
            return new(PackageWorkflowStatus.NotFound);
        if (!currentUser.IsActive)
            return new(PackageWorkflowStatus.Unauthorized);
        if (!currentUser.IsAdmin && !string.Equals(existing.Author, currentUser.Email, StringComparison.OrdinalIgnoreCase))
            return new(PackageWorkflowStatus.Forbidden);
        if (!Guid.TryParse(currentUser.Id, out var modifiedBy))
            return new(PackageWorkflowStatus.Unauthorized);

        var package = request.ToPackage(existing.Author);
        package.Id = id;
        if (!await _packageService.UpdateAsync(package, modifiedBy))
            return new(PackageWorkflowStatus.NotFound);

        await RefreshOwnerStatsAsync(existing.Author, ct);
        return new(PackageWorkflowStatus.Success);
    }

    public async Task<PackageWorkflowResult<string>> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var currentUser = await _userService.GetCurrentUserAsync(ct);
        var package = await _packageService.GetByIdAsync(id.ToString(), ct);
        if (package == null)
            return new(PackageWorkflowStatus.NotFound);
        if (!currentUser.IsActive)
            return new(PackageWorkflowStatus.Unauthorized);
        if (!currentUser.IsAdmin && !string.Equals(package.Author, currentUser.Email, StringComparison.OrdinalIgnoreCase))
            return new(PackageWorkflowStatus.Forbidden);
        if (!await _packageService.DeleteAsync(id))
            return new(PackageWorkflowStatus.NotFound);

        await RefreshOwnerStatsAsync(package.Author, ct);
        return new(PackageWorkflowStatus.Success);
    }

    private async Task RefreshOwnerStatsAsync(string email, CancellationToken ct)
    {
        var owner = await _userService.GetByEmailAsync(email, ct);
        if (owner != null && Guid.TryParse(owner.Id, out var ownerId))
            await _userService.RefreshUserStatsAsync(ownerId, ct);
    }
}