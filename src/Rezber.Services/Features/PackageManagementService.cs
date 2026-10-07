using Microsoft.Extensions.DependencyInjection;
using Rezber.Core.Attributs;
using Rezber.Domain.Identity;
using Rezber.Domain.Models;
using Rezber.Services.Helpers;
using Rezber.Services.Views.Packages;

namespace Rezber.Services.Features;

public enum PackageWorkflowStatus
{
    Success,
    NotFound,
    Forbidden,
    Unauthorized,
    Conflict,
    Invalid
}

public sealed record PackageWorkflowError(string Key, string Message);
public sealed record PackageOperationData(string Id, string Name, bool HasImage = false);
public sealed record PackageWorkflowResult<T>(
    PackageWorkflowStatus Status,
    T? Value = default,
    IReadOnlyCollection<PackageWorkflowError>? Errors = null,
    string? Message = null);

public interface IPackageManagementService
{
    Task<PackageWorkflowResult<PackageEditDto>> GetEditAsync(string id, CancellationToken ct = default);
    Task<PackageWorkflowResult<PackageOperationData>> UpdateAsync(
        string id,
        PackageEditDto model,
        Stream? imageStream,
        long imageLength,
        CancellationToken ct = default);
    Task<PackageWorkflowResult<PackageOperationData>> DeleteAsync(string id, CancellationToken ct = default);
    Task<PackageWorkflowResult<PackageOperationData>> CreateAsync(
        PackageFormDto model,
        Stream? packageStream,
        long packageLength,
        string fileName,
        CancellationToken ct = default);
}

[AutoInject(ServiceLifetime.Scoped)]
public sealed class PackageManagementService : IPackageManagementService
{
    private readonly IPackageService _packageService;
    private readonly IUserService _userService;
    private readonly INexusRepositoryClient _nexus;
    private readonly IRepositoryReadmeService _readmeService;
    private readonly IPackageImageService _packageImageService;

    public PackageManagementService(
        IPackageService packageService,
        IUserService userService,
        INexusRepositoryClient nexus,
        IRepositoryReadmeService readmeService,
        IPackageImageService packageImageService)
    {
        _packageService = packageService;
        _userService = userService;
        _nexus = nexus;
        _readmeService = readmeService;
        _packageImageService = packageImageService;
    }

    public async Task<PackageWorkflowResult<PackageEditDto>> GetEditAsync(
        string id,
        CancellationToken ct = default)
    {
        var package = await _packageService.GetByIdAsync(id, ct);
        if (package == null)
            return new(PackageWorkflowStatus.NotFound);
        if (!await CanManageAsync(package, ct))
            return new(PackageWorkflowStatus.Forbidden);

        return new(PackageWorkflowStatus.Success, new PackageEditDto
        {
            Id = package.Id.ToString(),
            Name = package.Name,
            Version = package.Version,
            Language = package.Lang,
            Target = package.Target,
            Description = package.Description,
            Tags = string.Join(", ", package.Tags),
            License = package.License,
            RepositoryUrl = package.Repository,
            ReadmeUrl = package.ReadmeUrl,
            Readme = package.Readme,
            HasImage = package.ImageWebp is { Length: > 0 },
            Dependencies = string.Join(Environment.NewLine, package.Dependencies.Select(dependency =>
                $"{dependency.Name} | {dependency.Version} | {dependency.Type}"))
        });
    }

