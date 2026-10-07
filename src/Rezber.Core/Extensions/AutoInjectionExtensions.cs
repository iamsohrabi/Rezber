
using System.Reflection;
using Rezber.Core.Attributs;
using Microsoft.Extensions.DependencyInjection;

namespace Rezber.Core.Extensions;

public static class AutoInjectionExtensions
{
    /// <summary>
    /// Scan assemblies and automatically register services with AutoInject.
    /// </summary>
    public static IServiceCollection AddAutoInjectedServices(this IServiceCollection services, params Assembly[] assemblies)
    {
        var targetAssemblies = assemblies.Any() ? assemblies : new[] { Assembly.GetCallingAssembly() };

        foreach (var assembly in targetAssemblies)
        {
            // Find all types with AutoInjectAttribute.
            var types = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && t.IsPublic)
                .Select(t => new
                {
                    ImplementationType = t,
                    Attribute = t.GetCustomAttribute<AutoInjectAttribute>()
                })
                .Where(x => x.Attribute != null)
                .ToList();

            foreach (var type in types)
            {
                var attr = type.Attribute!;

                // Determine the service type.
                var serviceType = attr.ServiceType ?? GetDefaultServiceType(type.ImplementationType);

                if (serviceType == null)
                {
                    // Register the class when no matching interface exists.
                    RegisterService(services, type.ImplementationType, type.ImplementationType, attr.Lifetime);
                }
                else
                {
                    RegisterService(services, serviceType, type.ImplementationType, attr.Lifetime);
                }

                // Register a lazy service when enabled.
                if (attr.Lazy)
                {
                    RegisterLazyService(services, serviceType ?? type.ImplementationType, type.ImplementationType, attr.Lifetime);
                }
            }
        }

        return services;
    }

    private static Type? GetDefaultServiceType(Type implementationType)
    {
        // Default rule: the matching interface is named I + ClassName.
        var interfaceName = $"I{implementationType.Name}";
        var serviceType = implementationType.GetInterface(interfaceName);

        if (serviceType != null)
            return serviceType;

        // Otherwise, return the first available interface.
        return implementationType.GetInterfaces().FirstOrDefault();
    }

    private static void RegisterService(IServiceCollection services, Type serviceType, Type implementationType, ServiceLifetime lifetime)
    {
        switch (lifetime)
        {
            case ServiceLifetime.Singleton:
                services.AddSingleton(serviceType, implementationType);
                break;
            case ServiceLifetime.Scoped:
                services.AddScoped(serviceType, implementationType);
                break;
            case ServiceLifetime.Transient:
                services.AddTransient(serviceType, implementationType);
                break;
        }
    }

    private static void RegisterLazyService(IServiceCollection services, Type serviceType, Type implementationType, ServiceLifetime lifetime)
    {
        // Register the lazy wrapper.
        var lazyType = typeof(Lazy<>).MakeGenericType(serviceType);

        switch (lifetime)
        {
            case ServiceLifetime.Singleton:
                services.AddSingleton(lazyType, sp =>
                    Activator.CreateInstance(lazyType, sp.GetRequiredService(serviceType))!);
                break;
            case ServiceLifetime.Scoped:
                services.AddScoped(lazyType, sp =>
                    Activator.CreateInstance(lazyType, sp.GetRequiredService(serviceType))!);
                break;
            case ServiceLifetime.Transient:
                services.AddTransient(lazyType, sp =>
                    Activator.CreateInstance(lazyType, sp.GetRequiredService(serviceType))!);
                break;
        }
    }
}
