namespace Rezber.Core.Settings;

public class MongoDbSettings
{
    public string Host { get; init; } = null!;
    public int Port { get; init; }
    public string User { get; init; } = null!;
    public string Password { get; init; } = null!;
    public string DatabaseName { get; init; } = null!;

    public string ConnectionString =>
        string.IsNullOrEmpty(User) || string.IsNullOrEmpty(Password)
            ? $"mongodb://{Host}:{Port}"
            : $"mongodb://{User}:{Password}@{Host}:{Port}/{DatabaseName}?authSource=admin";
}