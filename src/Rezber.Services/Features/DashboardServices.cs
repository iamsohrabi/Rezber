using Microsoft.Extensions.DependencyInjection;
using Rezber.Core.Attributs;
using Rezber.Services.Views.Admin;
using Rezber.Services.Views.Dashboard;

namespace Rezber.Services.Features;

public interface IUserDashboardService
{
    Task<UserDashboardDto> GetAsync(CancellationToken ct = default);
}

[AutoInject(ServiceLifetime.Scoped)]
public sealed class UserDashboardService : IUserDashboardService
{
    private readonly IPackageService _packageService;
    private readonly IUserService _userService;

    public UserDashboardService(IPackageService packageService, IUserService userService)
    {
        _packageService = packageService;
        _userService = userService;
    }

    public async Task<UserDashboardDto> GetAsync(CancellationToken ct = default)
    {
        var current = await _userService.GetCurrentUserAsync(ct);
        if (Guid.TryParse(current.Id, out var currentUserId))
        {
            await _userService.RefreshUserStatsAsync(currentUserId, ct);
            current = await _userService.GetByIdAsync(currentUserId, ct) ?? current;
        }

        var myPackages = await _packageService.GetByAuthorEmailAsync(current.Email, ct);
        var totalDownloads = myPackages.Sum(package => package.Downloads);
        var mostDownloadedPackage = myPackages
            .Where(package => package.Downloads > 0)
            .OrderByDescending(package => package.Downloads)
            .FirstOrDefault();
        var lastPublished = myPackages
            .OrderByDescending(package => package.UpdatedAt != default ? package.UpdatedAt : package.Created)
            .FirstOrDefault();

        return new UserDashboardDto
        {
            User = current,
            MyPackages = myPackages.OrderByDescending(package => package.UpdatedAt != default
                ? package.UpdatedAt
                : package.Created).ToList(),
            TotalDownloads = totalDownloads,
            AverageDownloadsPerPackage = myPackages.Count == 0
                ? 0
                : totalDownloads / (double)myPackages.Count,
            TotalVersions = myPackages.Sum(package => (package.Versions ?? [])
                .Select(version => version.Version)
                .Append(package.Version)
                .Where(version => !string.IsNullOrWhiteSpace(version))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count()),
            MostDownloadedPackage = mostDownloadedPackage?.Name,
            UsedStorageMb = current.StorageMb,
            LastPublished = lastPublished
        };
    }
}

public interface IAdminDashboardService
{
    Task<AdminDashboardDto> GetAsync(CancellationToken ct = default);
}

[AutoInject(ServiceLifetime.Scoped)]
public sealed class AdminDashboardService : IAdminDashboardService
{
    private readonly IPackageService _packageService;
    private readonly IUserService _userService;
    private readonly IStorageService _storageService;
    private readonly IAuditService _auditService;

    public AdminDashboardService(
        IPackageService packageService,
        IUserService userService,
        IStorageService storageService,
        IAuditService auditService)
    {
        _packageService = packageService;
        _userService = userService;
        _storageService = storageService;
        _auditService = auditService;
    }

    public async Task<AdminDashboardDto> GetAsync(CancellationToken ct = default)
    {
        var users = await _userService.GetAllAsync(ct);
        var packages = await _packageService.GetAllAsync(ct);
        var storage = await _storageService.GetSnapshotAsync(ct);
        var audit = await _auditService.GetRecentAsync(10, ct);

        return new AdminDashboardDto
        {
            TotalPackages = packages.Count,
            TotalUsers = await _userService.CountAsync(ct),
            TotalDownloads = packages.Sum(package => package.Downloads),
            Storage = storage,
            Packages = packages.ToList(),
            Users = users.ToList(),
            AuditLogs = audit.ToList()
        };
    }
}