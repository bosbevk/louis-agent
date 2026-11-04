using System.Text.RegularExpressions;
using LouisAgent.Testing;

namespace LouisAgent.Web.Testing.Styles;

/// <summary>
/// A web app's stylesheet as the browser sees it: the files its app.css imports, read in their order and joined.
/// Both web apps' tests use it, so a broken import fails in either.
/// </summary>
/// <remarks>
/// Extract Function (Fowler), chosen in CL26: the style tests each read app.css with their own copy of the same line;
/// since app.css became a list of imports, one reader follows it, so a rule can move between area files without
/// breaking a test, and an import that points nowhere fails them all. It lives here, beside the other things the web
/// apps' tests share, rather than as a copy in each.
/// </remarks>
public sealed partial record Stylesheet(string Folder)
{
    /// <summary>The stylesheet of one of our web apps, by its project's name.</summary>
    public static Stylesheet Of(string project) =>
        new(Path.Combine(TestPaths.RepoRoot, "src/web", project, "wwwroot", "css"));

    /// <summary>Every rule the app loads, file after file in app.css's order.</summary>
    public string Read() => string.Join("\n", Imports().Select(Text));

    /// <summary>The files app.css imports, in its order, as paths under the css folder.</summary>
    public IReadOnlyList<string> Imports() =>
        [.. Import().Matches(File.ReadAllText(Path.Combine(Folder, "app.css"))).Select(m => m.Groups[1].Value)];

    /// <summary>
    /// The area files on disk: every stylesheet under the css folder but app.css itself and the themes, which the
    /// page loads on its own.
    /// </summary>
    public IReadOnlyList<string> AreaFiles() =>
    [
        .. Directory.EnumerateFiles(Folder, "*.css", SearchOption.AllDirectories)
            .Select(file => Path.GetRelativePath(Folder, file).Replace('\\', '/'))
            .Where(file => file != "app.css" && !file.StartsWith("themes/", StringComparison.Ordinal))
    ];

    /// <summary>Where each imported file's text starts in the joined stylesheet, in import order.</summary>
    public IReadOnlyList<int> Starts()
    {
        string css = Read();
        return [.. Imports().Select(file => css.IndexOf(Text(file), StringComparison.Ordinal))];
    }

    /// <summary>One area file's text.</summary>
    private string Text(string file) => File.ReadAllText(Path.Combine(Folder, file));

    /// <summary>
    /// Every file path a rule points at with <c>url(…)</c> that the browser resolves from the stylesheet's own folder
    /// (FX30: a rule moved one folder deeper kept its old path). Web addresses, data, absolute paths and fragments are
    /// left out: they don't depend on where the file sits.
    /// </summary>
    public IReadOnlyList<StylesheetUrl> Urls() =>
    [
        .. AreaFiles().SelectMany(file => PathsIn(Text(file))
            .Where(IsRelative)
            .Select(url => new StylesheetUrl(file, url,
                Path.GetFullPath(Path.Combine(Folder, Path.GetDirectoryName(file) ?? "", url)))))
    ];

    /// <summary>
    /// The path inside each <c>url(…)</c> of one file, read by hand rather than by a regular expression (ADR 0057): a
    /// quoted path runs to its own closing quote, so <c>url("../../brand/tile.svg")</c> gives
    /// <c>../../brand/tile.svg</c> and an inline SVG's <c>url("data:…url(%23n)…")</c> stays one whole path; an
    /// unquoted one runs to the closing bracket.
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

    /// <summary>One import line, <c>@import url("chat/bars.css");</c>, and the path in it.</summary>
    [GeneratedRegex("""@import url\("([^"]+)"\);""")]
    private static partial Regex Import();
}
