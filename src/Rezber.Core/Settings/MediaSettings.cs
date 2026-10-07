namespace Rezber.Core.Settings;

public class MediaSettings
{
    public string BaseUrl { get; set; } = string.Empty;
    public long MaxFileSize { get; set; } = 10 * 1024 * 1024; // 10MB
    public string[] AllowedTypes { get; set; } = new[] { ".webp", ".gif", ".mp4", ".mp3", ".pdf" };
}
