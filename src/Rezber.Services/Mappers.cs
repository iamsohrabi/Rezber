
using System.Reflection;
using Rezber.Domain.Models;

namespace Rezber.Services;

public static class Mappers
{
    public static IReadOnlyCollection<PackageDto> ToDToList(this IReadOnlyCollection<Package> packages)
        => packages.Select(c => c.ToDto()).ToList();

    public static PackageDto ToDto(this Package package)
        => new PackageDto
        {
            Id = package.Id.ToString(),
            Name = package.Name,
            Version = package.Version,
            Lang = package.Lang,
            Target = package.Target,
            TargetKey = package.TargetKey,
            Description = package.Description,
            Downloads = package.Downloads,
            License = package.License,
            Tags = package.Tags,
            Repository = package.Repository,
            Author = package.Author,
            Readme = package.Readme,
            Size = package.Size,
            InstallCommand = package.InstallCommand,
            HasImage = package.ImageWebp is { Length: > 0 }
        };
}
