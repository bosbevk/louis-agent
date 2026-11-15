using LouisAgent.Web.Testing.Styles;

namespace LouisAgent.Web.Tests.Rules.Styles.Themes;

/// <summary>
/// F151.1: the colour names can't say "glass", so the framework names the surfaces too: a pane, a bar, a press, the
/// mark, you and the agent, and five steps of violet. A theme sets each one, so the list is the contract, and this
/// holds the sheet to it name for name. Every site forwards the sheet straight after the colours, so its values sit
/// with the framework's and a site's own come after.
/// </summary>
public class SurfaceNamesTests
{
    /// <summary>The names a theme sets beside the colours, as the F151 plan lists them.</summary>
    private static readonly string[] Surfaces =
    [
        "--web-pane-fill", "--web-pane-edge", "--web-pane-rim", "--web-pane-light", "--web-pane-shadow",
        "--web-pane-blur", "--web-pane-radius",
        "--web-bar-fill", "--web-bar-edge", "--web-bar-blur",
        "--web-press-fill", "--web-press-ink", "--web-press-edge",
        "--web-mark-fill", "--web-mark-gloss", "--web-mark-ink",
        "--web-you", "--web-agent",
        "--web-violet-0", "--web-violet-1", "--web-violet-2", "--web-violet-3", "--web-violet-4"
    ];

    private const string Colours = "web/settings/colours";

    private const string SurfacesSheet = "web/settings/surfaces";

    private static string Sheet => Path.Combine(TestPaths.RepoRoot,
        "src/web/louis-agent.web/Styles/web/settings/_surfaces.scss");

    /// <summary>The sites whose styles are written in Sass, each with its list in <c>app.scss</c>.</summary>
    private static IEnumerable<string> SassSites() =>
        FrameworkWebSites.Folders().Where(site => File.Exists(Path.Combine(site, "Styles", "app.scss")));

    /// <summary>The sheet sets exactly the surface names, no more and none fewer.</summary>
    [Test]
    public void TheSurfacesSheet_SetsEverySurfaceName_AndNoOther()
    {
        Assert.That(File.Exists(Sheet), Is.True, "the surfaces have a sheet of their own");

        Assert.That(ThemeSheet.NamesToSet(File.ReadAllText(Sheet)), Is.EqualTo(Surfaces));
    }

    /// <summary>Every site forwards the surfaces straight after the framework's colours.</summary>
    [Test]
    public void EverySite_ForwardsTheSurfaces_StraightAfterTheColours()
    {
        var sites = SassSites().ToList();
        Assert.That(sites, Has.Count.GreaterThanOrEqualTo(4), "the sites' lists were found");

        var astray = sites
            .Where(site => !SurfacesFollowColours(new SassStylesheet(site).Listed()))
            .Select(FrameworkWebSites.NameOf);

        Assert.That(astray, Is.Empty);
    }

    /// <summary>Whether a list names the surfaces on the line straight after the colours.</summary>
    private static bool SurfacesFollowColours(IReadOnlyList<string> listed)
    {
        var forwards = listed.ToList();
        return forwards.Contains(Colours) && forwards.IndexOf(SurfacesSheet) == forwards.IndexOf(Colours) + 1;
    }
}
