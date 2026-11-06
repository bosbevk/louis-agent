using System.Text.RegularExpressions;
using LouisAgent.Testing;

namespace LouisAgent.Web.Testing.Styles;

/// <summary>
/// A web app's stylesheet when it's written in Sass: the partials under <c>Styles/</c>, the list in <c>app.scss</c>,
/// and the CSS the build makes from them, which is what the browser gets. A style test reads that CSS, so it holds what
/// the page really loads and not what a partial happens to say.
/// </summary>
/// <remarks>
/// It stands beside <see cref="Stylesheet"/> rather than inside it, since the two read different things: that one
/// follows a CSS import list, for the apps that aren't on Sass yet, and it goes when they are.
/// </remarks>
public sealed partial record SassStylesheet(string Project)
{
    /// <summary>Shared Sass helpers that write no CSS of their own, so the list doesn't name them.</summary>
    private const string Tools = "tools/";

    /// <summary>The stylesheet of one of our web apps, by its project's name.</summary>
    public static SassStylesheet Of(string project) =>
        new(Directory.EnumerateDirectories(Path.Combine(TestPaths.RepoRoot, "demos"), project,
            SearchOption.AllDirectories).Single());

    private string Styles => Path.Combine(Project, "Styles");

    private string Compiled => Path.Combine(Project, "wwwroot", "css", "app.css");

    /// <summary>The CSS the build made, all of it, as the browser gets it.</summary>
    public string Read() => File.ReadAllText(Compiled);

    /// <summary>The partials <c>app.scss</c> names, in its order, such as <c>chat/bars</c>.</summary>
    public IReadOnlyList<string> Forwards() =>
        [.. Forward().Matches(File.ReadAllText(Path.Combine(Styles, "app.scss"))).Select(m => m.Groups[1].Value)];

    /// <summary>The partials on disk that write CSS, named the way <c>app.scss</c> names them.</summary>
    public IReadOnlyList<string> AreaFiles() =>
    [
        .. Directory.EnumerateFiles(Styles, "_*.scss", SearchOption.AllDirectories)
            .Select(file => Path.GetRelativePath(Styles, file).Replace('\\', '/'))
            .Select(file => file[..(file.LastIndexOf('/') + 1)] + Path.GetFileNameWithoutExtension(file)[1..])
            .Where(file => !file.StartsWith(Tools, StringComparison.Ordinal))
    ];

    /// <summary>What one partial says, by the name <c>app.scss</c> gives it.</summary>
    public string Source(string partial)
    {
        string folder = Path.GetDirectoryName(partial) ?? "";
        return File.ReadAllText(Path.Combine(Styles, folder, $"_{Path.GetFileName(partial)}.scss"));
    }

    /// <summary>
    /// Every file the CSS points at with <c>url(…)</c>, which the browser finds from where the CSS sits. Web addresses,
    /// data and fragments are left out, since they don't depend on that.
    /// </summary>
    public IReadOnlyList<StylesheetUrl> Urls() =>
    [
        .. new Stylesheet(Path.GetDirectoryName(Compiled)!).PathsOf(Read())
            .Select(url => new StylesheetUrl("app.css", url,
                Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Compiled)!, url))))
    ];

    /// <summary>One line of the list, <c>@forward "chat/bars";</c>, and the name in it.</summary>
    [GeneratedRegex("""@forward "([^"]+)";""")]
    private static partial Regex Forward();
}
