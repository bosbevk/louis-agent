using LouisAgent.Testing;

namespace LouisAgent.Puls.Tests.Styles;

/// <summary>
/// Puls's stylesheet is app.css's import list (CL26): every area file is imported once, nothing imported is missing,
/// and the tokens come first so every later rule can use them.
/// </summary>
public class PulsStylesheetTests
{
    /// <summary>Puls's stylesheet.</summary>
    private static readonly Stylesheet Css = Stylesheet.Of("louis-agent.puls");

    /// <summary>Every area file is imported, and every import points at a file that's there.</summary>
    [Test]
    public void EveryAreaFile_IsImported_AndEveryImportIsThere() =>
        Assert.That(Css.Imports().Order(), Is.EqualTo(Css.AreaFiles().Order()));

    /// <summary>The tokens are imported first, then the plain elements: the rest of the files build on them.</summary>
    [Test]
    public void TheTokensComeFirst_ThenThePlainElements() =>
        Assert.That(Css.Imports().Take(2), Is.EqualTo(new[] { "settings/tokens.css", "base/elements.css" }));

    /// <summary>The stylesheet reads as the files in their import order, one after the other.</summary>
    [Test]
    public void TheStylesheet_IsTheFilesInImportOrder()
    {
        var starts = Css.Starts();

        Assert.That(starts, Is.All.GreaterThanOrEqualTo(0), "every file is in it");
        Assert.That(starts, Is.Ordered, "in import order");
    }

    /// <summary>
    /// A trend's change since the run before (<c>▲ +13%</c>) stays one pill on one line (FX31): in a category card's
    /// small panel it broke into two lines inside its round pill, a green blob beside the value. The small panels fill
    /// their card, two or three across, instead of three fixed columns with one left empty.
    /// </summary>
    [Test]
    public void ATrendsChange_StaysOnOneLine_AndTheSmallPanelsFillTheirCard()
    {
        Assert.That(Rule("admin/benchmarks.css", ".trend-delta {"), Does.Contain("white-space: nowrap"));
        Assert.That(Rule("admin/run-page.css", ".trend.compact .trend-grid {"),
            Does.Contain("repeat(auto-fit, minmax("));
    }

    /// <summary>The one rule in an area file that starts with this selector, up to its closing brace.</summary>
    private static string Rule(string file, string selector)
    {
        string text = File.ReadAllText(Path.Combine(TestPaths.RepoRoot, "src", "louis-agent.puls", "wwwroot", "css",
            file));
        int start = text.IndexOf(selector, StringComparison.Ordinal);
        Assert.That(start, Is.GreaterThanOrEqualTo(0), $"{file} has {selector}");
        return text[start..(text.IndexOf('}', start) + 1)];
    }
}
