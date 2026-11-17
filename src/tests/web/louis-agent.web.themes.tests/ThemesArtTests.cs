using LouisAgent.Choices;
using LouisAgent.Web.Components.Areas.Themes;
using LouisAgent.Web.Testing.Styles;
using LouisAgent.Web.Themes.Flat;
using LouisAgent.Web.Themes.Glass;

namespace LouisAgent.Web.Themes.Tests;

/// <summary>
/// F151.4, Louis's pick A of three: a theme hands a piece its drawing as a value. Glass names, for each part and
/// state, a drawing the themes project serves, and flat names none but its mark's (FX181), since flat draws no glass.
/// </summary>
public class ThemesArtTests
{
    /// <summary>
    /// Where a page finds the themes project's files, written from the site's root: a drawing is a value, and a
    /// browser reads its address from the sheet of the rule that uses it, which for a component is not the site's.
    /// </summary>
    private const string Served = "/_content/louis-agent.web.themes/";

    /// <summary>
    /// The version flat's mark drawings are at, in their addresses: a browser keeps a drawing by its address, so a
    /// drawing that changes takes a new one, and every browser fetches it again (FX181, Louis seeing the old mark).
    /// </summary>
    private const int FlatMarks = 3;

    /// <summary>The themes project's served files on disk.</summary>
    private static readonly string Root = Path.Combine(TestPaths.RepoRoot, "src/web/louis-agent.web.themes/wwwroot");

    /// <summary>The art names a theme's block sets, with their values.</summary>
    private static Dictionary<string, string> Art(SiteTheme theme, SiteStyle style) =>
        ThemeSheet.Values(CompiledThemes.Css(), theme, style)
            .Where(value => value.Key.StartsWith("--web-art-", StringComparison.Ordinal))
            .ToDictionary(value => value.Key, value => value.Value);

    /// <summary>
    /// Each of glass's art names, in each style, is the drawing of that part and state, served by the project and
    /// there on disk.
    /// </summary>
    [TestCase(SiteStyle.Dark)]
    [TestCase(SiteStyle.Light)]
    public void GlassNamesADrawing_ForEveryPartAndState_ThatIsServed(SiteStyle style)
    {
        string word = style.Wire();
        var art = Art(GlassTheme.Theme, style);
        Assert.That(art, Has.Count.GreaterThan(30), "glass's art names were read");

        var wrong = art.Where(name =>
        {
            string drawing = name.Key["--web-art-".Length..];
            string expected = $"url(\"{Served}glass/{word}/{drawing}.svg\")";
            return name.Value != expected || !File.Exists(Path.Combine(Root, "glass", word, $"{drawing}.svg"));
        }).Select(name => $"{name.Key}: {name.Value}");

        Assert.That(wrong, Is.Empty);
    }

    /// <summary>
    /// Flat lays no drawing on any piece but its mark (FX181): the mark and the bar's ball carry the brand's l on its disc, the name's o is the disc, each
    /// served by the project and there on disk, and every other name is none.
    /// </summary>
    [TestCase(SiteStyle.Dark)]
    [TestCase(SiteStyle.Light)]
    public void FlatNamesNoDrawing_ButItsMark(SiteStyle style)
    {
        string word = style.Wire();
        var art = Art(FlatTheme.Theme, style);
        string[] marks = ["mark", "mark-ball", "mark-o"];
        Assert.That(art, Has.Count.GreaterThan(30), "flat's art names were read");

        var drawn = art.Where(name => name.Value != "none").Select(name => name.Key["--web-art-".Length..]);
        var missing = marks.Where(drawing =>
            art[$"--web-art-{drawing}"] != $"url(\"{Served}flat/{word}/{drawing}.svg?v={FlatMarks}\")"
            || !File.Exists(Path.Combine(Root, "flat", word, $"{drawing}.svg")));

        Assert.Multiple(() =>
        {
            Assert.That(drawn, Is.EquivalentTo(marks));
            Assert.That(missing, Is.Empty);
        });
    }
}
