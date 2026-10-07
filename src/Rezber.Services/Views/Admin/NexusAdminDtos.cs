using Rezber.Services.Features;

namespace Rezber.Services.Views.Admin;

public sealed class NexusAdminQuery
{
    public string? Query { get; set; }
    public string? SearchType { get; set; }
    public string? Repository { get; set; }
    public string? Format { get; set; }
    public string? Name { get; set; }
    public string? Version { get; set; }
    public string? Group { get; set; }
    public string? RepositoryName { get; set; }
    public string? ComponentId { get; set; }
    public string? AssetId { get; set; }
}

public sealed class NexusAdminDto
{
    public NexusStatus? Status { get; set; }
    public bool? Writable { get; set; }
    public IReadOnlyCollection<NexusRepository> Repositories { get; set; } = Array.Empty<NexusRepository>();
    public NexusSearchResult? SearchResult { get; set; }
    public NexusRepository? Repository { get; set; }
    public NexusComponent? Component { get; set; }
    public NexusAsset? Asset { get; set; }
    public string? Query { get; set; }
    public string? SearchType { get; set; }
    public string? RepositoryFilter { get; set; }
    public string? FormatFilter { get; set; }
    public string? NameFilter { get; set; }
    public string? VersionFilter { get; set; }
    public string? GroupFilter { get; set; }
    public string? Error { get; set; }
}

public sealed class NexusSettingsDto
{
    public bool Enabled { get; set; }
    public string BaseUrl { get; set; } = string.Empty;
    public string? Repository { get; set; }
    public NexusStatus? Status { get; set; }
    public bool? Writable { get; set; }
    public string? Error { get; set; }
}