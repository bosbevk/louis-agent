using LouisAgent.Api.Site.Rendering;

namespace LouisAgent.Api.Site;

/// <summary>
/// Puts a rendered page into the site's own index.html (F121): its title, its description, its link card and its
/// canonical address in the head, its structured data and a breadcrumb (F123), and its words inside the app's element,
/// hidden behind the loading screen (F128.1). The app replaces both when it starts, so a visitor sees the loader, never
/// a bare page, and a crawler that runs no script reads the words.
/// </summary>
/// <remarks>
/// Plain string reading over the site's own file, which the framework's sites share the shape of (ADR 0057: a regular
/// expression is the last option). A tag the file doesn't have is left out, never added in the wrong place, except the
/// canonical link, which goes before the head closes.
/// </remarks>
public static class PageShell
{
    private const string App = "<div id=\"app\">";
    private const string SiteNameTag = "<meta property=\"og:site_name\" content=\"";

    /// <summary>
    /// The site's index.html with the page in it, at the address asked for on the site's public URL; the route is the
    /// page's own, which its breadcrumb names.
    /// </summary>
    public static string Fill(string indexHtml, RenderedPage page, Uri publicUrl, string address, string route)
    {
        indexHtml = WithSchemas(indexHtml, page, publicUrl, route);
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
    /// The page's structured data before the head closes (F123), and, for a page under the landing, a breadcrumb of the
    /// site's name, then the page's title without the site's, at the page's own address.
    /// </summary>
    private static string WithSchemas(string html, RenderedPage page, Uri publicUrl, string route)
    {
        var scripts = new List<string>(page.Schemas ?? []);
        string siteName = Between(html, SiteNameTag, "\"");
        if (route != "/" && siteName.Length > 0 && page.Title.Length > 0)
        {
            string pageName = page.Title.Replace($" · {siteName}", "", StringComparison.Ordinal).Trim();
            scripts.Add(PageBreadcrumb.Of(siteName, publicUrl.ToString(), pageName,
                new Uri(publicUrl, route).ToString()).Script());
        }

        if (scripts.Count == 0) return html;
        return html.Replace("</head>", string.Concat(scripts.Select(script => $"    {script}\n")) + "</head>",
            StringComparison.Ordinal);
    }

    /// <summary>The text between a start and an end, or empty when the start isn't there.</summary>
    private static string Between(string html, string start, string end)
    {
        int from = html.IndexOf(start, StringComparison.Ordinal);
        if (from < 0) return "";
        from += start.Length;
        int to = html.IndexOf(end, from, StringComparison.Ordinal);
        return to < 0 ? "" : html[from..to];
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
    /// The page's words inside the app element, hidden after its loading screen (F128.1): a visitor sees the loader
    /// until the app starts and replaces both, and a crawler reads the words in the HTML. The element's closing tag is
    /// found by counting the elements opened and closed inside it, so everything after the element stays.
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
        return html[..close] + "\n<div class=\"prerendered\" hidden>\n" + body + "\n</div>\n" + html[close..];
    }
}
