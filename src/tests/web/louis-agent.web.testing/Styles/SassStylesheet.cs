using System.Text.RegularExpressions;
using LouisAgent.Testing;

namespace LouisAgent.Web.Testing.Styles;

/// <summary>
/// A web app's stylesheet when it's written in Sass: the partials under <c>Styles/</c>, the list in <c>app.scss</c>,
/// and the CSS the build makes from them, which is what the browser gets: <c>app.css</c>, and the bundle of every
/// component's own styles (CL58), the site's and the libraries' it references. A style test reads that CSS, so it
/// holds what the page really loads and not what a partial happens to say.
/// </summary>
public sealed partial record SassStylesheet(string Project)
{
    /// <summary>Shared Sass helpers that write no CSS of their own, so the list doesn't name them.</summary>
    private const string Tools = "tools/";

    /// <summary>The stylesheet of one of our web apps, by its project's name: a demo's or the framework's.</summary>
    public static SassStylesheet Of(string project) =>
        new(new[] { "demos", "src/site" }
            .SelectMany(folder => Directory.EnumerateDirectories(Path.Combine(TestPaths.RepoRoot, folder), project,
                SearchOption.AllDirectories))
            .Single());

    private string Styles => Path.Combine(Project, "Styles");

    private string Compiled => Path.Combine(Project, "wwwroot", "css", "app.css");

    /// <summary>
    /// The CSS the build made, all of it, as the browser gets it: <c>app.css</c>, then the components' bundle with
    /// the scope marks taken off (and the double space Blazor leaves where a mark sat before a combinator), so a
    /// test finds a rule by the selector its file wrote.
    /// </summary>
    public string Read() =>
        File.ReadAllText(Compiled) + "\n" + DoubleSpaces().Replace(ScopeMarks().Replace(Bundle(), ""), " ");

    /// <summary>
    /// The site's bundle of scoped styles, which the build leaves under <c>obj/</c>: its own rules, and one
    /// <c>@import</c> per library it references, read here from that library's own bundle.
    /// </summary>
    private string Bundle()
    {
        string? bundle = Directory
            .EnumerateFiles(Path.Combine(Project, "obj"), "*.styles.css", SearchOption.AllDirectories)
            .FirstOrDefault();
        if (bundle is null) return "";
        return BundleImport().Replace(File.ReadAllText(bundle), import => LibraryBundle(import.Groups[1].Value));
    }

    /// <summary>
    /// A referenced library's bundle of scoped styles, by the library's name, from its own <c>obj/</c>.
    /// </summary>
    private static string LibraryBundle(string library)
    {
        var folders = new[] { "src/web", "packs/web" }
            .Select(area => Path.Combine(TestPaths.RepoRoot, area, library, "obj"))
            .Where(Directory.Exists);
        string? bundle = folders
            .SelectMany(obj => Directory.EnumerateFiles(obj, "*.bundle.scp.css", SearchOption.AllDirectories))
            .FirstOrDefault();
        return bundle is null ? "" : File.ReadAllText(bundle);
    }

    /// <summary>The site's own partials on <c>app.scss</c>'s list, in its order, such as <c>chat/bars</c>.</summary>
    public IReadOnlyList<string> Forwards() =>
        [.. Listed().Where(name => !IsFrameworks(name) && !name.StartsWith("pack-", StringComparison.Ordinal))];

    /// <summary>The framework's partials on that list, in its order, such as <c>web/settings/colours</c>.</summary>
    public IReadOnlyList<string> FrameworkForwards() => [.. Listed().Where(IsFrameworks)];

    /// <summary>Everything on the list, in its order.</summary>
    private IEnumerable<string> Listed() =>
        Forward().Matches(File.ReadAllText(Path.Combine(Styles, "app.scss"))).Select(m => m.Groups[1].Value);

    /// <summary>Where one of the framework's partials sits, by the name a site's list gives it.</summary>
    public string FrameworkPartial(string partial)
    {
        string folder = Path.GetDirectoryName(partial) ?? "";
        string library = partial.StartsWith(EvalsPrefix, StringComparison.Ordinal)
            ? "src/web/louis-agent.web.evals/Styles"
            : "src/web/louis-agent.web/Styles";
        return Path.Combine(TestPaths.RepoRoot, library, folder, $"_{Path.GetFileName(partial)}.scss");
    }

    /// <summary>The studio's shared Sass is under <c>evals/</c> (CL57).</summary>
    private const string EvalsPrefix = "evals/";

    /// <summary>
    /// The framework's Sass is all under <c>web/</c> and the studio's shared Sass under <c>evals/</c>, so a name
    /// says whose it is; the account library has no partial since CL58, its components carry their own styles.
    /// </summary>
    private static bool IsFrameworks(string name) =>
        name.StartsWith("web/", StringComparison.Ordinal) ||
        name.StartsWith(EvalsPrefix, StringComparison.Ordinal);

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
    /// Every file the CSS points at with <c>url(…)</c>, which the browser finds from where the CSS sits. Web
    /// addresses, data and fragments are left out, since they don't depend on that.
    /// </summary>
    public IReadOnlyList<StylesheetUrl> Urls() =>
    [
        .. CssUrls.RelativeIn(Read())
            .Select(url => new StylesheetUrl("app.css", url,
                Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Compiled)!, url))))
    ];

    /// <summary>One line of the list, <c>@forward "chat/bars";</c>, and the name in it.</summary>
    [GeneratedRegex("""@forward "([^"]+)";""")]
    private static partial Regex Forward();

    /// <summary>
    /// One import in the site's bundle, and the library's name in it:
    /// <c>@import '_content/louis-agent.web/louis-agent.web.k3x9z2m1qa.bundle.scp.css';</c>.
    /// </summary>
    [GeneratedRegex("""@import '_content/([^/']+)/[^']+';""")]
    private static partial Regex BundleImport();

    /// <summary>
    /// Blazor's scope mark on a bundled selector: <c>[b-k3x9z2m1qa]</c>; a plain <c>[readonly]</c> is not one.
    /// </summary>
    [GeneratedRegex(@"\[b-[a-z0-9]{10}\]")]
    private static partial Regex ScopeMarks();

    /// <summary>Two or more spaces in a row, as in <c>.admin-main &gt;  *</c>; a single one is not matched.</summary>
    [GeneratedRegex(@"  +")]
    private static partial Regex DoubleSpaces();
}
