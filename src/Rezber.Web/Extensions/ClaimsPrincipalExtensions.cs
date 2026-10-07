
using System.Security.Claims;

namespace Rezber.Web.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid? GetUserId(this ClaimsPrincipal user)
    {
        var id = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(id, out var guid) ? guid : null;
    }

    public static string GetDisplayName(this ClaimsPrincipal user)
        => user.FindFirstValue("DisplayName")
        ?? user.Identity?.Name
        ?? "User";

    public static string GetEmail(this ClaimsPrincipal user)
        => user.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
}
