using Rezber.Domain.Models;
using Rezber.Web.Services;

namespace Rezber.Web.Extensions;

public static class CurrentUserExtensions
{
    public static UserDto GetCurrentUser(this HttpContext context)
    {
        var accessor = context.RequestServices.GetRequiredService<ICurrentUserAccessor>();
        return accessor.GetAsync().GetAwaiter().GetResult();        
    }

    public static bool IsCurrentUserAdmin(this HttpContext context)
    {
        var accessor = context.RequestServices.GetRequiredService<ICurrentUserAccessor>();
        return accessor.IsAdminAsync().GetAwaiter().GetResult();
    }
}
