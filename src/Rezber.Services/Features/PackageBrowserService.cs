using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Rezber.Core.Attributs;
using Rezber.Domain.Models;
using Rezber.Services.Helpers;
using Rezber.Services.Views.Packages;

namespace Rezber.Services.Features;

public interface IPackageBrowserService
{
    Task<PackageBrowserResult> BrowseAsync(
        string? query,
        string? filter,
        string? license,
        string? sort,
        string? source,
        int page,
        CancellationToken ct = default);
    Task<byte[]?> GetImageAsync(string id, CancellationToken ct = default);
    Task<PackageDetailDto?> GetDetailsAsync(string id, CancellationToken ct = default);
}

public interface IPackageImageService
{
    Task<byte[]> ConvertToWebpAsync(Stream source, CancellationToken ct);
    Task<byte[]?> GetNugetIconWebpAsync(string packageId, CancellationToken ct);
}

public sealed record PackageBrowserResult(bool RequiresAuthentication, PackageListVDto Model);

[AutoInject(ServiceLifetime.Scoped)]
public sealed class PackageBrowserService : IPackageBrowserService
{
    private readonly IPackageService _packageService;
    private readonly INexusCatalogService _nexusCatalog;
    private readonly IUserService _userService;
    private readonly IPackageImageService _packageImageService;
    private readonly IRepositoryReadmeService _readmeService;
    private readonly ILogger<PackageBrowserService> _logger;

    public PackageBrowserService(
        IPackageService packageService,
        INexusCatalogService nexusCatalog,
        IUserService userService,
        IPackageImageService packageImageService,
        IRepositoryReadmeService readmeService,
        ILogger<PackageBrowserService> logger)
    {
        _packageService = packageService;
        _nexusCatalog = nexusCatalog;
        _userService = userService;
        _packageImageService = packageImageService;
        _readmeService = readmeService;
        _logger = logger;
    }

    public async Task<PackageBrowserResult> BrowseAsync(
        string? query,
        string? filter,
        string? license,
        string? sort,
        string? source,
        int page,
        CancellationToken ct = default)
    {
        var selectedSource = source == "docker-hosted" ? "docker-hosted" : "rezber";
        var currentUser = selectedSource == "docker-hosted"
            ? await _userService.GetCurrentUserAsync(ct)
            : null;
        var requiresAuthentication = selectedSource == "docker-hosted" && currentUser?.IsActive != true;
        var langCounts = await _packageService.GetLangCountsAsync();
        var selectedFilter = string.IsNullOrWhiteSpace(filter) ? "all" : filter.Trim();
        if (!langCounts.ContainsKey(selectedFilter))
            selectedFilter = "all";

        var licenseCounts = await _packageService.GetLicenseCountsAsync();
        var selectedLicense = string.IsNullOrWhiteSpace(license) ? null : license.Trim();
        if (selectedLicense != null && !licenseCounts.ContainsKey(selectedLicense))
            selectedLicense = null;

        var selectedSort = sort is "downloads" or "oldest" or "name" ? sort : "recent";
        var dockerResults = selectedSource == "docker-hosted"
            ? await _nexusCatalog.SearchAsync(new NexusCatalogQuery
            {
                Query = query,
                Repository = "docker-hosted",
                Format = "docker",
                Page = page,
                PageSize = 25
            }, ct)
            : null;
        var packages = selectedSource == "docker-hosted"
            ? Array.Empty<PackageDto>()
            : await _packageService.FilterAsync(new PackageFilter
            {
                Query = query,
                Filter = selectedFilter,
                License = selectedLicense,
                Sort = selectedSort
            });

        var csharpLanguage = langCounts.Keys.FirstOrDefault(language =>
            language.Equals("csharp", StringComparison.OrdinalIgnoreCase)
            || language.Equals("c#", StringComparison.OrdinalIgnoreCase)
            || language.Contains(".net", StringComparison.OrdinalIgnoreCase)
            || language.Contains("dotnet", StringComparison.OrdinalIgnoreCase));
        var popularPackages = selectedSource == "rezber"
            && string.IsNullOrWhiteSpace(query)
            && selectedFilter == "all"
            && csharpLanguage != null
                ? (await _packageService.FilterAsync(new PackageFilter
                {
                    Filter = csharpLanguage,
                    Sort = "downloads"
                })).Take(8).ToList()
                : new List<PackageDto>();

        return new PackageBrowserResult(requiresAuthentication, new PackageListVDto
        {
            Packages = packages.ToList(),
            Source = selectedSource,
            DockerResults = dockerResults,
            PopularPackages = popularPackages,
            Query = query,
            Filter = selectedFilter,
            SelectedLicense = selectedLicense,
            Sort = selectedSort,
            TotalCount = dockerResults == null
                ? packages.Count
                : (int)Math.Min(int.MaxValue, dockerResults.TotalCount),
            TotalDownloads = packages.Sum(package => (long)package.Downloads),
            LangCounts = langCounts,
            LicenseCounts = licenseCounts
        });
    }

