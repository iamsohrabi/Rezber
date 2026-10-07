using Microsoft.Extensions.DependencyInjection;
using Rezber.Core.Attributs;
using Rezber.Core.UnitOfWork;
using Rezber.Domain.Abstracts;
using Rezber.Domain.Models;

namespace Rezber.Services.Features;

public interface IAuditService
{
    Task<IReadOnlyCollection<AuditLog>> GetRecentAsync(int count, CancellationToken ct = default);
    Task<int> DeleteOlderThanAsync(DateTime cutoff, CancellationToken ct = default);
    Task LogAsync(Guid userId, string userName, AuditActionType action, string target, string ip, CancellationToken ct = default);
    Task LogAsync(string userId, string userName, AuditActionType action, string target, string ip, CancellationToken ct = default);
}

[AutoInject(ServiceLifetime.Scoped)]
public sealed class AuditService : IAuditService
{
    private readonly IRepository<AuditLog> _repository;

    public AuditService(IRepository<AuditLog> repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyCollection<AuditLog>> GetRecentAsync(int count, CancellationToken ct = default)
        => (await _repository.GetAllAsync(log => !log.IsDeleted))
            .OrderByDescending(log => log.Created)
            .Take(Math.Max(count, 0))
            .ToList();

    public async Task<int> DeleteOlderThanAsync(DateTime cutoff, CancellationToken ct = default)
    {
        var logs = await _repository.GetAllAsync(log => !log.IsDeleted && log.Created < cutoff);
        foreach (var log in logs)
            await _repository.RemoveAsync(log.Id);

        return logs.Count;
    }

    public Task LogAsync(
        Guid userId,
        string userName,
        AuditActionType action,
        string target,
        string ip,
        CancellationToken ct = default)
        => _repository.CreateAsync(new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            UserName = userName,
            Action = action,
            Target = target,
            Ip = ip,
            Created = DateTime.UtcNow
        });

    public Task LogAsync(
        string userId,
        string userName,
        AuditActionType action,
        string target,
        string ip,
        CancellationToken ct = default)
        => LogAsync(Guid.TryParse(userId, out var id) ? id : Guid.Empty, userName, action, target, ip, ct);
}

public interface IStorageService
{
    Task<StorageSnapshot> GetSnapshotAsync(CancellationToken ct = default);
}

[AutoInject(ServiceLifetime.Scoped)]
public sealed class StorageService : IStorageService
{
    private readonly IRepository<StorageSnapshot> _repository;

    public StorageService(IRepository<StorageSnapshot> repository)
    {
        _repository = repository;
    }

    public async Task<StorageSnapshot> GetSnapshotAsync(CancellationToken ct = default)
        => (await _repository.GetAllAsync())
            .OrderByDescending(snapshot => snapshot.Created)
            .FirstOrDefault()
            ?? new StorageSnapshot { Id = Guid.Empty };
}
