using Ganss.Xss;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace Rezber.Services.Helpers;

public static class PackageReadmeRenderer
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UsePipeTables()
        .UseTaskLists()
        .UseAutoLinks()
        .Build();
    private static readonly HtmlSanitizer Sanitizer = new();

    public static string ToSafeHtml(string markdown)
    {
        var document = Markdown.Parse(markdown, Pipeline);

        foreach (var link in document.Descendants<LinkInline>())
        {
            if (!IsSafeDestination(link.Url))
                link.Url = null;
        }

        return Sanitizer.Sanitize(Markdown.ToHtml(document, Pipeline));
    }

    public static bool IsHtmlDocument(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return false;

        var trimmedContent = content.TrimStart();
        return trimmedContent.StartsWith("<!doctype html", StringComparison.OrdinalIgnoreCase) ||
            trimmedContent.StartsWith("<html", StringComparison.OrdinalIgnoreCase);
    }

    public static bool ContainsRightToLeftText(string? content) =>
        !string.IsNullOrEmpty(content) && content.Any(character =>
            character is >= '\u0590' and <= '\u08FF' or
                >= '\uFB1D' and <= '\uFDFF' or
                >= '\uFE70' and <= '\uFEFF');

    private static bool IsSafeDestination(string? destination)
    {
        if (string.IsNullOrWhiteSpace(destination))
            return true;

        if (destination.StartsWith("//", StringComparison.Ordinal) ||
            destination.StartsWith('\\'))
            return false;

        if (!Uri.TryCreate(destination, UriKind.Absolute, out var uri))
            return true;

        return uri.Scheme is "http" or "https" or "mailto";
    }
}