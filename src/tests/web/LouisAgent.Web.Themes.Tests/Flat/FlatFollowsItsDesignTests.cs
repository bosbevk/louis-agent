using LouisAgent.Choices;
using LouisAgent.Web.Components.Areas.Themes;
using LouisAgent.Web.Testing.Styles;
using LouisAgent.Web.Themes.Flat;

namespace LouisAgent.Web.Themes.Tests.Flat;

/// <summary>
/// FX181 to FX183: flat wears its design, the v8 flat mockup (Louis: "look at the design and make sure ours fit the
/// flat design", "all changes should just be theme related"). Every surface one solid colour with a hairline round
/// it, no gradient, no shadow, no blur, and the brand's l on its mark. Read from what the themes project's build
/// compiles.
/// </summary>
public class FlatFollowsItsDesignTests
{
    /// <summary>A value flat sets in one style.</summary>
    private static string Value(SiteStyle style, string name) =>
        ThemeSheet.Values(CompiledThemes.Css(), FlatTheme.Theme, style)[name];

    /// <summary>
    /// FX181: the mark carries the brand's l, drawn with a violet step under it (Louis's pick of three, "the
    /// stepped"), so the letter handed to the orb gets no ink, as in glass.
    /// </summary>
    [TestCase(SiteStyle.Dark)]
    [TestCase(SiteStyle.Light)]
    public void TheMark_CarriesTheBrandsL_AndTheLetterGetsNoInk(SiteStyle style)
    {
        Assert.Multiple(() =>
        {
            Assert.That(Value(style, "--web-art-mark"), Does.Contain($"flat/{style.Wire()}/mark.svg"));
            Assert.That(Value(style, "--web-art-mark-ball"), Does.Contain($"flat/{style.Wire()}/mark-ball.svg"));
            Assert.That(Value(style, "--web-art-mark-o"), Does.Contain($"flat/{style.Wire()}/mark-o.svg"));
            Assert.That(Value(style, "--web-mark-ink"), Is.EqualTo("transparent"));
        });
    }

    /// <summary>
    /// FX181: the orb's disc is glass's ball without its bubble, 37 of 60 across scaled by 1.055, so it is drawn in
    /// the mark and nothing fills under it (Louis: "circle is still too big"); the o of the name is the disc whole.
    /// </summary>
    [TestCase(SiteStyle.Dark)]
    [TestCase(SiteStyle.Light)]
    public void TheOrbsDisc_IsGlasssBall_DrawnWithNothingUnderIt(SiteStyle style)
    {
        string drawings = Path.Combine(TestPaths.RepoRoot, "src/web/LouisAgent.Web.Themes/wwwroot/flat", style.Wire());

        Assert.Multiple(() =>
        {
            Assert.That(Value(style, "--web-mark-fill"), Is.EqualTo("transparent"));
            Assert.That(File.ReadAllText(Path.Combine(drawings, "mark.svg")), Does.Contain("r=\"39\""));
            Assert.That(File.ReadAllText(Path.Combine(drawings, "mark-ball.svg")), Does.Contain("r=\"39\""));
            // Round it nothing: where glass's bubble is, flat's page shows (Louis: "this is what I wanted").
            Assert.That(File.ReadAllText(Path.Combine(drawings, "mark.svg")), Does.Not.Contain("r=\"57\""));
            Assert.That(File.ReadAllText(Path.Combine(drawings, "mark-o.svg")), Does.Contain("r=\"60\""));
        });
    }

    /// <summary>
    /// FX182: the page is one solid colour with no lights, and the box typed in lights with a ring and no glow, since
    /// flat has no shading.
    /// </summary>
    [TestCase(SiteStyle.Dark)]
    [TestCase(SiteStyle.Light)]
    public void ThePage_HasNoLights_AndNothingGlows(SiteStyle style)
    {
        Assert.Multiple(() =>
        {
            Assert.That(Value(style, "--web-page-lights"), Is.EqualTo("none"));
            Assert.That(Value(style, "--web-sunk-lit"), Does.Not.Contain("26px"));
        });
    }

