using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace LouisAgent.Web.Shared.Text;

/// <summary>
/// The images a markdown text shows, read with the parser <see cref="Markdown.ToHtml"/> renders with (FX22), so "an image" means the same on both sides: a
/// tool result's images are the ones an answer may show. Only http(s) URLs count, and a plain link is no image.
/// </summary>
public static class ImageLinks
{
    public static IReadOnlyList<string> Of(string? markdown) =>
        string.IsNullOrEmpty(markdown)
            ? []
            : [.. Markdown.Parse(markdown).Descendants<LinkInline>()
                .Where(link => link.IsImage && Uri.TryCreate(link.Url, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https")
                .Select(link => link.Url!)
                .Distinct()];
}
