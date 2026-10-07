namespace Rezber.Domain.Enums;

public enum RepositoryStatus
{
    Healthy = 1,
    Warning = 2,
    Critical = 3
}

public static class RepositoryStatusExtensions
{
    public static string ToPersianLabel(this RepositoryStatus status) => status switch
    {
        RepositoryStatus.Healthy => "Healthy",
        RepositoryStatus.Warning => "Cleanup required",
        RepositoryStatus.Critical => "Critical",
        _ => "Unknown"
    };

    public static string ToCssColor(this RepositoryStatus status) => status switch
    {
        RepositoryStatus.Healthy => "success",
        RepositoryStatus.Warning => "warning",
        RepositoryStatus.Critical => "danger",
        _ => "neutral"
    };
}