using Microsoft.AspNetCore.Identity;
using Rezber.Domain.Identity;

namespace Rezber.Web.Services;

public static class IdentitySeed
{
    private const string AdminUserName = "Mehrdad";
    private const string AdminEmail = "mehrdad@rezber.local";
    private const string AdminPassword = "So123!@#";

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

        if (!await roleManager.RoleExistsAsync(Roles.Admin))
        {
            var roleResult = await roleManager.CreateAsync(new Role(Roles.Admin));
            if (!roleResult.Succeeded)
                throw new InvalidOperationException(FormatErrors("admin role", roleResult.Errors));
        }

        var user = await userManager.FindByNameAsync(AdminUserName)
                   ?? await userManager.FindByEmailAsync(AdminEmail);

        if (user == null)
        {
            user = new User(AdminUserName, AdminEmail)
            {
                Email = AdminEmail,
                DisplayName = AdminUserName,
                JobTitle = "System Administrator",
                EmailConfirmed = true,
                IsActive = true,
                LastActive = DateTime.UtcNow
            };

            var createResult = await userManager.CreateAsync(user, AdminPassword);
            if (!createResult.Succeeded)
                throw new InvalidOperationException(FormatErrors("admin user", createResult.Errors));
        }

        if (!await userManager.IsInRoleAsync(user, Roles.Admin))
        {
            var roleResult = await userManager.AddToRoleAsync(user, Roles.Admin);
            if (!roleResult.Succeeded)
                throw new InvalidOperationException(FormatErrors("admin role assignment", roleResult.Errors));
        }
    }

    private static string FormatErrors(string target, IEnumerable<IdentityError> errors)
    {
        return $"Unable to seed {target}: {string.Join(", ", errors.Select(error => error.Description))}";
    }
}