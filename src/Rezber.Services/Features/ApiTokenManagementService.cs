using Microsoft.Extensions.DependencyInjection;
using Rezber.Core.Attributs;
using Rezber.Domain.Identity;

namespace Rezber.Services.Features;

public sealed record ApiTokenViewItem(Guid Id, string Name, string TokenPrefix, DateTime CreatedAt, DateTime? LastUsedAt);
public sealed record CreatedApiTokenView(Guid Id, string Name, string Token, string Message);
public sealed record AccountTokensView(IReadOnlyCollection<ApiToken> Tokens, string? NewlyCreatedToken = null);

public interface IApiTokenManagementService
{
    Task<PackageWorkflowResult<IReadOnlyCollection<ApiTokenViewItem>>> GetAsync(CancellationToken ct = default);
    Task<PackageWorkflowResult<CreatedApiTokenView>> CreateAsync(string? name, CancellationToken ct = default);
    Task<PackageWorkflowResult<string>> RevokeAsync(Guid id, CancellationToken ct = default);
    Task<UserDto?> GetActiveProfileAsync(CancellationToken ct = default);
    Task<PackageWorkflowResult<AccountTokensView>> GetAccountTokensAsync(CancellationToken ct = default);
    Task<PackageWorkflowResult<AccountTokensView>> CreateAccountTokenAsync(string? name, CancellationToken ct = default);
    Task<PackageWorkflowResult<string>> RevokeAccountTokenAsync(Guid id, CancellationToken ct = default);
}

[AutoInject(ServiceLifetime.Scoped)]
public sealed class ApiTokenManagementService : IApiTokenManagementService
{
    private readonly IUserService _userService;
    private readonly IApiTokenService _tokenService;

    public ApiTokenManagementService(IUserService userService, IApiTokenService tokenService)
    {
        _userService = userService;
        _tokenService = tokenService;
    }

    public async Task<PackageWorkflowResult<IReadOnlyCollection<ApiTokenViewItem>>> GetAsync(
        CancellationToken ct = default)
    {
        var userId = await GetCurrentUserIdAsync(ct);
        if (userId == null)
            return new(PackageWorkflowStatus.Unauthorized);

        var tokens = await _tokenService.GetByUserAsync(userId.Value, ct);
        return new(PackageWorkflowStatus.Success, tokens.Select(token => new ApiTokenViewItem(
            token.Id, token.Name, token.TokenPrefix, token.CreatedAt, token.LastUsedAt)).ToList());
    }

    public async Task<PackageWorkflowResult<CreatedApiTokenView>> CreateAsync(
        string? name,
        CancellationToken ct = default)
    {
        var userId = await GetCurrentUserIdAsync(ct);
        if (userId == null)
            return new(PackageWorkflowStatus.Unauthorized);

        var result = await _tokenService.CreateAsync(userId.Value, name ?? "CLI token", ct);
        return new(PackageWorkflowStatus.Success, new CreatedApiTokenView(
            result.Token.Id,
            result.Token.Name,
            result.PlainText,
            "Store this token now. It will not be shown again."));
    }

    public async Task<PackageWorkflowResult<string>> RevokeAsync(Guid id, CancellationToken ct = default)
    {
        var userId = await GetCurrentUserIdAsync(ct);
        if (userId == null)
            return new(PackageWorkflowStatus.Unauthorized);

        return await _tokenService.RevokeAsync(userId.Value, id, ct)
            ? new(PackageWorkflowStatus.Success)
            : new(PackageWorkflowStatus.NotFound);
    }

    public async Task<UserDto?> GetActiveProfileAsync(CancellationToken ct = default)
    {
        var user = await _userService.GetCurrentUserAsync(ct);
        return user.IsActive ? user : null;
    }

    public async Task<PackageWorkflowResult<AccountTokensView>> GetAccountTokensAsync(CancellationToken ct = default)
    {
        var userId = await GetCurrentUserIdAsync(ct);
        if (userId == null)
            return new(PackageWorkflowStatus.Unauthorized);

        return new(PackageWorkflowStatus.Success,
            new AccountTokensView(await _tokenService.GetByUserAsync(userId.Value, ct)));
    }

    public async Task<PackageWorkflowResult<AccountTokensView>> CreateAccountTokenAsync(
        string? name,
        CancellationToken ct = default)
    {
        var userId = await GetCurrentUserIdAsync(ct);
        if (userId == null)
            return new(PackageWorkflowStatus.Unauthorized);

        var created = await _tokenService.CreateAsync(userId.Value, name ?? "CLI token", ct);
        return new(PackageWorkflowStatus.Success, new AccountTokensView(
            await _tokenService.GetByUserAsync(userId.Value, ct), created.PlainText));
    }

    public async Task<PackageWorkflowResult<string>> RevokeAccountTokenAsync(Guid id, CancellationToken ct = default)
    {
        var userId = await GetCurrentUserIdAsync(ct);
        if (userId == null)
            return new(PackageWorkflowStatus.Unauthorized);

        return await _tokenService.RevokeAsync(userId.Value, id, ct)
            ? new(PackageWorkflowStatus.Success)
            : new(PackageWorkflowStatus.NotFound);
    }

    private async Task<Guid?> GetCurrentUserIdAsync(CancellationToken ct)
    {
        var user = await _userService.GetCurrentUserAsync(ct);
        return user.IsActive && Guid.TryParse(user.Id, out var userId) ? userId : null;
    }
}