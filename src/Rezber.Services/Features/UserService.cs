
using Microsoft.Extensions.Logging;
using Rezber.Core.UnitOfWork;
using Rezber.Domain.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Memory;
using IdentityRole = Rezber.Domain.Identity.Role;
using IdentityUser = Rezber.Domain.Identity.User;
using Rezber.Domain.Identity;
using Rezber.Core.Attributs;
using Microsoft.Extensions.DependencyInjection;

namespace Rezber.Services.Features;

public interface IUserService
{
    Task<(bool Succeeded, string? Error)> SignInAsync(string email, string password, bool rememberMe, CancellationToken ct = default);
    Task<(bool Succeeded, IReadOnlyCollection<string> Errors)> RegisterAsync(string displayName, string email, string jobTitle, string password, CancellationToken ct = default);
    Task SignOutAsync(CancellationToken ct = default);
    Task<(bool Succeeded, string? Error)> ChangePasswordAsync(string currentPassword, string newPassword, CancellationToken ct = default);
    Task<bool> UpdateLastActiveAsync(string email, CancellationToken ct = default);
    Task<UserDto> GetCurrentUserAsync(CancellationToken ct = default);
    Task<bool> IsCurrentUserAdminAsync(CancellationToken ct = default);
    Task<IReadOnlyCollection<UserDto>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyCollection<UserDto>> GetActiveUsersAsync(CancellationToken ct = default);
    Task<UserDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<UserDto?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<IReadOnlyCollection<UserDto>> GetByRoleAsync(string role, CancellationToken ct = default);
    Task<int> CountAsync(CancellationToken ct = default);
    Task<(bool Succeeded, string? Error)> CreateAsync(
        string displayName,
        string email,
        string jobTitle,
        string password,
        string role = "Developer",
        CancellationToken ct = default);
    Task<(bool Succeeded, string? Error)> UpdateAsync(
        Guid id,
        string displayName,
        string jobTitle,
        bool isActive,
        CancellationToken ct = default);
    Task<(bool Succeeded, string? Error)> DeleteAsync(Guid id, CancellationToken ct = default);
    Task RefreshUserStatsAsync(Guid userId, CancellationToken ct = default);
}

