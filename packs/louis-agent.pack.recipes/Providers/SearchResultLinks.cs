using System.Net;
using System.Text;
using LouisAgent.Packs.Recipes.Sources;

namespace LouisAgent.Packs.Recipes.Providers;

/// <summary>
/// Finds the results on a site's search page: every <c>h2</c> or <c>h3</c> heading that holds a link to the same
/// site is one result, in the order the page lists them. A result is the link's words and its address.
/// </summary>
/// <remarks>
/// A small reader before a regex (ADR 0057): a plain walk with <c>IndexOf</c>. Themes name their result headings
/// differently (<c>entry-title</c>, <c>post-title</c>, <c>is-title</c>), but nearly all use a heading with a link in
/// it, so the rule looks at the heading and not at the class. The price is that a heading that links to a page that
/// is not a recipe (a travel post, a category) is a hit too; the cook reads the title and sees that. A heading with no
/// link, a link to another site (a share button, an advert) and a link with no words are not hits.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0057-regex-is-the-last-option.md">ADR 0057: regex is the last option</seealso>
internal static class SearchResultLinks
{
    private static readonly string[] Tags = ["h2", "h3"];

    /// <summary>The hits on the page, no address twice, at most <paramref name="maxResults"/>.</summary>
    public static IReadOnlyList<RecipeHit> In(string html, Uri home, int maxResults = int.MaxValue)
    {
        var hits = new List<RecipeHit>();
        int at = 0;
        while (hits.Count < maxResults && NextHeading(html, at) is { } heading)
        {
            at = heading.End;
            RecipeHit? hit = HitIn(heading.Inner, home);
            if (hit is not null && hits.All(h => h.Url != hit.Url)) hits.Add(hit);
        }

        return hits;
    }

    /// <summary>A heading's insides and the place just after it.</summary>
    private readonly record struct Heading(string Inner, int End);

    /// <summary>The first heading at or after <paramref name="from"/>, or null when there is none left.</summary>
    private static Heading? NextHeading(string html, int from)
    {
        // Of the two heading kinds, the one that opens first; the other is found again on a later turn.
        var starts = Tags.Select(tag => (Tag: tag, At: OpenTag(html, tag, from))).Where(s => s.At >= 0).ToList();
        if (starts.Count == 0) return null;

        var (tag, start) = starts.MinBy(s => s.At);
        int innerStart = html.IndexOf('>', start) + 1;
        int close = innerStart == 0 ? -1 : html.IndexOf($"</{tag}>", innerStart, StringComparison.OrdinalIgnoreCase);
        // A heading that never closes ends the walk: there is nothing sound left to read.
        return close < 0 ? null : new Heading(html[innerStart..close], close + tag.Length + 3);
    }

    /// <summary>Where <c>&lt;h2</c> (as a whole tag name, not <c>&lt;h20</c>) next opens, or -1.</summary>
    private static int OpenTag(string html, string tag, int from)
    {
        for (int at = html.IndexOf($"<{tag}", from, StringComparison.OrdinalIgnoreCase);
             at >= 0;
             at = html.IndexOf($"<{tag}", at + 1, StringComparison.OrdinalIgnoreCase))
        {
            int after = at + tag.Length + 1;
            if (after < html.Length && (html[after] == '>' || char.IsWhiteSpace(html[after]))) return at;
        }

        return -1;
    }

    /// <summary>The heading's first link as a hit, or null when it has none, leaves the site or has no words.</summary>
    private static RecipeHit? HitIn(string inner, Uri home)
    {
        int open = AnchorStart(inner);
        int tagEnd = open < 0 ? -1 : inner.IndexOf('>', open);
        if (tagEnd < 0) return null;

        Uri? url = Address(AttributeValue(inner[open..tagEnd], "href"), home);
        if (url is null || !SameSite(url, home)) return null;

        int close = inner.IndexOf("</a>", tagEnd, StringComparison.OrdinalIgnoreCase);
        string name = Words(inner[(tagEnd + 1)..(close < 0 ? inner.Length : close)]);
        return name.Length == 0 ? null : new RecipeHit(name, url);
    }

    /// <summary>Where the heading's first <c>&lt;a</c> opens, or -1; <c>&lt;abbr</c> is not a link.</summary>
    private static int AnchorStart(string inner)
    {
        for (int at = inner.IndexOf("<a", StringComparison.OrdinalIgnoreCase);
             at >= 0;
             at = inner.IndexOf("<a", at + 1, StringComparison.OrdinalIgnoreCase))
            if (at + 2 < inner.Length && char.IsWhiteSpace(inner[at + 2]))
                return at;

        return -1;
    }

    /// <summary>The value of an attribute inside a tag's text, quoted or not, or null when it isn't there.</summary>
    private static string? AttributeValue(string tag, string name)
    {
        // The name must follow whitespace, so data-href is not href.
        int at = tag.IndexOf($"{name}=", StringComparison.OrdinalIgnoreCase);
        while (at > 0 && !char.IsWhiteSpace(tag[at - 1]))
            at = tag.IndexOf($"{name}=", at + 1, StringComparison.OrdinalIgnoreCase);

        int valueAt = at + name.Length + 1;
        if (at < 0 || valueAt >= tag.Length) return null;

        char quote = tag[valueAt];
        // An unquoted value runs to the next whitespace, or to the end of the tag's text.
        if (quote is not ('"' or '\''))
            return WebUtility.HtmlDecode(tag[valueAt..].Split((char[]?)null)[0]);

        int end = tag.IndexOf(quote, valueAt + 1);
        return end < 0 ? null : WebUtility.HtmlDecode(tag[(valueAt + 1)..end]);
    }

    /// <summary>The link as an absolute web address (a relative one is read from the home), or null.</summary>
    private static Uri? Address(string? href, Uri home) =>
        href is not null && Uri.TryCreate(home, href, out Uri? url) && url.Scheme is "http" or "https" ? url : null;

    /// <summary>The same site is the same host, a leading <c>www.</c> not counted.</summary>
    private static bool SameSite(Uri url, Uri home) =>
        string.Equals(Host(url), Host(home), StringComparison.OrdinalIgnoreCase);

    /// <summary>The address's host without a leading <c>www.</c>.</summary>
    public static string Host(Uri url) => url.Host.StartsWith("www.", StringComparison.OrdinalIgnoreCase)
        ? url.Host[4..]
        : url.Host;

    /// <summary>The text of some HTML: tags taken out, entities decoded, runs of whitespace made one space.</summary>
    private static string Words(string html)
    {
        var text = new StringBuilder();
        bool inTag = false;
        foreach (char c in html)
        {
            if (c == '<') inTag = true;
            else if (c == '>' && inTag) inTag = false;
            else if (!inTag) text.Append(c);
        }

        return string.Join(' ', WebUtility.HtmlDecode(text.ToString())
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }
}
