namespace Rezber.Core.Settings;

public class MinioSettings
{
    public string Endpoint { get; set; } = string.Empty;
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string BucketName { get; set; } = string.Empty;
    public bool UseSSL { get; set; } = false;
    public int PresignedUrlExpiryMinutes { get; set; } = 15;
    public long? MaxFileSize { get; set; } = 10 * 1024 * 1024; // 10MB default
    public string[]? AllowedTypes { get; set; }
}
