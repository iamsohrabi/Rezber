using Rezber.Core.Attributs;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Rezber.Core.Helpers;

public interface ICurrentSite
{
    Guid? SiteId { get; set; }
    string? CurrentLanguage { get; set; }
    object? Data { get; }
    bool IsAdminRequest { get; set; }
}

[AutoInject(ServiceLifetime.Scoped)]
public class CurrentSite : ICurrentSite
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private const string SiteIdKey = "X-Site-Id";
    private const string IsAdminKey = "IsAdminRequest";

    public CurrentSite(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public object? Data
        => _httpContextAccessor.HttpContext?.Items["Site"];

    public Guid? SiteId
    {
        get
        {
            if (_httpContextAccessor.HttpContext?.Items.TryGetValue(SiteIdKey, out var data) == true)
                return data as Guid?;
            return null;
        }
        set
        {
            if (_httpContextAccessor.HttpContext != null)
                _httpContextAccessor.HttpContext.Items[SiteIdKey] = value;
        }
    }

    public string? CurrentLanguage
    {
        get
        {
            if (_httpContextAccessor.HttpContext?.Items.TryGetValue("CurrentLanguage", out var data) == true)
                return data as string ?? data!.ToString();
            return null;
        }
        set
        {
            if (_httpContextAccessor.HttpContext != null)
                _httpContextAccessor.HttpContext.Items["CurrentLanguage"] = value;
        }
    }

    public bool IsAdminRequest
    {
        get
        {
            if (_httpContextAccessor.HttpContext?.Items.TryGetValue(IsAdminKey, out var data) == true)
                return data is bool boolValue && boolValue;
            return false;
        }
        set
        {
            if (_httpContextAccessor.HttpContext != null)
                _httpContextAccessor.HttpContext.Items[IsAdminKey] = value;
        }
    }
}
