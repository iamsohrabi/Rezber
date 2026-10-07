using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Text.RegularExpressions;
using Rezber.Core.Attributs;
using Rezber.Core.UnitOfWork;
using Rezber.Domain.Models;
using Rezber.Services.Helpers;

namespace Rezber.Services.Features;

public interface IPackageService
{
    Task<IReadOnlyCollection<Package>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyCollection<Package>> GetByAuthorEmailAsync(string authorEmail, CancellationToken ct = default);
    Task<Package?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<PackageDto> GetByIdAsync(Guid id);
    Task<IReadOnlyCollection<PackageDto>> FilterAsync(PackageFilter filter);
    Task<PackageDto> CreateNewPackagesync(Package package, Guid createdBy);
    Task<bool> UpdateAsync(Package package, Guid modifiedBy);
    Task<bool> UpdateImageAsync(Guid id, byte[]? imageWebp, bool lookupAttempted);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> DeleteAsync(string id, CancellationToken ct = default);
    Task<Dictionary<string, int>> GetLangCountsAsync();
    Task<Dictionary<string, int>> GetLicenseCountsAsync();
    Task<int> GetTotalDownloadsAsync();
    Task<bool> ExistsAsync(string name, string version);
}

[AutoInject(ServiceLifetime.Scoped)]
public class PackageService : IPackageService
{
    private readonly IRepository<Package> _repo;

    public PackageService(IRepository<Package> repo)
    {
        _repo = repo;
    }

    public Task<IReadOnlyCollection<Package>> GetAllAsync(CancellationToken ct = default)
        => _repo.GetAllAsync();

    public Task<IReadOnlyCollection<Package>> GetByAuthorEmailAsync(string authorEmail, CancellationToken ct = default)
        => _repo.GetAllAsync(package => package.Author == authorEmail);

    public async Task<Package?> GetByIdAsync(string id, CancellationToken ct = default)
        => Guid.TryParse(id, out var packageId) ? await _repo.GetAsync(packageId) : null;

    public async Task<PackageDto> GetByIdAsync(Guid id)
    {
        var doc = await _repo.GetAsync(id);
        return doc.ToDto();
    }

    public async Task<IReadOnlyCollection<PackageDto>> FilterAsync(PackageFilter filter)
    {
        var builder = Builders<Package>.Filter;
        var filters = new List<FilterDefinition<Package>>();

        if (filter.Filter != "all")
            filters.Add(builder.Eq(p => p.Lang, filter.Filter));

        if (!string.IsNullOrWhiteSpace(filter.Query))
        {
            var q = filter.Query.Trim();
            filters.Add(builder.Or(
                builder.Regex(p => p.Name, new BsonRegularExpression(Regex.Escape(q), "i")),
                builder.Regex(p => p.Description, new BsonRegularExpression(Regex.Escape(q), "i")),
                builder.Regex(p => p.Author, new BsonRegularExpression(Regex.Escape(q), "i")),
                builder.AnyEq(p => p.Tags, q)
            ));
        }

        if (!string.IsNullOrWhiteSpace(filter.Target))
            filters.Add(builder.Eq(p => p.TargetKey, filter.Target));

        if (!string.IsNullOrWhiteSpace(filter.License))
            filters.Add(builder.Eq(p => p.License, filter.License.Trim()));

        if (filter.OnlyMine)
            filters.Add(builder.Eq(p => p.Author, filter.CurrentUser));

        if (filter.OnlyRecent)
        {
            var monthAgo = DateTime.UtcNow.AddDays(-30);
            filters.Add(builder.Gte(p => p.LastModified, monthAgo));
        }

        var finalFilter = filters.Count > 0 ? builder.And(filters) : builder.Empty;

        var sort = filter.Sort switch
        {
            "downloads" => Builders<Package>.Sort.Descending(p => p.Downloads),
            "oldest" => Builders<Package>.Sort.Ascending(p => p.LastModified),
            "name" => Builders<Package>.Sort.Ascending(p => p.Name),
            _ => Builders<Package>.Sort.Descending(p => p.LastModified)
        };

        var res = await _repo.GetAllAsync(finalFilter, sort);

        return res.ToDToList();
    }

    public async Task<PackageDto> CreateNewPackagesync(Package package, Guid createdBy)
    {
        if (createdBy == Guid.Empty)
            throw new ArgumentException("A valid creator ID is required.", nameof(createdBy));

        var createdAt = DateTime.UtcNow;
        package.Created = createdAt;
        package.CreatedBy = createdBy;
        package.LastModified = createdAt;
        package.Dependencies ??= [];
        package.Versions ??= [];
        package.Changelog ??= [];

        if (string.IsNullOrEmpty(package.InstallCommand))
            package.InstallCommand = BuilderCommands.GetCommand(package.Name, package.Lang, package.Version);

        await _repo.CreateAsync(package);

        return package.ToDto();
    }

    public async Task<bool> UpdateAsync(Package package, Guid modifiedBy)
    {
        if (modifiedBy == Guid.Empty)
            throw new ArgumentException("A valid modifier ID is required.", nameof(modifiedBy));

        var existing = await _repo.GetAsync(package.Id);

        if (existing == null)
            return false;

        existing.Update(package, BuilderCommands.GetCommand(package.Name, package.Lang, package.Version));
        existing.LastModified = DateTime.UtcNow;
        existing.LastModifiedBy = modifiedBy;

        await _repo.UpdateAsync(existing);

        return true;
    }

    public async Task<bool> UpdateImageAsync(Guid id, byte[]? imageWebp, bool lookupAttempted)
    {
        var existing = await _repo.GetAsync(id);
        if (existing == null)
            return false;

        existing.ImageWebp = imageWebp;
        existing.ImageLookupAttempted = lookupAttempted;
        await _repo.UpdateAsync(existing);
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        await _repo.RemoveAsync(id);
        return true;
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken ct = default)
    {
        if (!Guid.TryParse(id, out var packageId))
            return false;

        return await DeleteAsync(packageId);
    }

    public async Task<Dictionary<string, int>> GetLangCountsAsync()
    {
        var packages = await _repo.GetAllAsync();
        var counts = packages
            .GroupBy(package => string.IsNullOrWhiteSpace(package.Lang)
                ? "generic"
                : package.Lang.Trim().ToLowerInvariant())
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);
        counts["all"] = packages.Count;

        return counts;
    }

    public async Task<int> GetTotalDownloadsAsync()
    {
        var result = await _repo.GroupAsync(
            new BsonDocument("_id", BsonNull.Value)
            .Add("total", new BsonDocument("$sum", "$downloads")));

        var total = result?["total"].ToInt32() ?? 0;

        return total;
    }

    public async Task<Dictionary<string, int>> GetLicenseCountsAsync()
    {
        var packages = await _repo.GetAllAsync();
        return packages
            .GroupBy(package => string.IsNullOrWhiteSpace(package.License)
                ? "Unspecified"
                : package.License.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);
    }

    public async Task<bool> ExistsAsync(string name, string version)
    {
        var all = await _repo.GetAllAsync();

        return all.Any(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase)
                         && p.Version == version);
    }

}
