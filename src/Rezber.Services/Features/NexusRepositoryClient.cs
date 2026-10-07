using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Rezber.Core.Settings;

namespace Rezber.Services.Features;

public interface INexusRepositoryClient
{
    Task UploadPackageAsync(
        Stream packageStream,
        string packageName,
        string packageVersion,
        string fileName,
        CancellationToken ct = default);
    Task<byte[]?> DownloadPackageAsync(
        string packageName,
        string packageVersion,
        CancellationToken ct = default);
    Task<NexusStatus> GetStatusAsync(CancellationToken ct = default);
    Task<bool> IsWritableAsync(CancellationToken ct = default);
    Task<IReadOnlyCollection<NexusRepository>> GetRepositoriesAsync(CancellationToken ct = default);
    Task<NexusRepository> GetRepositoryAsync(string repositoryName, CancellationToken ct = default);
    Task<NexusSearchResult> SearchAsync(NexusSearchQuery query, CancellationToken ct = default);
    Task<NexusSearchResult> SearchAssetsAsync(NexusSearchQuery query, CancellationToken ct = default);
    Task<NexusComponent> GetComponentAsync(string id, CancellationToken ct = default);
    Task<NexusAsset> GetAssetAsync(string id, CancellationToken ct = default);
    Task DeleteComponentAsync(string id, CancellationToken ct = default);
    Task DeleteAssetAsync(string id, CancellationToken ct = default);
    Task RunHealthCheckAsync(string repositoryName, CancellationToken ct = default);
    Task StopHealthCheckAsync(string repositoryName, CancellationToken ct = default);
}

public sealed class NexusRepositoryClient : INexusRepositoryClient
{
    private readonly HttpClient _httpClient;
    private readonly NexusSettings _settings;

    public NexusRepositoryClient(HttpClient httpClient, IOptions<NexusSettings> options)
    {
        _httpClient = httpClient;
        _settings = options.Value;

        if (!string.IsNullOrWhiteSpace(_settings.Username))
        {
            var credentials = Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{_settings.Username}:{_settings.Password}"));
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", credentials);
        }
    }