    public async Task<byte[]?> GetImageAsync(string id, CancellationToken ct = default)
    {
        var package = await _packageService.GetByIdAsync(id, ct);
        if (package == null)
            return null;

        if (package.ImageWebp is { Length: > 0 })
            return package.ImageWebp;
        if (package.ImageLookupAttempted)
            return null;

        if (!string.Equals(package.Lang, "csharp", StringComparison.OrdinalIgnoreCase))
        {
            await _packageService.UpdateImageAsync(package.Id, null, lookupAttempted: true);
            return null;
        }

        byte[]? image;
        try
        {
            image = await _packageImageService.GetNugetIconWebpAsync(package.Name, ct);
        }
        catch (HttpRequestException)
        {
            image = null;
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            image = null;
        }

        await _packageService.UpdateImageAsync(package.Id, image, lookupAttempted: true);
        return image is { Length: > 0 } ? image : null;
    }

    public async Task<PackageDetailDto?> GetDetailsAsync(string id, CancellationToken ct = default)
    {
        var package = await _packageService.GetByIdAsync(id, ct);
        if (package == null)
            return null;

        if (PackageReadmeRenderer.IsHtmlDocument(package.Readme))
        {
            try
            {
                package.Readme = string.IsNullOrWhiteSpace(package.Repository)
                    ? null
                    : await _readmeService.FetchAsync(package.Repository, null, ct);
            }
            catch (HttpRequestException exception)
            {
                _logger.LogWarning(exception, "Could not refresh README for package {PackageId}.", package.Id);
                package.Readme = null;
            }
            catch (InvalidOperationException exception)
            {
                _logger.LogWarning(exception, "Could not refresh README for package {PackageId}.", package.Id);
                package.Readme = null;
            }
            catch (OperationCanceledException exception) when (!ct.IsCancellationRequested)
            {
                _logger.LogWarning(exception, "Timed out while refreshing README for package {PackageId}.", package.Id);
                package.Readme = null;
            }
        }

        var currentUser = await _userService.GetCurrentUserAsync(ct);
        var canEdit = currentUser.IsActive && (currentUser.IsAdmin || string.Equals(
            package.Author, currentUser.Email, StringComparison.OrdinalIgnoreCase));
        var dailyDownloads = package.DailyDownloads.ToArray();
        var availableVersions = package.Versions
            .GroupBy(version => version.Version, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.OrderByDescending(version => version.Date).First())
            .ToList();

        if (!availableVersions.Any(version => string.Equals(
                version.Version,
                package.Version,
                StringComparison.OrdinalIgnoreCase)))
        {
            availableVersions.Add(new PackageVersion
            {
                Version = package.Version,
                Date = package.Created != default ? package.Created : package.LastModified,
                Size = package.Size,
                Downloads = package.Downloads,
                Latest = true
            });
        }

        var releases = availableVersions
            .OrderByDescending(version => string.Equals(version.Version, package.Version, StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(version => version.Date)
            .Select(version => new PackageReleaseDto
            {
                Version = version.Version,
                Date = version.Date,
                Size = version.Size,
                Downloads = version.Downloads,
                Latest = version.Latest || string.Equals(version.Version, package.Version, StringComparison.OrdinalIgnoreCase),
                InstallCommand = BuilderCommands.GetCommand(package.Name, package.Lang, version.Version)
            })
            .ToList();
        package.InstallCommand = releases.FirstOrDefault()?.InstallCommand
            ?? BuilderCommands.GetCommand(package.Name, package.Lang, package.Version);

        return new PackageDetailDto
        {
            Package = package,
            CanEdit = canEdit,
            DailyDownloads = dailyDownloads,
            WeekDownloads = dailyDownloads.TakeLast(7).Sum(),
            TotalDownloads = package.Downloads,
            AvailableVersions = releases,
            ReadmeHtml = string.IsNullOrWhiteSpace(package.Readme)
                ? null
                : PackageReadmeRenderer.ToSafeHtml(package.Readme)
        };
    }
}

public interface IRepositoryReadmeService
{
    Task<string?> FetchAsync(string repositoryUrl, string? readmeUrl, CancellationToken ct = default);
}