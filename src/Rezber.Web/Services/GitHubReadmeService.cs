using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Rezber.Services.Features;
using Rezber.Services.Helpers;

namespace Rezber.Web.Services;

public sealed class RepositoryReadmeService(HttpClient httpClient) : IRepositoryReadmeService
{
    private const int MaxReadmeBytes = 1_048_576;

    public async Task<string?> FetchAsync(
        string repositoryUrl,
        string? readmeUrl,
        CancellationToken ct = default)
    {
        if (!Uri.TryCreate(repositoryUrl, UriKind.Absolute, out var uri) ||
            !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
            !string.IsNullOrEmpty(uri.UserInfo))
            throw new InvalidOperationException("Use a valid HTTPS repository URL.");

        if (!string.IsNullOrWhiteSpace(readmeUrl))
        {
            if (!Uri.TryCreate(readmeUrl, UriKind.Absolute, out var rawReadmeUri) ||
                !string.Equals(rawReadmeUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
                !string.IsNullOrEmpty(rawReadmeUri.UserInfo) ||
                !string.Equals(rawReadmeUri.Authority, uri.Authority, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("README URL must use HTTPS and the same repository host and port.");

            using var rawRequest = new HttpRequestMessage(HttpMethod.Get, rawReadmeUri);
            var rawReadme = await ReadReadmeAsync(rawRequest, ct);
            if (rawReadme == null)
                throw new InvalidOperationException("The README URL could not be found.");
            if (PackageReadmeRenderer.IsHtmlDocument(rawReadme))
                throw new InvalidOperationException("The README URL returned a web page, not a raw README file.");
            return rawReadme;
        }

        if (string.Equals(uri.IdnHost, "github.com", StringComparison.OrdinalIgnoreCase))
        {
            if (!TryGetGitHubRepositoryPath(uri, out var owner, out var repository))
                throw new InvalidOperationException("Enter a GitHub repository URL in the format https://github.com/owner/repository.");

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"https://api.github.com/repos/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(repository)}/readme");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.raw+json"));
            return await ReadReadmeAsync(request, ct);
        }

        if (TryGetGitLabProjectPath(uri, out var projectPath))
        {
            foreach (var fileName in new[] { "README.md", "README", "README.markdown", "README.rst" })
            {
                var project = Uri.EscapeDataString(projectPath);
                var path = Uri.EscapeDataString(fileName);
                var url = $"{uri.GetLeftPart(UriPartial.Authority)}/api/v4/projects/{project}/repository/files/{path}/raw?ref=HEAD";
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                string? readme;
                try
                {
                    readme = await ReadReadmeAsync(request, ct);
                }
                catch (HttpRequestException)
                {
                    return null;
                }
                if (readme != null)
                    return readme;
            }

            return null;
        }

        return null;
    }

    private async Task<string?> ReadReadmeAsync(HttpRequestMessage request, CancellationToken ct)
    {
        using var response = await httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            ct);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();

        if (response.Content.Headers.ContentLength > MaxReadmeBytes)
            throw new InvalidOperationException("README files larger than 1 MB cannot be imported.");

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var buffer = new MemoryStream();
        var chunk = new byte[8192];
        int bytesRead;

        while ((bytesRead = await stream.ReadAsync(chunk.AsMemory(), ct)) > 0)
        {
            if (buffer.Length + bytesRead > MaxReadmeBytes)
                throw new InvalidOperationException("README files larger than 1 MB cannot be imported.");

            await buffer.WriteAsync(chunk.AsMemory(0, bytesRead), ct);
        }

        var content = Encoding.UTF8.GetString(buffer.ToArray());
        if (PackageReadmeRenderer.IsHtmlDocument(content))
            throw new InvalidOperationException("The README URL returned a web page, not a raw README file.");

        return content;
    }

    private static bool TryGetGitHubRepositoryPath(Uri uri, out string owner, out string repository)
    {
        owner = string.Empty;
        repository = string.Empty;

        var segments = uri.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length < 2)
            return false;

        owner = Uri.UnescapeDataString(segments[0]);
        repository = Uri.UnescapeDataString(segments[1]);
        if (repository.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
            repository = repository[..^4];

        return IsSafeSegment(owner) && IsSafeSegment(repository);
    }

    private static bool TryGetGitLabProjectPath(Uri uri, out string projectPath)
    {
        projectPath = string.Empty;
        var segments = uri.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        var separatorIndex = Array.IndexOf(segments, "-");
        if (separatorIndex >= 0)
            segments = segments[..separatorIndex];

        if (segments.Length < 2)
            return false;

        var decodedSegments = segments.Select(Uri.UnescapeDataString).ToArray();
        if (decodedSegments[^1].EndsWith(".git", StringComparison.OrdinalIgnoreCase))
            decodedSegments[^1] = decodedSegments[^1][..^4];

        if (decodedSegments.Any(segment => !IsSafeSegment(segment)))
            return false;

        projectPath = string.Join('/', decodedSegments);
        return true;
    }

    private static bool IsSafeSegment(string value) =>
        value.Length is > 0 and <= 100 &&
        value is not "." and not ".." &&
        value.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_' or '.');
}
