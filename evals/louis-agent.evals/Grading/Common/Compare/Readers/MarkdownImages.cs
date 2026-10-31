using LouisAgent.Evals.Readers;

namespace LouisAgent.Evals.Grading.Common.Compare.Readers;

/// <summary>
/// Reads every image a text writes as <c>![alt](https://…)</c>, in order, an image inside a link too: what the
/// fetched pages and the agent's answers both use. It leaves nothing out; a domain that knows a page's logos and share
/// buttons aren't images of its subject ships its own reader.
/// </summary>
/// <remarks>
/// The plain image reader any eval can name, <c>markdown-images</c>. It reads the same images Puls's
/// <c>Links.ImageUrls</c> pattern did (<c>!\[[^\]]*\]\((https?://[^)\s]+)\)</c>), by walking the text instead (ADR
/// 0057): the alt text runs to the first closing bracket, and the address to its closing parenthesis, with no space.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0057-regex-is-the-last-option.md">ADR 0057: a regular expression is the last option</seealso>
public sealed class MarkdownImages : IImageReader
{
    /// <summary>What opens an image.</summary>
    private const string Opening = "![";

    /// <summary>The schemes an image's address may have: a web address only.</summary>
    private static readonly string[] Schemes = ["https://", "http://"];

    /// <inheritdoc />
    public string Name => "markdown-images";

    /// <inheritdoc />
    public IReadOnlyList<string> Images(string text)
    {
        var found = new List<string>();
        var at = text.IndexOf(Opening, StringComparison.Ordinal);
        while (at >= 0)
        {
            var image = ImageAt(text, at);
            if (image is { } read) found.Add(read.Url);
            // Read on after this image, or after this opening when it wasn't one.
            at = text.IndexOf(Opening, image?.End + 1 ?? at + 1, StringComparison.Ordinal);
        }

        return found;
    }

    /// <summary>
    /// The image that opens at this point, and where its closing parenthesis is; null when it isn't a whole image
    /// with a web address.
    /// </summary>
    /// <remarks>Guard clauses, one way of not being an image each (ADR 0051).</remarks>
    private static (string Url, int End)? ImageAt(string text, int at)
    {
        // The alt text runs to the first closing bracket, which must be followed by the address's parenthesis.
        var close = text.IndexOf(']', at + Opening.Length);
        if (close < 0 || close + 1 >= text.Length || text[close + 1] != '(') return null;

        // The address runs to its closing parenthesis; a space or a line break first means it isn't one.
        var start = close + 2;
        var end = start;
        while (end < text.Length && text[end] != ')' && !char.IsWhiteSpace(text[end])) end++;
        if (end >= text.Length || text[end] != ')') return null;

        var url = text[start..end];
        return IsWebAddress(url) ? (url, end) : null;
    }

    /// <summary>An address that starts with a web scheme and has something after it.</summary>
    private static bool IsWebAddress(string url) =>
        Schemes.Any(scheme => url.Length > scheme.Length && url.StartsWith(scheme, StringComparison.Ordinal));
}
