
namespace Rezber.Domain.Abstracts;

public enum AuditActionType
{
    Upload = 1,
    Update = 2,
    Delete = 3,
    Cleanup = 4,
    Login = 5,
    Logout = 6,
    Create = 7
}

public static class AuditActionTypeExtensions
{
    public static string ToPersianLabel(this AuditActionType action) => action switch
    {
        AuditActionType.Upload => "Upload",
        AuditActionType.Update => "Update",
        AuditActionType.Delete => "Delete",
        AuditActionType.Cleanup => "Cleanup",
        AuditActionType.Login => "Sign in",
        AuditActionType.Logout => "Sign out",
        AuditActionType.Create => "Create",
        _ => "Unknown"
    };

    public static string ToCssColor(this AuditActionType action) => action switch
    {
        AuditActionType.Upload => "success",
        AuditActionType.Update => "info",
        AuditActionType.Delete => "danger",
        AuditActionType.Cleanup => "purple",
        AuditActionType.Login => "neutral",
        AuditActionType.Logout => "neutral",
        AuditActionType.Create => "success",
        _ => "neutral"
    };
}