    public async Task UploadPackageAsync(
        Stream packageStream,
        string packageName,
        string packageVersion,
        string fileName,
        CancellationToken ct = default)
    {
        EnsureEnabled();

        if (string.IsNullOrWhiteSpace(_settings.Repository))
            throw new InvalidOperationException("NexusSettings:Repository is required for uploads.");
        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
            throw new InvalidOperationException("NexusSettings:ApiKey is required for NuGet uploads.");

        var safeFileName = Path.GetFileName(fileName);
        if (string.IsNullOrWhiteSpace(packageName) || string.IsNullOrWhiteSpace(packageVersion))
            throw new InvalidOperationException("Package name and version are required for uploads.");

        var packageContent = new StreamContent(packageStream);
        packageContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        using var content = new MultipartFormDataContent();
        content.Add(packageContent, "package", safeFileName);

        using var request = new HttpRequestMessage(
            HttpMethod.Put,
            $"repository/{Uri.EscapeDataString(_settings.Repository)}/")
        {
            Content = content
        };
        request.Headers.TryAddWithoutValidation("X-NuGet-ApiKey", _settings.ApiKey.Trim());

        using var response = await _httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task<byte[]?> DownloadPackageAsync(
        string packageName,
        string packageVersion,
        CancellationToken ct = default)
    {
        EnsureEnabled();

        var result = await SearchAssetsAsync(new NexusSearchQuery
        {
            Repository = _settings.Repository,
            Format = "nuget",
            Name = packageName,
            Version = packageVersion
        }, ct);
        var asset = result.Items
            .SelectMany(item => item.Assets)
            .FirstOrDefault(item => item.Path.EndsWith(".nupkg", StringComparison.OrdinalIgnoreCase));
        if (asset == null)
            return null;

        if (!Uri.TryCreate(asset.DownloadUrl, UriKind.Absolute, out var downloadUri) ||
            _httpClient.BaseAddress == null ||
            !string.Equals(downloadUri.GetLeftPart(UriPartial.Authority),
                _httpClient.BaseAddress.GetLeftPart(UriPartial.Authority), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Nexus returned an invalid package download URL.");

        using var response = await _httpClient.GetAsync(
            downloadUri, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        const long maxPackageSize = 100_000_000;
        if (response.Content.Headers.ContentLength > maxPackageSize)
            throw new InvalidOperationException("The package exceeds the 100 MB download limit.");

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        int bytesRead;
        while ((bytesRead = await stream.ReadAsync(chunk, ct)) > 0)
        {
            if (buffer.Length + bytesRead > maxPackageSize)
                throw new InvalidOperationException("The package exceeds the 100 MB download limit.");

            await buffer.WriteAsync(chunk.AsMemory(0, bytesRead), ct);
        }

        return buffer.ToArray();
    }

    public async Task<NexusStatus> GetStatusAsync(CancellationToken ct = default)
    {
        EnsureEnabled();

        using var response = await _httpClient.GetAsync("service/rest/v1/status", ct);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync(ct);
        if (string.IsNullOrWhiteSpace(body))
            return new NexusStatus("Unknown", "Nexus Repository", "Available");

        try
        {
            return JsonSerializer.Deserialize<NexusStatus>(body)
                ?? new NexusStatus("Unknown", "Nexus Repository", "Available");
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException("Nexus status endpoint returned an invalid response.", exception);
        }
    }

    public async Task<bool> IsWritableAsync(CancellationToken ct = default)
    {
        EnsureEnabled();
        using var response = await _httpClient.GetAsync("service/rest/v1/status/writable", ct);
        return response.IsSuccessStatusCode;
    }

    public async Task<IReadOnlyCollection<NexusRepository>> GetRepositoriesAsync(
        CancellationToken ct = default)
    {
        var repositories = await GetAsync<List<NexusRepository>>(
            "service/rest/v1/repositories", ct);

        return repositories;
    }

    public Task<NexusRepository> GetRepositoryAsync(
        string repositoryName,
        CancellationToken ct = default) =>
        GetAsync<NexusRepository>(
            $"service/rest/v1/repositories/{Uri.EscapeDataString(repositoryName)}", ct);

    public Task<NexusSearchResult> SearchAsync(
        NexusSearchQuery query,
        CancellationToken ct = default) =>
        SearchAsync("search", query, ct);

    public Task<NexusSearchResult> SearchAssetsAsync(
        NexusSearchQuery query,
        CancellationToken ct = default) =>
        SearchAsync("search/assets", query, ct);

    public Task<NexusComponent> GetComponentAsync(string id, CancellationToken ct = default) =>
        GetAsync<NexusComponent>($"service/rest/v1/components/{Uri.EscapeDataString(id)}", ct);

    public Task<NexusAsset> GetAssetAsync(string id, CancellationToken ct = default) =>
        GetAsync<NexusAsset>($"service/rest/v1/assets/{Uri.EscapeDataString(id)}", ct);

    public Task DeleteComponentAsync(string id, CancellationToken ct = default) =>
        SendAsync(HttpMethod.Delete, $"service/rest/v1/components/{Uri.EscapeDataString(id)}", ct);

    public Task DeleteAssetAsync(string id, CancellationToken ct = default) =>
        SendAsync(HttpMethod.Delete, $"service/rest/v1/assets/{Uri.EscapeDataString(id)}", ct);

    public Task RunHealthCheckAsync(string repositoryName, CancellationToken ct = default) =>
        SendAsync(HttpMethod.Post,
            $"service/rest/v1/repositories/{Uri.EscapeDataString(repositoryName)}/health-check", ct);

    public Task StopHealthCheckAsync(string repositoryName, CancellationToken ct = default) =>
        SendAsync(HttpMethod.Delete,
            $"service/rest/v1/repositories/{Uri.EscapeDataString(repositoryName)}/health-check", ct);

    private Task<NexusSearchResult> SearchAsync(
        string endpoint,
        NexusSearchQuery query,
        CancellationToken ct)
    {
        var parameters = new List<string>();
        AddQueryParameter(parameters, "q", query.Q);
        AddQueryParameter(parameters, "repository", query.Repository ?? _settings.Repository);
        AddQueryParameter(parameters, "format", query.Format);
        AddQueryParameter(parameters, "name", query.Name);
        AddQueryParameter(parameters, "version", query.Version);
        AddQueryParameter(parameters, "group", query.Group);
        AddQueryParameter(parameters, "continuationToken", query.ContinuationToken);

        return GetAsync<NexusSearchResult>(
            $"service/rest/v1/{endpoint}?{string.Join('&', parameters)}", ct);
    }

    private async Task<T> GetAsync<T>(string path, CancellationToken ct)
    {
        EnsureEnabled();

        using var response = await _httpClient.GetAsync(path, ct);
        response.EnsureSuccessStatusCode();

        try
        {
            return await response.Content.ReadFromJsonAsync<T>(cancellationToken: ct)
                ?? throw new InvalidOperationException($"Nexus returned an empty response for '{path}'.");
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException($"Nexus returned an empty or invalid JSON response for '{path}'.", exception);
        }
    }

    private async Task SendAsync(HttpMethod method, string path, CancellationToken ct)
    {
        EnsureEnabled();
        using var response = await _httpClient.SendAsync(new HttpRequestMessage(method, path), ct);
        response.EnsureSuccessStatusCode();
    }

    private void EnsureEnabled()
    {
        if (!_settings.Enabled)
            throw new InvalidOperationException("Nexus integration is disabled.");
    }

    private static void AddQueryParameter(
        ICollection<string> parameters,
        string name,
        string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            parameters.Add($"{name}={Uri.EscapeDataString(value.Trim())}");
    }
}

public sealed class NexusSearchQuery
{
    public string? Q { get; init; }
    public string? Repository { get; init; }
    public string? Format { get; init; }
    public string? Name { get; init; }
    public string? Version { get; init; }
    public string? Group { get; init; }
    public string? ContinuationToken { get; init; }
}

public sealed record NexusStatus(
    [property: JsonPropertyName("version")] string Version,
    [property: JsonPropertyName("edition")] string Edition,
    [property: JsonPropertyName("state")] string State);

public sealed record NexusRepository(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("format")] string Format,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("url")] string Url,
    [property: JsonPropertyName("online")] bool Online);

public sealed record NexusComponent(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("repository")] string Repository,
    [property: JsonPropertyName("format")] string Format,
    [property: JsonPropertyName("group")] string? Group,
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("version")] string? Version,
    [property: JsonPropertyName("assets")] IReadOnlyCollection<NexusAsset> Assets);

