using LouisAgent.Api.Site.Rendering;

namespace LouisAgent.Api.Site;

/// <summary>
/// Puts a rendered page into the site's own index.html (F121): its title, its description, its link card and its
/// canonical address in the head, and its words inside the app's element in place of the loading screen. The app
/// replaces those words when it starts, so a visitor sees the page at once and a crawler that runs no script reads it.
/// </summary>
/// <remarks>
/// Plain string reading over the site's own file, which the framework's sites share the shape of (ADR 0057: a regular
/// expression is the last option). A tag the file doesn't have is left out, never added in the wrong place, except the
/// canonical link, which goes before the head closes.
/// </remarks>
public static class PageShell
{
    private const string App = "<div id=\"app\">";

    /// <summary>The site's index.html with the page in it, at the address on the site's public URL.</summary>
    public static string Fill(string indexHtml, RenderedPage page, Uri publicUrl, string address)
    {
        string canonical = Escaped(new Uri(publicUrl, address).ToString());
        string title = Escaped(page.Title);
        string description = Escaped(page.Description);

        string html = indexHtml;
        if (title.Length > 0)
        {
            html = Replace(html, "<title>", "</title>", title);
            html = Replace(html, "<meta property=\"og:title\" content=\"", "\"", title);
        }

        if (description.Length > 0)
        {
            html = Replace(html, "<meta name=\"description\" content=\"", "\"", description);
            html = Replace(html, "<meta property=\"og:description\" content=\"", "\"", description);
        }

        html = Replace(html, "<meta property=\"og:url\" content=\"", "\"", canonical);
        html = html.Replace("</head>", $"    <link rel=\"canonical\" href=\"{canonical}\" />\n</head>",
            StringComparison.Ordinal);
        return InApp(html, page.Body);
    }

    /// <summary>
    /// Text made safe inside an element or an attribute: only the four characters that matter, so a page's own
    /// letters (an Afrikaans word, a middle dot) stay as they are.
    /// </summary>
    private static string Escaped(string text) => text.Replace("&", "&amp;", StringComparison.Ordinal)
        .Replace("<", "&lt;", StringComparison.Ordinal).Replace(">", "&gt;", StringComparison.Ordinal)
        .Replace("\"", "&quot;", StringComparison.Ordinal);

    /// <summary>
    /// The text between a start and an end replaced, the first time the start appears; as it was without it.
    /// </summary>
    private static string Replace(string html, string start, string end, string value)
    {
        int from = html.IndexOf(start, StringComparison.Ordinal);
        if (from < 0) return html;
        from += start.Length;
        int to = html.IndexOf(end, from, StringComparison.Ordinal);
        return to < 0 ? html : html[..from] + value + html[to..];
    }

    /// <summary>
    /// The page's words as the app element's whole content: its closing tag found by counting the elements opened and
    /// closed inside it, so the loading screen goes and everything after the element stays.
    /// </summary>
    private static string InApp(string html, string body)
    {
        int open = html.IndexOf(App, StringComparison.Ordinal);
        if (open < 0) return html;
        int inside = open + App.Length;
        int depth = 1;
        int at = inside;
        while (depth > 0)
        {
            int nextOpen = html.IndexOf("<div", at, StringComparison.Ordinal);
            int nextClose = html.IndexOf("</div>", at, StringComparison.Ordinal);
            if (nextClose < 0) return html;
            bool opens = nextOpen >= 0 && nextOpen < nextClose;
            depth += opens ? 1 : -1;
            at = (opens ? nextOpen : nextClose) + 4;
        }

        int close = at - 4;
        return html[..inside] + "\n" + body + "\n" + html[close..];
    }
}
