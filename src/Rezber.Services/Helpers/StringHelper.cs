
namespace Rezber.Services.Helpers;

public static class BuilderCommands
{
    public static string GetCommand(string name, string lang, string? version = null)
    {
        var normalizedLanguage = lang.Trim().ToLowerInvariant();
        var versionSuffix = string.IsNullOrWhiteSpace(version) ? string.Empty : $" --version {version.Trim()}";

        return normalizedLanguage switch
        {
            "csharp" or "dotnet" or "nuget" => $"rezber package install {name}{versionSuffix}",
            "js" or "javascript" or "typescript" => $"npm install {name}{(string.IsNullOrWhiteSpace(version) ? string.Empty : $"@{version.Trim()}")}",
            "python" => $"pip install {name}{(string.IsNullOrWhiteSpace(version) ? string.Empty : $"=={version.Trim()}")}",
            "go" => $"go get {name}{(string.IsNullOrWhiteSpace(version) ? string.Empty : $"@{version.Trim()}")}",
            _ => $"rezber package install {name}{versionSuffix}"
        };
    }
}