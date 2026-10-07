
using Microsoft.Extensions.DependencyInjection;

namespace Rezber.Core.Attributs;

[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class AutoInjectAttribute : Attribute
{
    /// <summary>
    /// Service lifetime (Singleton, Scoped, or Transient).
    /// </summary>
    public ServiceLifetime Lifetime { get; set; } = ServiceLifetime.Scoped;

    /// <summary>
    /// Service type, when explicitly specified.
    /// </summary>
    public Type? ServiceType { get; set; }

    /// <summary>
    /// Whether this service should be injected lazily.
    /// </summary>
    public bool Lazy { get; set; } = false;

    public AutoInjectAttribute() { }

    public AutoInjectAttribute(ServiceLifetime lifetime)
    {
        Lifetime = lifetime;
    }

    public AutoInjectAttribute(Type serviceType)
    {
        ServiceType = serviceType;
    }

    public AutoInjectAttribute(Type serviceType, ServiceLifetime lifetime)
    {
        ServiceType = serviceType;
        Lifetime = lifetime;
    }
}