public sealed class NexusSearchResult
{
    [JsonPropertyName("items")]
    public IReadOnlyCollection<NexusSearchItem> Items { get; init; } = Array.Empty<NexusSearchItem>();

    [JsonPropertyName("continuationToken")]
    public string? ContinuationToken { get; init; }
}

public sealed record NexusSearchItem(
    [property: JsonPropertyName("repository")] string Repository,
    [property: JsonPropertyName("format")] string Format,
    [property: JsonPropertyName("group")] string? Group,
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("version")] string? Version,
    [property: JsonPropertyName("assets")] IReadOnlyCollection<NexusAsset> Assets);

public sealed record NexusAsset(
    [property: JsonPropertyName("downloadUrl")] string DownloadUrl,
    [property: JsonPropertyName("path")] string Path,
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("repository")] string Repository,
    [property: JsonPropertyName("format")] string Format,
    [property: JsonPropertyName("contentType")] string ContentType,
    [property: JsonPropertyName("fileSize")] long? FileSize,
    [property: JsonPropertyName("lastModified")] DateTimeOffset? LastModified,
    [property: JsonPropertyName("lastDownloaded")] DateTimeOffset? LastDownloaded,
    [property: JsonPropertyName("checksum")] IReadOnlyDictionary<string, object>? Checksum);