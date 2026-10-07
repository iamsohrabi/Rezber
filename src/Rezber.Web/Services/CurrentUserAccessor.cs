using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using Rezber.Domain.Identity;
using IdentityUser = Rezber.Domain.Identity.User;
using Rezber.Services.Features;

namespace Rezber.Web.Services;

public interface ICurrentUserAccessor
{
    bool IsAuthenticated { get; }
    Guid? UserId { get; }
    Task<UserDto> GetAsync(CancellationToken ct = default);
    Task<bool> IsAdminAsync(CancellationToken ct = default);
}

public class CurrentUserAccessor : ICurrentUserAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IUserService _userService;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IMemoryCache _cache;

    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(15);

    public CurrentUserAccessor(
        IHttpContextAccessor httpContextAccessor,
        IUserService userService,
        UserManager<IdentityUser> userManager,
        IMemoryCache cache)
    {
        _httpContextAccessor = httpContextAccessor;
        _userService = userService;
        _userManager = userManager;
        _cache = cache;
    }

    private ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    public Guid? UserId
    {
        get
        {
            var id = Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(id, out var guid) ? guid : null;
        }
    }

    public async Task<UserDto> GetAsync(CancellationToken ct = default)
    {
        if (!IsAuthenticated)
            return GuestUser();

        var user = await _userManager.GetUserAsync(Principal!);
        if (user == null || !user.IsActive)
            return GuestUser();

        var cacheKey = GetCacheKey(user.Id);
        if (_cache.TryGetValue(cacheKey, out UserDto? cached) && cached != null)
            return cached;

        var dto = await _userService.GetByIdAsync(user.Id, ct) ?? GuestUser();
        _cache.Set(cacheKey, dto, CacheDuration);
        return dto;
    }

    public async Task<bool> IsAdminAsync(CancellationToken ct = default)
    {
        var dto = await GetAsync(ct);
        return dto.IsAdmin;
    }

    private static string GetCacheKey(Guid userId) => $"current_user_dto:{userId}";

    private static UserDto GuestUser() => new()
    {
        Id = Guid.Empty.ToString(),
        UserName = "guest",
        Email = string.Empty,
        DisplayName = "Guest",
        JobTitle = "—",
        Roles = new List<string>(),
        IsActive = false
    };
}