    public async Task<PackageWorkflowResult<PackageOperationData>> UpdateAsync(
        string id,
        PackageEditDto model,
        Stream? imageStream,
        long imageLength,
        CancellationToken ct = default)
    {
        var existing = await _packageService.GetByIdAsync(id, ct);
        if (existing == null)
            return new(PackageWorkflowStatus.NotFound);
        if (!await CanManageAsync(existing, ct))
            return new(PackageWorkflowStatus.Forbidden);

        var errors = new List<PackageWorkflowError>();
        ValidateRepositoryUrls(model.RepositoryUrl, model.ReadmeUrl, errors);
        if (errors.Count > 0)
            return Invalid<PackageOperationData>(errors, value: new(existing.Id.ToString(), existing.Name, existing.ImageWebp is { Length: > 0 }));

        var imageWebp = existing.ImageWebp;
        var imageLookupAttempted = existing.ImageLookupAttempted;
        if (imageLength > 0)
        {
            if (imageLength > 5 * 1024 * 1024)
            {
                errors.Add(new(string.Empty, "Image files must be 5 MB or smaller."));
            }
            else if (imageStream != null)
            {
                try
                {
                    imageWebp = await _packageImageService.ConvertToWebpAsync(imageStream, ct);
                    imageLookupAttempted = true;
                }
                catch (InvalidDataException exception)
                {
                    errors.Add(new(string.Empty, exception.Message));
                }
            }
        }
        else if (model.RemoveImage)
        {
            imageWebp = null;
            imageLookupAttempted = true;
        }

        if (errors.Count > 0)
            return Invalid<PackageOperationData>(errors, value: new(existing.Id.ToString(), existing.Name, existing.ImageWebp is { Length: > 0 }));

        if (!string.IsNullOrWhiteSpace(model.RepositoryUrl) && string.IsNullOrWhiteSpace(model.Readme))
        {
            try
            {
                model.Readme = await _readmeService.FetchAsync(model.RepositoryUrl, model.ReadmeUrl, ct);
            }
            catch (HttpRequestException)
            {
                errors.Add(new(nameof(model.RepositoryUrl), "Could not fetch the README. Check the repository URL or paste the README manually."));
            }
            catch (InvalidOperationException exception)
            {
                errors.Add(new(nameof(model.RepositoryUrl), exception.Message));
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                errors.Add(new(nameof(model.RepositoryUrl), "Fetching the README timed out. Paste it manually and try again."));
            }
        }

        if (errors.Count > 0)
            return Invalid<PackageOperationData>(errors, value: new(existing.Id.ToString(), existing.Name, existing.ImageWebp is { Length: > 0 }));

        var editor = await _userService.GetCurrentUserAsync(ct);
        if (!Guid.TryParse(editor.Id, out var modifiedBy))
            return new(PackageWorkflowStatus.Unauthorized);

        var updated = new Package
        {
            Id = existing.Id,
            Name = model.Name.Trim(),
            Version = existing.Version,
            Lang = existing.Lang,
            Target = model.Target?.Trim(),
            TargetKey = existing.TargetKey,
            Description = model.Description.Trim(),
            ImageWebp = imageWebp,
            ImageLookupAttempted = imageLookupAttempted,
            Tags = model.ParseTags(),
            License = model.License.Trim(),
            Repository = model.RepositoryUrl?.Trim(),
            ReadmeUrl = model.ReadmeUrl?.Trim(),
            Readme = model.Readme,
            Dependencies = model.ParseDependencies(),
            Author = existing.Author,
            AuthorRole = existing.AuthorRole,
            Size = existing.Size,
            InstallCommand = existing.InstallCommand
        };

        if (!await _packageService.UpdateAsync(updated, modifiedBy))
            return new(PackageWorkflowStatus.NotFound);

        await RefreshOwnerStatsAsync(existing.Author, ct);
        return new(PackageWorkflowStatus.Success, new(updated.Id.ToString(), updated.Name));
    }

    public async Task<PackageWorkflowResult<PackageOperationData>> DeleteAsync(
        string id,
        CancellationToken ct = default)
    {
        var package = await _packageService.GetByIdAsync(id, ct);
        if (package == null)
            return new(PackageWorkflowStatus.NotFound);
        if (!await CanManageAsync(package, ct))
            return new(PackageWorkflowStatus.Forbidden);

        await _packageService.DeleteAsync(package.Id);
        await RefreshOwnerStatsAsync(package.Author, ct);
        return new(PackageWorkflowStatus.Success, new(package.Id.ToString(), package.Name));
    }

