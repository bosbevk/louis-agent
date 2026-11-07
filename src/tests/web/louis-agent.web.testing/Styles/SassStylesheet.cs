using System.Text.RegularExpressions;
using LouisAgent.Testing;

namespace LouisAgent.Web.Testing.Styles;

/// <summary>
/// A web app's stylesheet when it's written in Sass: the partials under <c>Styles/</c>, the list in <c>app.scss</c>,
/// and the CSS the build makes from them, which is what the browser gets. A style test reads that CSS, so it holds
/// what the page really loads and not what a partial happens to say.
/// </summary>
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
        string library = partial.StartsWith(AccountsPrefix, StringComparison.Ordinal)
            ? "src/web/louis-agent.web.accounts/Styles"
            : "src/web/louis-agent.web/Styles";
        return Path.Combine(TestPaths.RepoRoot, library, folder, $"_{Path.GetFileName(partial)}.scss");
    }

    /// <summary>The account library's Sass is under <c>accounts/</c> (CL30), as the framework's is under <c>web/</c>.</summary>
    private const string AccountsPrefix = "accounts/";

    /// <summary>The framework's Sass is all under <c>web/</c>, and the account library's under <c>accounts/</c>, so a name says whose it is.</summary>
    private static bool IsFrameworks(string name) =>
        name.StartsWith("web/", StringComparison.Ordinal) || name.StartsWith(AccountsPrefix, StringComparison.Ordinal);

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
}
