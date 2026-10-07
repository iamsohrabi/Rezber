using Microsoft.Extensions.DependencyInjection;
using Rezber.Core.Attributs;
using Rezber.Domain.Abstracts;
using Rezber.Domain.Models;
using Rezber.Services.Views.Admin;

namespace Rezber.Services.Features;

public interface IAdminOperationsService
{
    Task<IReadOnlyCollection<Package>> GetPackagesAsync(CancellationToken ct = default);
    Task<IReadOnlyCollection<AuditLog>> GetAuditLogsAsync(CancellationToken ct = default);
    Task<PackageWorkflowResult<PackageOperationData>> DeletePackageAsync(string id, UserDto admin, string ip, CancellationToken ct = default);
    Task<string> RunCleanupAsync(CleanupFormDto form, UserDto admin, string ip, CancellationToken ct = default);
    Task<string> DeleteOldAuditLogsAsync(UserDto admin, string ip, CancellationToken ct = default);
}

[AutoInject(ServiceLifetime.Scoped)]
public sealed class AdminOperationsService : IAdminOperationsService
{
    private readonly IPackageService _packageService;
    private readonly IAuditService _auditService;

    public AdminOperationsService(IPackageService packageService, IAuditService auditService)
    {
        _packageService = packageService;
        _auditService = auditService;
    }

    public Task<IReadOnlyCollection<Package>> GetPackagesAsync(CancellationToken ct = default) =>
        _packageService.GetAllAsync(ct);

    public Task<IReadOnlyCollection<AuditLog>> GetAuditLogsAsync(CancellationToken ct = default) =>
        _auditService.GetRecentAsync(200, ct);

    public async Task<PackageWorkflowResult<PackageOperationData>> DeletePackageAsync(
        string id,
        UserDto admin,
        string ip,
        CancellationToken ct = default)
    {
        var package = await _packageService.GetByIdAsync(id, ct);
        if (package == null)
            return new(PackageWorkflowStatus.NotFound);

        await _packageService.DeleteAsync(id, ct);
        await _auditService.LogAsync(admin.Id, admin.DisplayName,
            AuditActionType.Delete, package.Name, ip, ct);
        return new(PackageWorkflowStatus.Success, new(package.Id.ToString(), package.Name));
    }

    public async Task<string> RunCleanupAsync(
        CleanupFormDto form,
        UserDto admin,
        string ip,
        CancellationToken ct = default)
    {
        var deletedLogs = form.Logs
            ? await _auditService.DeleteOlderThanAsync(DateTime.UtcNow.AddDays(-Math.Clamp(form.LogDays, 1, 3650)), ct)
            : 0;

        await _auditService.LogAsync(admin.Id, admin.DisplayName,
            AuditActionType.Cleanup,
            $"Deleted {deletedLogs} audit logs older than {form.LogDays} days", ip, ct);

        return form.Logs
            ? $"Deleted {deletedLogs} audit logs older than {form.LogDays} days."
            : "No cleanup action was selected.";
    }

    public async Task<string> DeleteOldAuditLogsAsync(
        UserDto admin,
        string ip,
        CancellationToken ct = default)
    {
        var deleted = await _auditService.DeleteOlderThanAsync(DateTime.UtcNow.AddDays(-30), ct);
        await _auditService.LogAsync(admin.Id, admin.DisplayName,
            AuditActionType.Cleanup, $"Deleted {deleted} old logs", ip, ct);
        return $"{deleted} old logs deleted.";
    }
}