    /// <summary>
    /// FX185: a pane's one line is drawn inside its edge, and under the pointer the same line turns a deeper grey, so
    /// only its colour changes, as the design's card does (Louis: "make sure our flat hover effects is like the
    /// design"). What floats over the page wears the same line.
    /// </summary>
    [TestCase(SiteStyle.Dark, "#2b2b33", "#55555f")]
    [TestCase(SiteStyle.Light, "#a7aaba", "#5d6072")]
    public void APane_UnderThePointer_OnlyChangesItsLinesColour(SiteStyle style, string line, string hoverLine)
    {
        Assert.Multiple(() =>
        {
            Assert.That(Value(style, "--web-pane-edge"), Is.EqualTo("none"));
            Assert.That(Value(style, "--web-pane-shadow"), Is.EqualTo($"inset 0 0 0 1px {line}"));
            Assert.That(Value(style, "--web-float-shadow"), Is.EqualTo($"inset 0 0 0 1px {line}"));
            Assert.That(Value(style, "--web-pane-hover-edge"), Is.EqualTo($"inset 0 0 0 1px {hoverLine}"));
            Assert.That(Value(style, "--web-chip-edge"), Is.EqualTo($"1px solid {line}"));
        });
    }

    /// <summary>
    /// FX185: under the pointer the main button turns the lighter violet, the second fills with the words' colour and
    /// takes the page's for its words, and a chip's line turns the words' colour, as the design's do.
    /// </summary>
    [TestCase(SiteStyle.Dark, "#ffffff", "#0c0c0e")]
    [TestCase(SiteStyle.Light, "#0b0b0e", "#f7f7f8")]
    public void ThePresses_UnderThePointer_AreTheDesigns(SiteStyle style, string words, string page)
    {
        Assert.Multiple(() =>
        {
            Assert.That(Value(style, "--web-press-hover-fill"), Is.EqualTo("#9282ff"));
            Assert.That(Value(style, "--web-quiet-hover-fill"), Is.EqualTo(words));
            Assert.That(Value(style, "--web-quiet-hover-ink"), Is.EqualTo(page));
            Assert.That(Value(style, "--web-chip-hover-edge"), Is.EqualTo($"1px solid {words}"));
        });
    }

    /// <summary>
    /// FX186: a tile's icon stands on the page's own colour, a tad darker than white in the light and the near-black
    /// grey in the dark, darker than its card (Louis: "make white", "darker"; then "flat dark background just a very
    /// dark gray almost black", "same with the white make it a tad darker white"), with no shadow.
    /// </summary>
    [TestCase(SiteStyle.Dark, "#0c0c0e")]
    [TestCase(SiteStyle.Light, "#f7f7f8")]
    public void ATilesIcon_StandsOnThePagesColour(SiteStyle style, string fill)
    {
        Assert.Multiple(() =>
        {
            Assert.That(Value(style, "--web-icon-fill"), Is.EqualTo(fill));
            Assert.That(Value(style, "--web-icon-shadow"), Is.EqualTo("none"));
        });
    }

    /// <summary>FX182: in the dark a chip is the design's second grey, a step lighter than a pane.</summary>
    [Test]
    public void InTheDark_AChip_IsTheSecondGrey() =>
        Assert.That(Value(SiteStyle.Dark, "--web-chip-fill"), Is.EqualTo("#1a1a1f"));

    /// <summary>
    /// FX184: the wordmark's o is the size of glass's ball without its bubble (Louis: "the blue dot must be smaller on
    /// flat cause we do not have another bubble"): glass's ball is 37 of 60 across, scaled by 1.055, in a .76em o.
    /// </summary>
    [TestCase(SiteStyle.Dark)]
    [TestCase(SiteStyle.Light)]
    public void TheWordmarksO_IsGlasssBallWithoutItsBubble(SiteStyle style) =>
        Assert.That(Value(style, "--web-wordmark-o"), Is.EqualTo(".5em"));
}
