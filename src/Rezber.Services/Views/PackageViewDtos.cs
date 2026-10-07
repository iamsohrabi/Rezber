using System.ComponentModel.DataAnnotations;
using Rezber.Domain.Models;
using Rezber.Services.Features;

namespace Rezber.Services.Views.Packages;

public class PackageListVDto
{
    public string Source { get; set; } = "rezber";
    public NexusCatalogPage? DockerResults { get; set; }
    public List<PackageDto> Packages { get; set; } = new();
    public List<PackageDto> PopularPackages { get; set; } = new();
    public string? Query { get; set; }
    public string Filter { get; set; } = "all";
    public string Sort { get; set; } = "recent";
    public string? SelectedLicense { get; set; }
    public string? Target { get; set; }
    public string? License { get; set; }
    public bool OnlyMine { get; set; }
    public bool OnlyRecent { get; set; }
    public int TotalCount { get; set; }
    public int RecentCount { get; set; }
    public long TotalDownloads { get; set; }
    public int MyPackagesCount { get; set; }
    public Dictionary<string, int> LangCounts { get; set; } = new();
    public Dictionary<string, int> LicenseCounts { get; set; } = new();
}

public class PackageDetailDto
{
    public Package Package { get; set; } = new() { Id = Guid.Empty };
    public bool CanEdit { get; set; }
    public int[] DailyDownloads { get; set; } = Array.Empty<int>();
    public int WeekDownloads { get; set; }
    public int TotalDownloads { get; set; }
    public string? ReadmeHtml { get; set; }
    public List<PackageReleaseDto> AvailableVersions { get; set; } = new();
}

public sealed class PackageReleaseDto
{
    public string Version { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string Size { get; set; } = string.Empty;
    public int Downloads { get; set; }
    public bool Latest { get; set; }
    public string InstallCommand { get; set; } = string.Empty;
}

public sealed class PackageRequest
{
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = "1.0.0";
    public string Lang { get; set; } = "csharp";
    public string? Target { get; set; }
    public string? TargetKey { get; set; }
    public string Description { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public string? Repository { get; set; }
    public string? Readme { get; set; }
    public string Size { get; set; } = "0 MB";
    public string? InstallCommand { get; set; }

    public Package ToPackage(string author) => new()
    {
        Id = Guid.NewGuid(),
        Name = Name.Trim(),
        Version = Version.Trim(),
        Lang = Lang.Trim(),
        Target = Target?.Trim(),
        TargetKey = TargetKey?.Trim(),
        Description = Description.Trim(),
        Tags = Tags.Where(tag => !string.IsNullOrWhiteSpace(tag)).Select(tag => tag.Trim()).ToList(),
        Author = author,
        Repository = Repository?.Trim(),
        Readme = Readme,
        Size = Size.Trim(),
        InstallCommand = InstallCommand?.Trim() ?? string.Empty
    };
}

public class PackageFormDto
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string Version { get; set; } = "1.0.0";

    [Required]
    public string Language { get; set; } = "csharp";

    [StringLength(100)]
    public string? Target { get; set; }

    [Required]
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    public string Tags { get; set; } = string.Empty;
    public string License { get; set; } = "MIT";
    [StringLength(2048)]
    public string? RepositoryUrl { get; set; }
    [StringLength(2048)]
    public string? ReadmeUrl { get; set; }
    public string? Readme { get; set; }
    public string Dependencies { get; set; } = string.Empty;

    public List<string> ParseTags() =>
        Tags.Split([',', ';', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    public List<Dependency> ParseDependencies() =>
        Dependencies.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(value => value.Split('|', 3, StringSplitOptions.TrimEntries))
            .Where(parts => parts.Length > 0 && !string.IsNullOrWhiteSpace(parts[0]))
            .Select(parts => new Dependency
            {
                Name = parts[0],
                Version = parts.Length > 1 ? parts[1] : string.Empty,
                Type = parts.Length > 2 && !string.IsNullOrWhiteSpace(parts[2]) ? parts[2] : "runtime"
            })
            .ToList();
}

public class PackageEditDto : PackageFormDto
{
    public string Id { get; set; } = string.Empty;
    public bool HasImage { get; set; }
    public bool RemoveImage { get; set; }
}
