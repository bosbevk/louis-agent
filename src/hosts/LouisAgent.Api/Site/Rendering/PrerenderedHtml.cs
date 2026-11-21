using System.Net;

namespace LouisAgent.Api.Site.Rendering;

/// <summary>
/// Takes a prerender's HTML apart (F121): the head's title and description, and the page's body, by the markers
/// <see cref="PrerenderRoot"/> writes. Plain string reading, since the shape is our own (ADR 0057: a regular expression
/// is the last option).
/// </summary>
internal static class PrerenderedHtml
{
    private const string DescriptionTag = "<meta name=\"description\" content=\"";
    private const string SchemaTag = "<script type=\"application/ld+json\">";
    private const string ScriptEnd = "</script>";

    /// <summary>The page, from the root's HTML: its title, its description, its structured data and its body.</summary>
    public static RenderedPage Read(string html)
    {
        int head = html.IndexOf(PrerenderRoot.HeadMark, StringComparison.Ordinal) + PrerenderRoot.HeadMark.Length;
        int body = html.IndexOf(PrerenderRoot.BodyMark, StringComparison.Ordinal);
        string headHtml = html[head..body];
        int bodyEnd = html.LastIndexOf("</div>", StringComparison.Ordinal);
        string bodyHtml = html[(body + PrerenderRoot.BodyMark.Length)..bodyEnd];
        return new RenderedPage(Between(headHtml, "<title>", "</title>"), Between(headHtml, DescriptionTag, "\""),
            bodyHtml.Trim(), Schemas(headHtml));
    }

    /// <summary>Every JSON-LD script in the head, each whole as the page wrote it (F123).</summary>
    private static List<string> Schemas(string headHtml)
    {
        var schemas = new List<string>();
        int at = headHtml.IndexOf(SchemaTag, StringComparison.Ordinal);
        while (at >= 0)
        {
            int end = headHtml.IndexOf(ScriptEnd, at, StringComparison.Ordinal);
            if (end < 0) break;
            schemas.Add(headHtml[at..(end + ScriptEnd.Length)]);
            at = headHtml.IndexOf(SchemaTag, end, StringComparison.Ordinal);
        }

        return schemas;
    }

    /// <summary>The decoded text between two markers, or empty when the first isn't there.</summary>
    private static string Between(string html, string start, string end)
    {
        int from = html.IndexOf(start, StringComparison.Ordinal);
        if (from < 0) return "";
        from += start.Length;
        int to = html.IndexOf(end, from, StringComparison.Ordinal);
        return to < 0 ? "" : WebUtility.HtmlDecode(html[from..to]).Trim();
    }
}
