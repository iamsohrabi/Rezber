
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Rezber.Core.Extensions;
using Rezber.Core.Settings;
using Rezber.Domain.Identity;

namespace Rezber.Web.Services;

public static class ApplicationServicesExtensions
{
    public static IServiceCollection AddApplicationService(this IServiceCollection services)
    {
        services.AddAutoInjectedServices(
            Rezber.Services.AssemblyMarker.AssemblyName,
            Core.AssemblyMarker.AssemblyType);

        services.AddScoped<ICurrentUserAccessor, CurrentUserAccessor>();
            
        return services;
    }

    public static IServiceCollection AddIdentityService(
        this IServiceCollection services,
        MongoDbSettings mongoDbSettings)
    {
        services.AddIdentity<User, Role>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequiredLength = 6;
        })
        .AddMongoDbStores<User, Role, Guid>(
            mongoDbSettings.ConnectionString,
            mongoDbSettings.DatabaseName);

        return services;
    }

    public static IServiceCollection AddAuthenticationService(this IServiceCollection services)
    {
        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.LoginPath = "/account/login";
                options.AccessDeniedPath = "/account/accessdenied";
            });

        return services;
    }

}
