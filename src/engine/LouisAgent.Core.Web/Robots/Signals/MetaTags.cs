namespace LouisAgent.Core.Web.Robots.Signals;

/// <summary>
/// Finds what a page's <c>&lt;meta name="..." content="..."&gt;</c> tag says, whatever the case, the quotes or the
/// order of its attributes.
/// </summary>
/// <remarks>
/// A plain walk over the HTML with <c>IndexOf</c>, no regular expression and no HTML parser: a page's head is outside
/// text we don't control, and all we need is two attributes of one tag.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0057-regex-is-the-last-option.md">ADR 0057: regex is the last option</seealso>
internal static class MetaTags
{
    private const string Open = "<meta";

    /// <summary>
    /// The content of every meta tag with this name, in page order.
    /// </summary>
    public static IEnumerable<string> ContentsOf(string html, string name)
    {
        int from = 0;
        while (html.IndexOf(Open, from, StringComparison.OrdinalIgnoreCase) is var start and >= 0)
        {
            int end = html.IndexOf('>', start);
            if (end < 0) yield break;

            // The tag must be "<meta" and then a space: "<metadata" is another element.
            from = end;
            string tag = html[start..end];
            if (tag.Length <= Open.Length || !char.IsWhiteSpace(tag[Open.Length])) continue;

            if (Attribute(tag, "name")?.Equals(name, StringComparison.OrdinalIgnoreCase) != true) continue;

            yield return Attribute(tag, "content") ?? "";
        }
    }

    /// <summary>
    /// The value of one attribute of a tag, with or without quotes; <c>data-name</c> is not <c>name</c>.
    /// </summary>
    private static string? Attribute(string tag, string attribute)
    {
        int from = 0;
        while (tag.IndexOf(attribute, from, StringComparison.OrdinalIgnoreCase) is var found and >= 0)
        {
            from = found + attribute.Length;

            // It must start an attribute (after a space or a quote) and be followed by "=".
            if (!char.IsWhiteSpace(tag[found - 1]) && tag[found - 1] is not ('"' or '\'')) continue;

            string afterName = tag[from..].TrimStart();
            if (!afterName.StartsWith('=')) continue;

            return ValueAt(afterName[1..].TrimStart());
        }

        return null;
    }

    /// <summary>
    /// The text up to the closing quote, or to the next space when the value has no quotes.
    /// </summary>
    private static string ValueAt(string value)
    {
        if (value.Length == 0) return "";

        char quote = value[0];
        if (quote is not ('"' or '\''))
        {
            int space = value.IndexOfAny([' ', '\t', '\r', '\n']);
            return space < 0 ? value : value[..space];
        }

        int close = value.IndexOf(quote, 1);
        return close < 0 ? value[1..] : value[1..close];
    }
}
