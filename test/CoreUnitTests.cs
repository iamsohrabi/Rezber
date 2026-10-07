using Rezber.Core.Domain;
using Rezber.Core.Helpers;
using Rezber.Services.Helpers;
using Xunit;

namespace Rezber.Tests;

public class CoreUnitTests
{
    [Theory]
    [InlineData(null, 1)]
    [InlineData("", 1)]
    [InlineData(" \n\t ", 1)]
    [InlineData("one two three", 1)]
    public void CalculateReadTime_ReturnsAtLeastOneMinute(string? content, int expected)
    {
        Assert.Equal(expected, content!.CalculateReadTime());
    }

    [Theory]
    [InlineData(200, 1)]
    [InlineData(201, 2)]
    [InlineData(400, 2)]
    [InlineData(401, 3)]
    public void CalculateReadTime_RoundsUpByWords(int wordCount, int expected)
    {
        var content = string.Join(' ', Enumerable.Repeat("word", wordCount));

        Assert.Equal(expected, content.CalculateReadTime());
    }

    [Fact]
    public void PagedResult_ComputesPageCountAndNavigation()
    {
        var result = new PagedResult<string>
        {
            Items = ["item"],
            TotalCount = 21,
            Page = 2,
            PageSize = 10
        };

        Assert.Equal(3, result.TotalPages);
        Assert.True(result.HasPreviousPage);
        Assert.True(result.HasNextPage);
    }

    [Fact]
    public void PagedResult_LastPageHasNoNextPage()
    {
        var result = new PagedResult<string>
        {
            Items = [],
            TotalCount = 21,
            Page = 3,
            PageSize = 10
        };

        Assert.True(result.HasPreviousPage);
        Assert.False(result.HasNextPage);
    }

    [Fact]
    public void PackageReadmeRenderer_RendersEmbeddedHtml()
    {
        var html = PackageReadmeRenderer.ToSafeHtml("<p>README content</p>");

        Assert.Contains("<p>README content</p>", html);
        Assert.DoesNotContain("&lt;p&gt;", html);
    }

    [Fact]
    public void PackageReadmeRenderer_RemovesUnsafeHtml()
    {
        var html = PackageReadmeRenderer.ToSafeHtml(
            "<p>README content</p><script>alert(1)</script><a href=\"javascript:alert(1)\">unsafe</a>");

        Assert.Contains("<p>README content</p>", html);
        Assert.DoesNotContain("<script", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("javascript:", html, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("<!DOCTYPE html><html><body>GitHub page</body></html>")]
    [InlineData("  <html><body>GitHub page</body></html>")]
    public void PackageReadmeRenderer_IdentifiesFullHtmlDocuments(string content)
    {
        Assert.True(PackageReadmeRenderer.IsHtmlDocument(content));
    }

    [Fact]
    public void PackageReadmeRenderer_DoesNotIdentifyMarkdownAsHtmlDocument()
    {
        Assert.False(PackageReadmeRenderer.IsHtmlDocument("# Package README"));
    }

    [Theory]
    [InlineData("متن فارسی با واژه‌های English")]
    [InlineData("עברית")]
    public void PackageReadmeRenderer_DetectsRightToLeftText(string content)
    {
        Assert.True(PackageReadmeRenderer.ContainsRightToLeftText(content));
    }

    [Fact]
    public void PackageReadmeRenderer_DoesNotDetectRightToLeftTextInEnglish()
    {
        Assert.False(PackageReadmeRenderer.ContainsRightToLeftText("English README"));
    }
}