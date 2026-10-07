using Microsoft.Extensions.DependencyInjection;
using Rezber.Core.Attributs;
using Rezber.Domain.Models;
using Rezber.Services.Helpers;

namespace Rezber.Services.Features;

public interface IPackagePushService
{
    Task<PackageWorkflowResult<string>> PushAsync(
        string plainTextToken,
        string packageName,
        string version,
        string fileName,
        Stream body,
        long? contentLength,
        CancellationToken ct = default);
}

[AutoInject(ServiceLifetime.Scoped)]
public sealed class PackagePushService : IPackagePushService
{
    private readonly IApiTokenService _tokenService;
    private readonly INexusRepositoryClient _nexus;
    private readonly IUserService _userService;
    private readonly IPackageService _packageService;

    public PackagePushService(
        IApiTokenService tokenService,
        INexusRepositoryClient nexus,
        IUserService userService,
        IPackageService packageService)
    {
        _tokenService = tokenService;
        _nexus = nexus;
        _userService = userService;
        _packageService = packageService;
    }

    public async Task<PackageWorkflowResult<string>> PushAsync(
        string plainTextToken,
        string packageName,
        string version,
        string fileName,
        Stream body,
        long? contentLength,
        CancellationToken ct = default)
    {
        var token = await _tokenService.ValidateAsync(plainTextToken, ct);
        if (token == null)
            return new(PackageWorkflowStatus.Unauthorized, Message: "A valid Rezber API token is required.");
        if (contentLength is 0)
            return new(PackageWorkflowStatus.Invalid, Message: "The package body is empty.");

        var user = await _userService.GetByIdAsync(token.UserId, ct);
        if (user == null)
            return new(PackageWorkflowStatus.Unauthorized);
        if (await _packageService.ExistsAsync(packageName, version))
            return new(PackageWorkflowStatus.Conflict, Message: "This package version is already registered.");

        await _nexus.UploadPackageAsync(body, packageName, version, fileName, ct);
        var language = DetectLanguage(fileName);
        await _packageService.CreateNewPackagesync(new Package
        {
            Id = Guid.NewGuid(),
            Name = packageName,
            Version = version,
            Lang = language,
            Author = user.Email,
            AuthorRole = user.JobTitle,
            Size = contentLength is long length
                ? $"{Math.Round(length / 1024d / 1024d, 2):0.##} MB"
                : "0 MB",
            Repository = fileName,
            InstallCommand = BuilderCommands.GetCommand(packageName, language, version)
        }, token.UserId);

        return new(PackageWorkflowStatus.Success);
    }

    private static string DetectLanguage(string fileName) =>
        Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".nupkg" or ".cs" or ".csproj" => "csharp",
            ".js" or ".mjs" or ".cjs" => "javascript",
            ".ts" or ".tsx" => "typescript",
            ".py" => "python",
            ".go" => "go",
            ".fs" or ".fsproj" => "fsharp",
            ".vb" or ".vbproj" => "visualbasic",
            ".java" => "java",
            ".rs" => "rust",
            _ => "generic"
        };
}