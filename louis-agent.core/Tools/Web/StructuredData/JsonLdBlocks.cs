using System.Net;
using System.Text.Json;

namespace LouisAgent.Core.Tools.Web.StructuredData;

/// <summary>
/// Gives you the data a page publishes for machines: every JSON-LD block
/// (<c>&lt;script type="application/ld+json"&gt;</c>) of the HTML, in page order, already decoded and parsed. A block
/// that isn't valid JSON is skipped. The caller owns the documents and disposes them. A raw
/// <c>&lt;/script&gt;</c> can only be the end tag, because JSON escapes the slash
/// (<c>&lt;\/script&gt;</c>) inside a string.
/// </summary>
/// <remarks>
/// A plain walk over the text with <c>IndexOf</c>, no regular expression: the block's end is the next closing script
/// tag, so there is nothing for a pattern to backtrack on, and every caller (the lead image, a recipe reader) shares
/// this one reader instead of keeping its own pattern.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0057-regex-is-the-last-option.md">ADR 0057: regex is the last option</seealso>
public static class JsonLdBlocks
{
    private const string LdJson = "application/ld+json";

    /// <summary>
    /// Every JSON-LD block of the page that parses, in page order. The caller disposes the documents.
    /// </summary>
    public static IReadOnlyList<JsonDocument> In(string html)
    {
        var blocks = new List<JsonDocument>();
        int position = 0;
        while (NextScript(html, position) is { } script)
        {
            // Carry on after this script's end, whether or not it was ours.
            position = script.End;
            if (!IsLdJson(html[script.Start..script.TagEnd])) continue;

            if (Parse(html[(script.TagEnd + 1)..script.End]) is { } document) blocks.Add(document);
        }

        return blocks;
    }

    /// <summary>
    /// Finds the next script element from a position: where it starts, where its tag ends, where its body ends.
    /// </summary>
    private static Script? NextScript(string html, int from)
    {
        int start = html.IndexOf("<script", from, StringComparison.OrdinalIgnoreCase);
        if (start < 0) return null;

        int tagEnd = html.IndexOf('>', start);
        if (tagEnd < 0) return null;

        int end = html.IndexOf("</script", tagEnd, StringComparison.OrdinalIgnoreCase);
        return end < 0 ? null : new Script(start, tagEnd, end);
    }

    /// <summary>
    /// Whether the tag's <c>type</c> is JSON-LD, whatever its case, quotes or <c>; charset</c>; <c>data-type</c> or a
    /// longer tag name such as <c>scripts</c> is not it.
    /// </summary>
    private static bool IsLdJson(string tag)
    {
        // The tag's name must end after "<script": "<scripts" or "<script-x" is another element.
        if (tag.Length <= "<script".Length || !char.IsWhiteSpace(tag["<script".Length])) return false;

        // Tabs and line breaks between attributes count as the space they are.
        string flat = tag.ReplaceLineEndings(" ").Replace('\t', ' ');
        int name = flat.IndexOf(" type", StringComparison.OrdinalIgnoreCase);
        if (name < 0) return false;

        // After the name: optional spaces, "=", optional spaces, then the value.
        string afterName = flat[(name + " type".Length)..].TrimStart();
        if (!afterName.StartsWith('=')) return false;

        string value = afterName[1..].TrimStart();
        string quote = value.Length > 0 && value[0] is '"' or '\'' ? value[..1] : "";
        string text = value[quote.Length..];
        int close = quote.Length > 0
            ? text.IndexOf(quote, StringComparison.Ordinal)
            : text.IndexOfAny([' ', '\t', '\r', '\n']);
        string type = close < 0 ? text : text[..close];
        return type.Split(';')[0].Trim().Equals(LdJson, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>The block's text decoded and parsed, or null when the page left it malformed.</summary>
    private static JsonDocument? Parse(string body)
    {
        try
        {
            return JsonDocument.Parse(WebUtility.HtmlDecode(body));
        }
        catch (JsonException)
        {
            // A script the page left malformed says nothing; the caller still gets the others.
            return null;
        }
    }

    /// <summary>
    /// Where one script element sits in the page: its start, the end of its tag and the end of its body.
    /// </summary>
    private readonly record struct Script(int Start, int TagEnd, int End);
}
