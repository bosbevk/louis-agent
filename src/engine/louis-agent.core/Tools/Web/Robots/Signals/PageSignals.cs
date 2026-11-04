namespace LouisAgent.Core.Tools.Web.Robots.Signals;

/// <summary>
/// Looks for the "no AI readers" notes a page carries itself: a robots meta tag with <c>noai</c>, an
/// <c>X-Robots-Tag: noai</c> header, a <c>Content-Signal: ai-input=no</c> header or <c>tdm-reservation: 1</c>. It tells
/// you which one it found, or nothing.
/// </summary>
/// <remarks>
/// A plain reader over the headers we already have and the page we already fetched, so it costs no extra request.
/// <c>ai-train=no</c> alone is not a refusal: we read a page to answer a question, we don't train on it, so a site
/// that only says no to training lets us in (ADR 0068). <c>noimageai</c> is about pictures, so it doesn't refuse
/// either.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0068-every-fetch-obeys-robots-txt.md">ADR 0068: every fetch obeys robots.txt</seealso>
public static class PageSignals
{
    /// <summary>
    /// The first signal that says no, looking at the headers and then at the page's meta tag; null when there is none.
    /// </summary>
    public static PageSignal? Read(IReadOnlyDictionary<string, string> headers, string? html) =>
        FromHeaders(headers) ?? FromMeta(html);

    /// <summary>The first of the three header signals that says no.</summary>
    private static PageSignal? FromHeaders(IReadOnlyDictionary<string, string> headers)
    {
        if (Tokens(Header(headers, "X-Robots-Tag")).Contains("noai", StringComparer.OrdinalIgnoreCase))
            return PageSignal.XRobotsTagNoAi;

        if (Tokens(Header(headers, "Content-Signal")).Contains("ai-input=no", StringComparer.OrdinalIgnoreCase))
            return PageSignal.ContentSignalAiInputNo;

        return Header(headers, "tdm-reservation")?.Trim() == "1" ? PageSignal.TdmReservation : null;
    }

    /// <summary>The meta tag signal: a robots tag whose content has noai.</summary>
    private static PageSignal? FromMeta(string? html)
    {
        if (html is null) return null;

        // A page can carry more than one robots tag (an SEO plugin and a theme); any of them saying noai counts.
        bool noAi = MetaTags.ContentsOf(html, "robots")
            .Any(content => Tokens(content).Contains("noai", StringComparer.OrdinalIgnoreCase));
        return noAi ? PageSignal.MetaNoAi : null;
    }

    /// <summary>A header's value by name, whatever the case of the name; null when it isn't there.</summary>
    private static string? Header(IReadOnlyDictionary<string, string> headers, string name) =>
        headers.FirstOrDefault(h => h.Key.Equals(name, StringComparison.OrdinalIgnoreCase)).Value;

    /// <summary>The comma-separated words of a value, trimmed, with spaces around "=" removed.</summary>
    private static IEnumerable<string> Tokens(string? value) =>
        (value ?? "").Split(',').Select(t => t.Replace(" ", "").Trim());
}
