namespace Rezber.Core.Settings;

public sealed class NexusSettings
{
    public bool Enabled { get; init; }
    public string BaseUrl { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string ApiKey { get; init; } = string.Empty;
    public string? Repository { get; init; }
}