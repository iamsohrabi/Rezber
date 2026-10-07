using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Rezber.Core.Attributs;
using Rezber.Core.UnitOfWork;
using Rezber.Domain.Identity;

namespace Rezber.Services.Features;

public interface IApiTokenService
{
    Task<(ApiToken Token, string PlainText)> CreateAsync(Guid userId, string name, CancellationToken ct = default);
    Task<IReadOnlyCollection<ApiToken>> GetByUserAsync(Guid userId, CancellationToken ct = default);
    Task<bool> RevokeAsync(Guid userId, Guid tokenId, CancellationToken ct = default);
    Task<ApiToken?> ValidateAsync(string plainText, CancellationToken ct = default);
}

[AutoInject(ServiceLifetime.Scoped)]
public sealed class ApiTokenService : IApiTokenService
{
    private readonly IRepository<ApiToken> _repository;

    public ApiTokenService(IRepository<ApiToken> repository)
    {
        _repository = repository;
    }

    public async Task<(ApiToken Token, string PlainText)> CreateAsync(
        Guid userId,
        string name,
        CancellationToken ct = default)
    {
        var plainText = $"nxr_{CreateRandomValue()}";
        var token = new ApiToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = string.IsNullOrWhiteSpace(name) ? "CLI token" : name.Trim(),
            TokenHash = Hash(plainText),
            TokenPrefix = plainText[..12],
            CreatedAt = DateTime.UtcNow,
            Created = DateTime.UtcNow
        };

        await _repository.CreateAsync(token);
        return (token, plainText);
    }

    public Task<IReadOnlyCollection<ApiToken>> GetByUserAsync(Guid userId, CancellationToken ct = default) =>
        _repository.GetAllAsync(token => token.UserId == userId && token.RevokedAt == null);

    public async Task<bool> RevokeAsync(Guid userId, Guid tokenId, CancellationToken ct = default)
    {
        var token = await _repository.GetAsync(tokenId);
        if (token == null || token.UserId != userId || token.RevokedAt != null)
            return false;

        token.RevokedAt = DateTime.UtcNow;
        token.LastModified = DateTime.UtcNow;
        await _repository.UpdateAsync(token);
        return true;
    }

    public async Task<ApiToken?> ValidateAsync(string plainText, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(plainText))
            return null;

        var token = (await _repository.GetAllAsync(candidate =>
            candidate.TokenHash == Hash(plainText) && candidate.RevokedAt == null)).FirstOrDefault();
        if (token == null)
            return null;

        token.LastUsedAt = DateTime.UtcNow;
        await _repository.UpdateAsync(token);
        return token;
    }

    private static string CreateRandomValue() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');

    private static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}