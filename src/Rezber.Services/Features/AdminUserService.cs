using Microsoft.Extensions.DependencyInjection;
using Rezber.Core.Attributs;
using Rezber.Domain.Abstracts;
using Rezber.Domain.Identity;

namespace Rezber.Services.Features;

public sealed record AdminUserResult(bool Succeeded, string? Message = null, bool NotFound = false);

public interface IAdminUserService
{
    Task<IReadOnlyCollection<UserDto>> GetUsersAsync(string? status, string? role, CancellationToken ct = default);
    Task<AdminUserResult> CreateAsync(string displayName, string email, string jobTitle, string password, UserDto admin, string ip, CancellationToken ct = default);
    Task<AdminUserResult> UpdateAsync(Guid id, string displayName, string jobTitle, bool isActive, UserDto admin, string ip, CancellationToken ct = default);
    Task<AdminUserResult> DeleteAsync(Guid id, UserDto admin, string ip, CancellationToken ct = default);
}

[AutoInject(ServiceLifetime.Scoped)]
public sealed class AdminUserService : IAdminUserService
{
    private readonly IUserService _userService;
    private readonly IAuditService _auditService;

    public AdminUserService(IUserService userService, IAuditService auditService)
    {
        _userService = userService;
        _auditService = auditService;
    }

    public async Task<IReadOnlyCollection<UserDto>> GetUsersAsync(
        string? status,
        string? role,
        CancellationToken ct = default)
    {
        var users = role is Roles.Admin or Roles.Developer
            ? await _userService.GetByRoleAsync(role, ct)
            : status == "active"
                ? await _userService.GetActiveUsersAsync(ct)
                : await _userService.GetAllAsync(ct);

        if (status == "inactive")
            return users.Where(user => !user.IsActive).ToList();
        if (status == "active")
            return users.Where(user => user.IsActive).ToList();
        return users;
    }

    public async Task<AdminUserResult> CreateAsync(
        string displayName,
        string email,
        string jobTitle,
        string password,
        UserDto admin,
        string ip,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(displayName) || string.IsNullOrWhiteSpace(jobTitle))
            return new(false, "Display name and job title are required.");

        var (succeeded, error) = await _userService.CreateAsync(
            displayName, email, jobTitle, password, Roles.Developer, ct);
        if (!succeeded)
            return new(false, error);

        await _auditService.LogAsync(admin.Id, admin.DisplayName,
            AuditActionType.Create, $"Created user {email}", ip, ct);
        return new(true, $"Developer account for {email} created.");
    }

    public async Task<AdminUserResult> UpdateAsync(
        Guid id,
        string displayName,
        string jobTitle,
        bool isActive,
        UserDto admin,
        string ip,
        CancellationToken ct = default)
    {
        var target = await _userService.GetByIdAsync(id, ct);
        if (target == null)
            return new(false, NotFound: true);
        if (admin.Id == id.ToString() && !isActive)
            return new(false, "You cannot deactivate your own administrator account.");
        if (target.IsAdmin && !isActive)
            return new(false, "Administrator accounts cannot be deactivated here.");

        var (succeeded, error) = await _userService.UpdateAsync(id, displayName, jobTitle, isActive, ct);
        if (!succeeded)
            return new(false, error);

        await _auditService.LogAsync(admin.Id, admin.DisplayName,
            AuditActionType.Update, $"Updated user {target.Email}", ip, ct);
        return new(true, $"User {target.DisplayName} updated.");
    }

    public async Task<AdminUserResult> DeleteAsync(
        Guid id,
        UserDto admin,
        string ip,
        CancellationToken ct = default)
    {
        var target = await _userService.GetByIdAsync(id, ct);
        if (target == null)
            return new(false, NotFound: true);

        var (succeeded, error) = await _userService.DeleteAsync(id, ct);
        if (!succeeded)
            return new(false, error);

        await _auditService.LogAsync(admin.Id, admin.DisplayName,
            AuditActionType.Delete, $"Deleted user {target.DisplayName}", ip, ct);
        return new(true, $"User {target.DisplayName} deleted.");
    }
}