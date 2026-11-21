using LouisAgent.Web.Components.Areas.Themes;
using LouisAgent.Web.Testing.Styles;

namespace LouisAgent.Web.Tests.Rules.Styles.Themes;

/// <summary>
/// F151.1: the helper every theme's tests use to hold the theme to the framework's names. It reads the names the
/// framework sets, and says which of them a theme's block for one style leaves unset.
/// </summary>
public class ThemeSheetTests
{
    /// <summary>The framework's names, as its settings write them, and a rule that only reads one.</summary>
    private const string Framework = """
                                     :root {
                                       --web-ink: #111111;
                                       --web-panel: #ffffff;
                                     }
                                     .web-card {
                                       color: var(--web-ink);
                                     }
                                     :root {
                                       --web-pane-fill: var(--web-panel);
                                     }
                                     """;

    /// <summary>The names the framework sets.</summary>
    private static readonly string[] Names = ["--web-ink", "--web-panel", "--web-pane-fill"];

    /// <summary>A theme with an id of its own, as a package names it.</summary>
    private static readonly SiteTheme Glass = new("glass", "Glass");

    /// <summary>The names come from every plain <c>:root</c> block, each once, not from a rule reading one.</summary>
    [Test]
    public void TheNamesToSet_AreEveryNameTheFrameworksRootBlocksSet() =>
        Assert.That(ThemeSheet.NamesToSet(Framework), Is.EqualTo(Names));

    /// <summary>A theme that sets every name in both styles leaves nothing unset.</summary>
    [Test]
    public void AThemeThatSetsEveryName_LeavesNothingUnset()
    {
        const string theme = """
                             :root[data-theme="glass"][data-style="dark"] {
                               --web-ink: #f5f5f5;
                               --web-panel: #000000;
                               --web-pane-fill: rgba(255, 255, 255, .06);
                             }
                             :root[data-theme="glass"][data-style="light"] {
                               --web-ink: #000000;
                               --web-panel: #ffffff;
                               --web-pane-fill: rgba(255, 255, 255, .7);
                             }
                             """;

        Assert.Multiple(() =>
        {
            Assert.That(ThemeSheet.Missing(theme, Glass, SiteStyle.Dark, Names), Is.Empty);
            Assert.That(ThemeSheet.Missing(theme, Glass, SiteStyle.Light, Names), Is.Empty);
        });
    }

    /// <summary>F151.4: the values a theme's block for one style sets, by name, as the block writes them.</summary>
    [Test]
    public void TheValues_AreWhatTheBlockSets_ByName()
    {
        const string theme = """
                             :root[data-theme=glass][data-style=dark] {
                               --web-ink: #f5f5f5;
                               --web-art-pane-rest: url("../_content/x/pane-rest.svg");
                             }
                             """;

        var values = ThemeSheet.Values(theme, Glass, SiteStyle.Dark);

        Assert.That(values, Is.EqualTo(new Dictionary<string, string>
        {
            ["--web-ink"] = "#f5f5f5",
            ["--web-art-pane-rest"] = "url(\"../_content/x/pane-rest.svg\")"
        }));
    }

    /// <summary>A name left out of the dark block is missing in dark, and only there.</summary>
    [Test]
    public void ANameLeftOutOfOneStyle_IsMissingInThatStyleOnly()
    {
        const string theme = """
                             :root[data-theme="glass"][data-style="dark"] {
                               --web-ink: #f5f5f5;
                               --web-panel: #000000;
                             }
                             :root[data-theme="glass"][data-style="light"] {
                               --web-ink: #000000;
                               --web-panel: #ffffff;
                               --web-pane-fill: rgba(255, 255, 255, .7);
                             }
                             """;

        Assert.Multiple(() =>
        {
            Assert.That(ThemeSheet.Missing(theme, Glass, SiteStyle.Dark, Names),
                Is.EqualTo(new[] { "--web-pane-fill" }));
            Assert.That(ThemeSheet.Missing(theme, Glass, SiteStyle.Light, Names), Is.Empty);
        });
    }

    /// <summary>
    /// F151.2: Sass writes an attribute's value without its quotes, <c>[data-theme=glass]</c>, and the block is still
    /// the theme's.
    /// </summary>
    [Test]
    public void ABlockAsSassWritesIt_WithoutQuotes_IsTheThemes()
    {
        const string theme = """
                             :root[data-theme=glass][data-style=dark] {
                               --web-ink: #f5f5f5;
                               --web-panel: #000000;
                               --web-pane-fill: rgba(255, 255, 255, .06);
                             }
                             """;

        Assert.That(ThemeSheet.Missing(theme, Glass, SiteStyle.Dark, Names), Is.Empty);
    }

    /// <summary>A sheet with no block for the theme leaves every name unset, even with another theme's in it.</summary>
    [Test]
    public void ASheetWithNoBlockForTheTheme_LeavesEveryNameUnset()
    {
        const string other = """
                             :root[data-theme="flat"][data-style="dark"] {
                               --web-ink: #f5f5f5;
                               --web-panel: #000000;
                               --web-pane-fill: #101010;
                             }
                             """;

        Assert.That(ThemeSheet.Missing(other, Glass, SiteStyle.Dark, Names), Is.EqualTo(Names));
    }
}