[AutoInject(ServiceLifetime.Scoped)]
public class UserService : IUserService
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IRepository<Package> _packageRepo;
    private readonly IMemoryCache _cache;
    private readonly ILogger<UserService> _logger;

    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(15);

    public UserService(UserManager<IdentityUser> userManager,
                       RoleManager<IdentityRole> roleManager,
                       SignInManager<IdentityUser> signInManager,
                       IHttpContextAccessor httpContextAccessor,
                       IRepository<Package> packageRepo,
                       IMemoryCache cache,
                       ILogger<UserService> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _signInManager = signInManager;
        _httpContextAccessor = httpContextAccessor;
        _packageRepo = packageRepo;
        _cache = cache;
        _logger = logger;
    }

    public async Task<(bool Succeeded, string? Error)> SignInAsync(
        string email, string password, bool rememberMe, CancellationToken ct = default)
    {
        var login = email.Trim();
        var user = await _userManager.FindByEmailAsync(login)
                   ?? await _userManager.FindByNameAsync(login);
        var result = user == null
            ? SignInResult.Failed
            : await _signInManager.CheckPasswordSignInAsync(
                user, password, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            await _signInManager.SignInAsync(user!, rememberMe);
            await UpdateLastActiveAsync(user!.Email ?? login, ct);
            return (true, null);
        }

        return result.IsLockedOut
            ? (false, "The account is locked because of repeated failed attempts.")
            : (false, "The email or password is incorrect.");
    }

    public async Task<(bool Succeeded, IReadOnlyCollection<string> Errors)> RegisterAsync(
        string displayName, string email, string jobTitle, string password, CancellationToken ct = default)
    {
        var result = await CreateAsync(displayName, email, jobTitle, password, Roles.Developer, ct);
        if (!result.Succeeded)
            return (false, new[] { result.Error ?? "Registration failed." });

        await _signInManager.PasswordSignInAsync(
            email.Trim(), password, isPersistent: false, lockoutOnFailure: false);
        return (true, Array.Empty<string>());
    }

    public Task SignOutAsync(CancellationToken ct = default) => _signInManager.SignOutAsync();

    public async Task<(bool Succeeded, string? Error)> ChangePasswordAsync(
        string currentPassword,
        string newPassword,
        CancellationToken ct = default)
    {
        var principal = _httpContextAccessor.HttpContext?.User;
        if (principal?.Identity?.IsAuthenticated != true)
            return (false, "You must be signed in to change your password.");

        var user = await _userManager.GetUserAsync(principal);
        if (user == null || !user.IsActive)
            return (false, "The active user account could not be found.");

        var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        if (!result.Succeeded)
            return (false, JoinErrors(result));

        _cache.Remove(GetCurrentUserCacheKey(user.Id));
        await _signInManager.RefreshSignInAsync(user);
        return (true, null);
    }

    public async Task<bool> UpdateLastActiveAsync(string email, CancellationToken ct = default)
    {
        var user = await _userManager.FindByEmailAsync(email.Trim());
        if (user == null) return false;

        user.LastActive = DateTime.UtcNow;
        var result = await _userManager.UpdateAsync(user);
        _cache.Remove(GetCurrentUserCacheKey(user.Id));
        return result.Succeeded;
    }

    public async Task<UserDto> GetCurrentUserAsync(CancellationToken ct = default)
    {
        var principal = _httpContextAccessor.HttpContext?.User;
        if (principal?.Identity?.IsAuthenticated != true)
            return GuestUser();

        var user = await _userManager.GetUserAsync(principal);
        if (user == null || !user.IsActive)
            return GuestUser();

        var cacheKey = GetCurrentUserCacheKey(user.Id);
        if (_cache.TryGetValue(cacheKey, out UserDto? cached) && cached != null)
            return cached;

        var dto = await MapToDtoAsync(user);
        _cache.Set(cacheKey, dto, CacheDuration);
        return dto;
    }

    public async Task<bool> IsCurrentUserAdminAsync(CancellationToken ct = default)
    {
        var dto = await GetCurrentUserAsync(ct);
        return dto.IsAdmin;
    }

    public async Task<IReadOnlyCollection<UserDto>> GetAllAsync(CancellationToken ct = default)
    {
        var users = _userManager.Users
            .OrderBy(u => u.UserName)
            .ToList();

        var result = new List<UserDto>(users.Count);
        foreach (var user in users)
            result.Add(await MapToDtoAsync(user));

        return result;
    }

    public async Task<IReadOnlyCollection<UserDto>> GetActiveUsersAsync(CancellationToken ct = default)
    {
        var users = _userManager.Users
            .Where(u => u.IsActive)
            .OrderBy(u => u.UserName)
            .ToList();

        var result = new List<UserDto>(users.Count);
        foreach (var user in users)
            result.Add(await MapToDtoAsync(user));

        return result;
    }

    public async Task<UserDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        return user == null ? null : await MapToDtoAsync(user);
    }

    public async Task<UserDto?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(email)) return null;
        var user = await _userManager.FindByEmailAsync(email.Trim());
        return user == null ? null : await MapToDtoAsync(user);
    }

    public async Task<IReadOnlyCollection<UserDto>> GetByRoleAsync(string role, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(role))
            return Array.Empty<UserDto>();

        var users = await _userManager.GetUsersInRoleAsync(role);
        var result = new List<UserDto>(users.Count);
        foreach (var user in users)
            result.Add(await MapToDtoAsync(user));

        return result;
    }

    public Task<int> CountAsync(CancellationToken ct = default)
        => Task.FromResult(_userManager.Users.Count());

    // ============================================================
    //  WRITE
    // ============================================================

    public async Task<(bool Succeeded, string? Error)> CreateAsync(
        string displayName,
        string email,
        string jobTitle,
        string password,
        string role = Roles.Developer,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(email))
            return (false, "Email is required.");

        if (!await _roleManager.RoleExistsAsync(role))
            return (false, $"Role '{role}' does not exist.");

        var existing = await _userManager.FindByEmailAsync(email.Trim());
        if (existing != null)
            return (false, $"A user with email '{email}' already exists.");

        var user = new IdentityUser(email.Trim(), email.Trim())
        {
            Email = email.Trim(),
            DisplayName = displayName.Trim(),
            JobTitle = jobTitle.Trim(),
            EmailConfirmed = true,
            IsActive = true,
            LastActive = DateTime.UtcNow
        };

        var createResult = await _userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
            return (false, JoinErrors(createResult));

        var roleResult = await _userManager.AddToRoleAsync(user, role);
        if (!roleResult.Succeeded)
        {
            // rollback
            await _userManager.DeleteAsync(user);
            return (false, JoinErrors(roleResult));
        }

        _logger.LogInformation("New user created: {Email} with role {Role}", email, role);
        return (true, null);
    }

    public async Task<(bool Succeeded, string? Error)> UpdateAsync(
        Guid id,
        string displayName,
        string jobTitle,
        bool isActive,
        CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null) return (false, "User not found.");

        user.DisplayName = displayName.Trim();
        user.JobTitle = jobTitle.Trim();
        user.IsActive = isActive;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return (false, JoinErrors(result));

        _cache.Remove(GetCurrentUserCacheKey(user.Id));
        _logger.LogInformation("User {Email} updated", user.Email);
        return (true, null);
    }

    public async Task<(bool Succeeded, string? Error)> DeleteAsync(
        Guid id,
        CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null) return (false, "User not found.");

        // Administrators cannot be deleted.
        var roles = await _userManager.GetRolesAsync(user);
        if (roles.Contains(Roles.Admin))
            return (false, "Administrator accounts cannot be deleted.");

        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
            return (false, JoinErrors(result));

        _cache.Remove(GetCurrentUserCacheKey(user.Id));
        _logger.LogInformation("User {Email} deleted", user.Email);
        return (true, null);
    }

    // ============================================================
    //  STATS
    // ============================================================

    public async Task RefreshUserStatsAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return;

        var packages = await _packageRepo.GetAllAsync(p => p.Author == user.Email);
        user.PackageCount = packages.Count;
        user.StorageMb = (int)packages.Sum(p => ParseSizeToMb(p.Size));

        await _userManager.UpdateAsync(user);
        _cache.Remove(GetCurrentUserCacheKey(user.Id));

        _logger.LogInformation(
            "Statistics refreshed for {Email}: {Count} packages, {Size} MB",
            user.Email, user.PackageCount, user.StorageMb);

    }

    // ============================================================
    //  MAPPING & HELPERS
    // ============================================================

    private async Task<UserDto> MapToDtoAsync(IdentityUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? Roles.Developer;
        return new UserDto
        {
            Id = user.Id.ToString(),
            UserName = user.UserName ?? string.Empty,
            Name = string.IsNullOrWhiteSpace(user.DisplayName) ? user.UserName ?? "—" : user.DisplayName,
            DisplayName = user.DisplayName,
            JobTitle = user.JobTitle,
            Email = user.Email ?? string.Empty,
            Role = role,
            RoleLabel = role,
            Roles = roles.ToList(),
            Packages = user.PackageCount,
            StorageMb = user.StorageMb,
            IsActive = user.IsActive,
            LastActive = user.LastActive,
            IsAdmin = roles.Contains(Roles.Admin)
        };
    }

    private static string GetCurrentUserCacheKey(Guid userId) => $"current_user_dto:{userId}";

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

    private static string JoinErrors(IdentityResult result) =>
        string.Join(" | ", result.Errors.Select(e => e.Description));

    private static double ParseSizeToMb(string? size)
    {
        if (string.IsNullOrWhiteSpace(size)) return 0;

        var parts = size.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2) return 0;
        if (!double.TryParse(parts[0], out var value)) return 0;

        return parts[1].ToUpperInvariant() switch
        {
            "B" => value / (1024 * 1024),
            "KB" => value / 1024,
            "MB" => value,
            "GB" => value * 1024,
            "TB" => value * 1024 * 1024,
            _ => 0
        };
    }
}
