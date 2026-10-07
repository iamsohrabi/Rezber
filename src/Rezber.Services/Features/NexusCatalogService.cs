using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using MongoDB.Bson;
using MongoDB.Driver;
using Rezber.Domain.Models;

namespace Rezber.Services.Features;

public interface INexusCatalogService
{
    Task<NexusCatalogSyncSummary> SyncAsync(CancellationToken ct = default);
    Task<NexusCatalogPage> SearchAsync(NexusCatalogQuery query, CancellationToken ct = default);
}

public sealed class NexusCatalogService : INexusCatalogService
{
    private readonly INexusRepositoryClient _nexus;
    private readonly IMongoCollection<NexusCatalogEntry> _entries;

    public NexusCatalogService(IMongoDatabase database, INexusRepositoryClient nexus)
    {
        _nexus = nexus;
        _entries = database.GetCollection<NexusCatalogEntry>("nexusCatalog");
        _entries.Indexes.CreateOne(new CreateIndexModel<NexusCatalogEntry>(
            Builders<NexusCatalogEntry>.IndexKeys
                .Ascending(entry => entry.Repository)
                .Ascending(entry => entry.Format)
                .Ascending(entry => entry.Name)
                .Ascending(entry => entry.Version)));
    }

    public async Task<NexusCatalogSyncSummary> SyncAsync(CancellationToken ct = default)
    {
        var startedAt = DateTime.UtcNow;
        var repositories = await _nexus.GetRepositoriesAsync(ct);
        var indexedCount = 0;

        foreach (var repository in repositories)
        {
            string? continuationToken = null;
            var seenTokens = new HashSet<string>(StringComparer.Ordinal);

            do
            {
                var page = await _nexus.SearchAsync(new NexusSearchQuery
                {
                    Repository = repository.Name,
                    ContinuationToken = continuationToken
                }, ct);

                var entries = page.Items
                    .Where(item => !string.IsNullOrWhiteSpace(item.Name)
                        && !string.IsNullOrWhiteSpace(item.Version))
                    .Select(item => ToEntry(item, startedAt))
                    .ToList();

                if (entries.Count > 0)
                {
                    var writes = entries.Select(entry => new ReplaceOneModel<NexusCatalogEntry>(
                        Builders<NexusCatalogEntry>.Filter.Eq(current => current.Id, entry.Id), entry)
                    {
                        IsUpsert = true
                    }).ToList();

                    await _entries.BulkWriteAsync(writes,
                        new BulkWriteOptions { IsOrdered = false }, ct);
                    indexedCount += entries.Count;
                }

                continuationToken = page.ContinuationToken;
                if (!string.IsNullOrWhiteSpace(continuationToken) &&
                    !seenTokens.Add(continuationToken))
                    throw new InvalidOperationException(
                        $"Nexus repeated a continuation token while syncing '{repository.Name}'.");
            }
            while (!string.IsNullOrWhiteSpace(continuationToken));
        }

        await _entries.DeleteManyAsync(entry => entry.IndexedAt < startedAt, ct);
        return new NexusCatalogSyncSummary(repositories.Count, indexedCount, DateTime.UtcNow);
    }

    public async Task<NexusCatalogPage> SearchAsync(
        NexusCatalogQuery query,
        CancellationToken ct = default)
    {
        var filters = new List<FilterDefinition<NexusCatalogEntry>>();
        var builder = Builders<NexusCatalogEntry>.Filter;

        if (!string.IsNullOrWhiteSpace(query.Repository))
            filters.Add(builder.Eq(entry => entry.Repository, query.Repository.Trim()));
        if (!string.IsNullOrWhiteSpace(query.Format))
            filters.Add(builder.Eq(entry => entry.Format, query.Format.Trim()));
        if (!string.IsNullOrWhiteSpace(query.Name))
            filters.Add(builder.Eq(entry => entry.Name, query.Name.Trim()));
        if (!string.IsNullOrWhiteSpace(query.Version))
            filters.Add(builder.Eq(entry => entry.Version, query.Version.Trim()));
        if (!string.IsNullOrWhiteSpace(query.Group))
            filters.Add(builder.Eq(entry => entry.Group, query.Group.Trim()));

        if (!string.IsNullOrWhiteSpace(query.Query))
        {
            var expression = new BsonRegularExpression(Regex.Escape(query.Query.Trim()), "i");
            filters.Add(builder.Regex(entry => entry.SearchText, expression));
        }

        var filter = filters.Count == 0 ? builder.Empty : builder.And(filters);
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var totalCount = await _entries.CountDocumentsAsync(filter, cancellationToken: ct);
        var items = await _entries.Find(filter)
            .SortBy(entry => entry.Name)
            .ThenBy(entry => entry.Version)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(ct);

        return new NexusCatalogPage(items, totalCount, page, pageSize);
    }

    private static NexusCatalogEntry ToEntry(NexusSearchItem item, DateTime indexedAt)
    {
        var name = item.Name!.Trim();
        var version = item.Version!.Trim();
        var group = string.IsNullOrWhiteSpace(item.Group) ? null : item.Group.Trim();
        var identity = string.Join("\n", item.Repository, item.Format, group ?? "", name, version);
        var id = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identity)));
        var assets = item.Assets ?? Array.Empty<NexusAsset>();
        var sizes = assets.Where(asset => asset.FileSize.HasValue).Select(asset => asset.FileSize!.Value);
        var modifiedAt = assets
            .Where(asset => asset.LastModified.HasValue)
            .Select(asset => asset.LastModified!.Value.UtcDateTime)
            .DefaultIfEmpty()
            .Max();

        return new NexusCatalogEntry
        {
            Id = id,
            Repository = item.Repository,
            Format = item.Format,
            Group = group,
            Name = name,
            Version = version,
            SearchText = string.Join(' ', new[] { group, name, version, item.Repository, item.Format }
                .Where(value => !string.IsNullOrWhiteSpace(value))),
            AssetPaths = assets.Select(asset => asset.Path).Where(path => !string.IsNullOrWhiteSpace(path)).ToList(),
            SizeBytes = sizes.Any() ? sizes.Sum() : null,
            LastModified = modifiedAt == default ? null : modifiedAt,
            IndexedAt = indexedAt
        };
    }

}

public sealed class NexusCatalogQuery
{
    public string? Query { get; set; }
    public string? Repository { get; set; }
    public string? Format { get; set; }
    public string? Name { get; set; }
    public string? Version { get; set; }
    public string? Group { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}

public sealed record NexusCatalogPage(
    IReadOnlyCollection<NexusCatalogEntry> Items,
    long TotalCount,
    int Page,
    int PageSize);

public sealed record NexusCatalogSyncSummary(
    int RepositoryCount,
    int PackageVersionCount,
    DateTime CompletedAt);