namespace LouisAgent.Web.Testing.Styles;

/// <summary>The files a stylesheet points at with <c>url(…)</c>, read by hand, not by a regex (ADR 0057).</summary>
internal static class CssUrls
{
    /// <summary>The relative paths the <c>url(…)</c>s in this CSS point at.</summary>
    public static IEnumerable<string> RelativeIn(string css) => PathsIn(css).Where(IsRelative);

    /// <summary>
    /// The path inside each <c>url(…)</c>: a quoted path runs to its own closing quote, so
    /// <c>url("../../brand/tile.svg")</c> gives <c>../../brand/tile.svg</c> and an inline SVG's
    /// <c>url("data:…url(%23n)…")</c> stays one whole path; an unquoted one runs to the closing bracket.
    /// </summary>
    private static IEnumerable<string> PathsIn(string css)
    {
        const string open = "url(";
        int at = css.IndexOf(open, StringComparison.Ordinal);
        while (at >= 0)
        {
            int start = at + open.Length;
            while (start < css.Length && char.IsWhiteSpace(css[start])) start++;

            // A quoted path ends at its own quote, an unquoted one at the bracket.
            char first = start < css.Length ? css[start] : ')';
            bool quoted = first is '"' or '\'';
            int end = quoted ? css.IndexOf(first, start + 1) : css.IndexOf(')', start);
            if (end < 0) yield break;

            yield return quoted ? css[(start + 1)..end] : css[start..end].Trim();
            at = css.IndexOf(open, end + 1, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// Whether a path is found from the stylesheet's own folder: <c>../brand/tile.svg</c> is, <c>data:…</c>,
    /// <c>https://…</c>, <c>/brand/tile.svg</c> and <c>#n</c> aren't.
    /// </summary>
    private static bool IsRelative(string url) =>
        url.Length > 0 && !url.Contains(':') && !url.StartsWith('/') && !url.StartsWith('#');
}