    public async Task<PackageWorkflowResult<PackageOperationData>> CreateAsync(
        PackageFormDto model,
        Stream? packageStream,
        long packageLength,
        string fileName,
        CancellationToken ct = default)
    {
        var errors = new List<PackageWorkflowError>();
        if (packageStream == null || packageLength == 0)
            errors.Add(new(string.Empty, "A package file is required."));
        if (errors.Count > 0)
            return Invalid<PackageOperationData>(errors);

        var name = model.Name.Trim();
        var version = model.Version.Trim();
        if (await _packageService.ExistsAsync(name, version))
            return Invalid<PackageOperationData>([new(nameof(model.Version), "This package version is already registered.")], PackageWorkflowStatus.Conflict);

        ValidateRepositoryUrls(model.RepositoryUrl, model.ReadmeUrl, errors);
        if (errors.Count > 0)
            return Invalid<PackageOperationData>(errors);

        if (!string.IsNullOrWhiteSpace(model.RepositoryUrl) && string.IsNullOrWhiteSpace(model.Readme))
        {
            try
            {
                model.Readme = await _readmeService.FetchAsync(model.RepositoryUrl, model.ReadmeUrl, ct);
            }
            catch (HttpRequestException)
            {
                errors.Add(new(nameof(model.RepositoryUrl), "Could not fetch the README. Check the repository URL or paste the README manually."));
            }
            catch (InvalidOperationException exception)
            {
                errors.Add(new(nameof(model.RepositoryUrl), exception.Message));
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                errors.Add(new(nameof(model.RepositoryUrl), "Fetching the README timed out. Paste it manually and try again."));
            }
        }

        if (errors.Count > 0)
            return Invalid<PackageOperationData>(errors);

        var currentUser = await _userService.GetCurrentUserAsync(ct);
        if (!Guid.TryParse(currentUser.Id, out var createdBy))
            return new(PackageWorkflowStatus.Unauthorized);

        try
        {
            await _nexus.UploadPackageAsync(packageStream!, name, version, fileName, ct);
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException)
        {
            return Invalid<PackageOperationData>([new(string.Empty, $"Package upload failed: {exception.Message}")]);
        }

        var package = new Package
        {
            Id = Guid.NewGuid(),
            Name = name,
            Version = version,
            Lang = model.Language.Trim(),
            Target = model.Target?.Trim(),
            Description = model.Description.Trim(),
            Tags = model.ParseTags(),
            License = model.License?.Trim() ?? "MIT",
            Size = $"{Math.Round(packageLength / 1024d / 1024d, 2):0.##} MB",
            Repository = model.RepositoryUrl?.Trim(),
            ReadmeUrl = model.ReadmeUrl?.Trim(),
            Readme = model.Readme,
            Dependencies = model.ParseDependencies(),
            Author = currentUser.Email,
            AuthorRole = currentUser.JobTitle
        };

        var created = await _packageService.CreateNewPackagesync(package, createdBy);
        await _userService.RefreshUserStatsAsync(createdBy, ct);
        return new(PackageWorkflowStatus.Success, new(created.Id, created.Name));
    }

    private async Task<bool> CanManageAsync(Package package, CancellationToken ct)
    {
        var currentUser = await _userService.GetCurrentUserAsync(ct);
        return currentUser.IsActive && (currentUser.IsAdmin || string.Equals(
            package.Author, currentUser.Email, StringComparison.OrdinalIgnoreCase));
    }

    private async Task RefreshOwnerStatsAsync(string email, CancellationToken ct)
    {
        var owner = await _userService.GetByEmailAsync(email, ct);
        if (owner != null && Guid.TryParse(owner.Id, out var ownerId))
            await _userService.RefreshUserStatsAsync(ownerId, ct);
    }

    private static void ValidateRepositoryUrls(
        string? repositoryUrl,
        string? readmeUrl,
        ICollection<PackageWorkflowError> errors)
    {
        if (!string.IsNullOrWhiteSpace(repositoryUrl) && !IsValidRepositoryUrl(repositoryUrl))
            errors.Add(new(nameof(PackageFormDto.RepositoryUrl), "Use a valid HTTPS repository URL."));

        if (!IsValidReadmeUrl(repositoryUrl, readmeUrl))
            errors.Add(new(nameof(PackageFormDto.ReadmeUrl), "README URL must use HTTPS and the same host and port as the repository URL."));
    }

    private static bool IsValidRepositoryUrl(string? value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
        string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) &&
        string.IsNullOrEmpty(uri.UserInfo);

    private static bool IsValidReadmeUrl(string? repositoryUrl, string? readmeUrl)
    {
        if (string.IsNullOrWhiteSpace(readmeUrl))
            return true;

        return IsValidRepositoryUrl(repositoryUrl) &&
            Uri.TryCreate(repositoryUrl, UriKind.Absolute, out var repositoryUri) &&
            Uri.TryCreate(readmeUrl, UriKind.Absolute, out var readmeUri) &&
            string.Equals(readmeUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) &&
            string.IsNullOrEmpty(readmeUri.UserInfo) &&
            string.Equals(repositoryUri.Authority, readmeUri.Authority, StringComparison.OrdinalIgnoreCase);
    }

    private static PackageWorkflowResult<T> Invalid<T>(
        IReadOnlyCollection<PackageWorkflowError> errors,
        PackageWorkflowStatus status = PackageWorkflowStatus.Invalid,
        T? value = default) => new(status, value, errors);
}