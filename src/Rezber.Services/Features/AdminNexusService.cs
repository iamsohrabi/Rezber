using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Rezber.Core.Attributs;
using Rezber.Core.Settings;
using Rezber.Domain.Abstracts;
using Rezber.Services.Views.Admin;

namespace Rezber.Services.Features;

public interface IAdminNexusService
{
    Task<NexusAdminDto> GetPageAsync(NexusAdminQuery query, CancellationToken ct = default);
    Task<string> SyncCatalogAsync(string ip, CancellationToken ct = default);
    Task<string> RunHealthCheckAsync(string repositoryName, string ip, CancellationToken ct = default);
    Task<string> StopHealthCheckAsync(string repositoryName, string ip, CancellationToken ct = default);
    Task<bool> DeleteAssetFromUiAsync(string id, string ip, CancellationToken ct = default);
    Task<bool> DeleteComponentFromUiAsync(string id, string ip, CancellationToken ct = default);
}

[AutoInject(ServiceLifetime.Scoped)]
public sealed class AdminNexusService : IAdminNexusService
{
    private readonly INexusRepositoryClient _nexus;
    private readonly INexusCatalogService _catalog;
    private readonly IAuditService _auditService;
    private readonly IUserService _userService;

    public AdminNexusService(
        INexusRepositoryClient nexus,
        INexusCatalogService catalog,
        IAuditService auditService,
        IUserService userService)
    {
        _nexus = nexus;
        _catalog = catalog;
        _auditService = auditService;
        _userService = userService;
    }

    public async Task<NexusAdminDto> GetPageAsync(NexusAdminQuery query, CancellationToken ct = default)
    {
        var model = new NexusAdminDto
        {
            Query = query.Query,
            SearchType = query.SearchType == "components" ? "components" : "assets",
            RepositoryFilter = query.Repository,
            FormatFilter = query.Format,
            NameFilter = query.Name,
            VersionFilter = query.Version,
            GroupFilter = query.Group
        };
        try
        {
            model.Status = await _nexus.GetStatusAsync(ct);
            model.Writable = await _nexus.IsWritableAsync(ct);
            model.Repositories = await _nexus.GetRepositoriesAsync(ct);
            if (!string.IsNullOrWhiteSpace(query.Query) || !string.IsNullOrWhiteSpace(query.Repository) ||
                !string.IsNullOrWhiteSpace(query.Format) || !string.IsNullOrWhiteSpace(query.Name) ||
                !string.IsNullOrWhiteSpace(query.Version) || !string.IsNullOrWhiteSpace(query.Group))
            {
                var search = new NexusSearchQuery
                {
                    Q = query.Query?.Trim(),
                    Repository = query.Repository?.Trim(),
                    Format = query.Format?.Trim(),
                    Name = query.Name?.Trim(),
                    Version = query.Version?.Trim(),
                    Group = query.Group?.Trim()
                };
                model.SearchResult = model.SearchType == "components"
                    ? await _nexus.SearchAsync(search, ct)
                    : await _nexus.SearchAssetsAsync(search, ct);
            }

            if (!string.IsNullOrWhiteSpace(query.RepositoryName))
                model.Repository = await _nexus.GetRepositoryAsync(query.RepositoryName, ct);
            if (!string.IsNullOrWhiteSpace(query.ComponentId))
                model.Component = await _nexus.GetComponentAsync(query.ComponentId, ct);
            if (!string.IsNullOrWhiteSpace(query.AssetId))
                model.Asset = await _nexus.GetAssetAsync(query.AssetId, ct);
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException)
        {
            model.Error = exception.Message;
        }

        return model;
    }

    public async Task<string> SyncCatalogAsync(string ip, CancellationToken ct = default)
    {
        var summary = await _catalog.SyncAsync(ct);
        await LogAdminActionAsync(AuditActionType.Update,
            $"Synced {summary.PackageVersionCount} Nexus package versions from {summary.RepositoryCount} repositories", ip, ct);
        return $"Indexed {summary.PackageVersionCount} package versions from {summary.RepositoryCount} repositories.";
    }

    public async Task<string> RunHealthCheckAsync(string repositoryName, string ip, CancellationToken ct = default)
    {
        await _nexus.RunHealthCheckAsync(repositoryName, ct);
        await LogAdminActionAsync(AuditActionType.Update, $"Started health check for {repositoryName}", ip, ct);
        return $"Health check started for {repositoryName}.";
    }

    public async Task<string> StopHealthCheckAsync(string repositoryName, string ip, CancellationToken ct = default)
    {
        await _nexus.StopHealthCheckAsync(repositoryName, ct);
        await LogAdminActionAsync(AuditActionType.Update, $"Stopped health check for {repositoryName}", ip, ct);
        return $"Health check stopped for {repositoryName}.";
    }

    public async Task<bool> DeleteAssetFromUiAsync(string id, string ip, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(id))
            return false;
        await _nexus.DeleteAssetAsync(id, ct);
        await LogAdminActionAsync(AuditActionType.Delete, $"Deleted Nexus asset {id}", ip, ct);
        return true;
    }

    public async Task<bool> DeleteComponentFromUiAsync(string id, string ip, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(id))
            return false;
        await _nexus.DeleteComponentAsync(id, ct);
        await LogAdminActionAsync(AuditActionType.Delete, $"Deleted Nexus component {id}", ip, ct);
        return true;
    }

    private async Task LogAdminActionAsync(AuditActionType action, string target, string ip, CancellationToken ct)
    {
        var current = await _userService.GetCurrentUserAsync(ct);
        await _auditService.LogAsync(current.Id, current.DisplayName, action, target, ip, ct);
    }
}

public interface IAdminNexusSettingsService
{
    Task<NexusSettingsDto> GetAsync(CancellationToken ct = default);
}

[AutoInject(ServiceLifetime.Scoped)]
public sealed class AdminNexusSettingsService : IAdminNexusSettingsService
{
    private readonly INexusRepositoryClient _nexus;
    private readonly NexusSettings _settings;

    public AdminNexusSettingsService(INexusRepositoryClient nexus, IOptions<NexusSettings> options)
    {
        _nexus = nexus;
        _settings = options.Value;
    }

    public async Task<NexusSettingsDto> GetAsync(CancellationToken ct = default)
    {
        var model = new NexusSettingsDto
        {
            Enabled = _settings.Enabled,
            BaseUrl = _settings.BaseUrl,
            Repository = _settings.Repository
        };
        if (!_settings.Enabled)
            return model;

        try
        {
            model.Status = await _nexus.GetStatusAsync(ct);
            model.Writable = await _nexus.IsWritableAsync(ct);
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException)
        {
            model.Error = exception.Message;
        }

        return model;
    }
}