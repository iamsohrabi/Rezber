using System.Security.Claims;
using Rezber.Core.Attributs;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Rezber.Core.Helpers;

public interface ICurrentUser
{
    Guid Id { get; }
    string UserName { get; }
    string Email { get; }
    List<string> Roles { get; }
    Dictionary<string, string> Claims { get; }
    bool IsAuthenticated { get; }
    bool IsInRole(string role);
    T? GetClaimValue<T>(string claimType);
}


[AutoInject(ServiceLifetime.Scoped)]
public class CurrentUserService : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<CurrentUserService> _logger;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor, ILogger<CurrentUserService> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public Guid Id
    {
        get
        {
            string? id = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            Guid userId = id == null ? default : Guid.Parse(id);
            return userId;
        }
    }

    public string UserName => User?.Identity?.Name ?? string.Empty;

    public string Email => User?.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty;

    public List<string> Roles => User?.Claims
        .Where(c => c.Type == ClaimTypes.Role)
        .Select(c => c.Value)
        .ToList() ?? new List<string>();

    public Dictionary<string, string> Claims => User?.Claims
        .ToDictionary(c => c.Type, c => c.Value) ?? new Dictionary<string, string>();

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    public bool IsInRole(string role)
    {
        return Roles.Contains(role);
    }

    public T? GetClaimValue<T>(string claimType)
    {
        var claim = User?.Claims.FirstOrDefault(c => c.Type == claimType);
        if (claim == null)
            return default;

        try
        {
            return (T)Convert.ChangeType(claim.Value, typeof(T));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not convert claim {ClaimType} to type {Type}", claimType, typeof(T).Name);
            return default;
        }
    }
}
