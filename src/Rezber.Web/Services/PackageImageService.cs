using System.Text.Json;
using SkiaSharp;
using Rezber.Services.Features;

namespace Rezber.Web.Services;

public sealed class PackageImageService : IPackageImageService
{
    private const int MaxUploadBytes = 5 * 1024 * 1024;
    private const int MaxWebpBytes = 1024 * 1024;
    private readonly HttpClient _httpClient;

    public PackageImageService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<byte[]> ConvertToWebpAsync(Stream source, CancellationToken ct)
    {
        using var input = new MemoryStream();
        var buffer = new byte[81920];
        int bytesRead;
        while ((bytesRead = await source.ReadAsync(buffer, ct)) > 0)
        {
            if (input.Length + bytesRead > MaxUploadBytes)
                throw new InvalidDataException("Image files must be 5 MB or smaller.");

            await input.WriteAsync(buffer.AsMemory(0, bytesRead), ct);
        }

        input.Position = 0;
        using var bitmap = SKBitmap.Decode(input);
        if (bitmap == null || bitmap.Width > 4096 || bitmap.Height > 4096)
            throw new InvalidDataException("Choose a valid image no larger than 4096 x 4096 pixels.");

        using var image = SKImage.FromBitmap(bitmap);
        using var encoded = image.Encode(SKEncodedImageFormat.Webp, 82);
        if (encoded == null || encoded.Size > MaxWebpBytes)
            throw new InvalidDataException("The converted WebP image must be 1 MB or smaller.");

        return encoded.ToArray();
    }

    public async Task<byte[]?> GetNugetIconWebpAsync(string packageId, CancellationToken ct)
    {
        var searchUri = new Uri($"https://azuresearch-usnc.nuget.org/query?q=packageid%3A{Uri.EscapeDataString(packageId)}&take=10&prerelease=false");
        using var searchResponse = await _httpClient.GetAsync(searchUri, ct);
        if (!searchResponse.IsSuccessStatusCode)
            return null;

        await using var searchStream = await searchResponse.Content.ReadAsStreamAsync(ct);
        using var searchJson = await JsonDocument.ParseAsync(searchStream, cancellationToken: ct);
        if (!searchJson.RootElement.TryGetProperty("data", out var results))
            return null;

        string? iconUrl = null;
        foreach (var result in results.EnumerateArray())
        {
            if (result.TryGetProperty("id", out var id)
                && string.Equals(id.GetString(), packageId, StringComparison.OrdinalIgnoreCase)
                && result.TryGetProperty("iconUrl", out var icon)
                && Uri.TryCreate(icon.GetString(), UriKind.Absolute, out var candidate)
                && IsTrustedIconHost(candidate))
            {
                iconUrl = candidate.AbsoluteUri;
                break;
            }
        }

        if (iconUrl == null)
            return null;

        using var iconResponse = await _httpClient.GetAsync(iconUrl, HttpCompletionOption.ResponseHeadersRead, ct);
        if (!iconResponse.IsSuccessStatusCode
            || iconResponse.Content.Headers.ContentLength > MaxUploadBytes)
            return null;

        await using var iconStream = await iconResponse.Content.ReadAsStreamAsync(ct);
        using var boundedStream = new MemoryStream();
        var buffer = new byte[81920];
        int bytesRead;
        while ((bytesRead = await iconStream.ReadAsync(buffer, ct)) > 0)
        {
            if (boundedStream.Length + bytesRead > MaxUploadBytes)
                return null;
            await boundedStream.WriteAsync(buffer.AsMemory(0, bytesRead), ct);
        }

        boundedStream.Position = 0;
        try
        {
            return await ConvertToWebpAsync(boundedStream, ct);
        }
        catch (InvalidDataException)
        {
            return null;
        }
    }

    private static bool IsTrustedIconHost(Uri uri)
    {
        if (uri.Scheme != Uri.UriSchemeHttps)
            return false;

        var host = uri.IdnHost;
        return host.Equals("nuget.org", StringComparison.OrdinalIgnoreCase)
            || host.EndsWith(".nuget.org", StringComparison.OrdinalIgnoreCase)
            || host.Equals("githubusercontent.com", StringComparison.OrdinalIgnoreCase)
            || host.EndsWith(".githubusercontent.com", StringComparison.OrdinalIgnoreCase);
    }
}