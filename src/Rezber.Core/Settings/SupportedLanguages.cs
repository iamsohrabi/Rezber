
namespace Rezber.Core.Settings;

/// <summary>
/// Languages supported by the system.
/// </summary>
public static class SupportedLanguages
{
    public const string Persian = "fa";
    public const string English = "en";
    public const string Arabic = "ar";
    public const string Turkish = "tr";
    public const string French = "fr";
    public const string Spanish = "es";

    public static readonly List<string> All = new()
    {
        Persian, English, Arabic, Turkish, French, Spanish
    };

    public static string GetLanguageName(string code)
    {
        return code switch
        {
            Persian => "Persian",
            English => "English",
            Arabic => "Arabic",
            Turkish => "Türkçe",
            French => "Français",
            Spanish => "Español",
            _ => code
        };
    }
}

