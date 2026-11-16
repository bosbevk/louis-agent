using LouisAgent.Web.Components.Areas.Themes;
using LouisAgent.Web.Testing.Styles;
using LouisAgent.Web.Themes.Glass;

namespace LouisAgent.Web.Themes.Tests.Glass;

/// <summary>
/// F151.6: glass is the design page's glass. A pane and a button are their drawing alone, with nothing painted
/// under it and nothing blurred behind it, the page behind is a picture of silk ribbons of light, and the header is
/// a plain band by night and white glass by day.
/// </summary>
public class GlassValuesTests
{
    /// <summary>What glass sets a name to, in one style.</summary>
    private static string Value(SiteStyle style, string name) =>
        ThemeSheet.Values(CompiledThemes.Css(), GlassTheme.Theme, style).GetValueOrDefault(name, "");

    /// <summary>
    /// Nothing is painted under a drawing: a pane, what is set in, a veil and a button are see-through.
    /// </summary>
    [TestCase(SiteStyle.Dark, "--web-pane-fill")]
    [TestCase(SiteStyle.Dark, "--web-inset-fill")]
    [TestCase(SiteStyle.Dark, "--web-veil-fill")]
    [TestCase(SiteStyle.Dark, "--web-press-fill")]
    [TestCase(SiteStyle.Light, "--web-pane-fill")]
    [TestCase(SiteStyle.Light, "--web-inset-fill")]
    [TestCase(SiteStyle.Light, "--web-veil-fill")]
    [TestCase(SiteStyle.Light, "--web-press-fill")]
    public void NothingIsPaintedUnderADrawing(SiteStyle style, string name) =>
        Assert.That(Value(style, name), Is.EqualTo("transparent"));

    /// <summary>
    /// Nothing blurs what is behind the glass (Louis, 2026-11-16: "yes keep it 0 blur"): a pane only brightens and
    /// deepens the page under it, and the bar and a chip only deepen it.
    /// </summary>
    [TestCase(SiteStyle.Dark, "--web-pane-blur", "brightness(1.18) saturate(1.4)")]
    [TestCase(SiteStyle.Light, "--web-pane-blur", "saturate(1.5)")]
    [TestCase(SiteStyle.Dark, "--web-dock-blur", "saturate(1.4)")]
    [TestCase(SiteStyle.Light, "--web-dock-blur", "saturate(1.4)")]
    [TestCase(SiteStyle.Dark, "--web-chip-blur", "saturate(1.4)")]
    [TestCase(SiteStyle.Light, "--web-chip-blur", "saturate(1.4)")]
    public void NothingBlursWhatIsBehind(SiteStyle style, string name, string value) =>
        Assert.That(Value(style, name), Is.EqualTo(value));

    /// <summary>
    /// Glass bends what is behind the card under the pointer, or the one picked or shown (Louis's way 2 of three):
    /// its wall at the edge is 52 pixels thick and its middle stands 70 higher, as the design's cards.
    /// </summary>
    [TestCase(SiteStyle.Dark)]
    [TestCase(SiteStyle.Light)]
    public void TheLens_IsTheDesignsCard(SiteStyle style)
    {
        Assert.Multiple(() =>
        {
            Assert.That(Value(style, "--web-lens-wall"), Is.EqualTo("52"));
            Assert.That(Value(style, "--web-lens-rise"), Is.EqualTo("70"));
        });
    }

    /// <summary>
    /// A glass card leans towards the pointer as the design's cards do (Louis: "yes a lens with a tilt"): seven
    /// degrees across its width and its height.
    /// </summary>
    [TestCase(SiteStyle.Dark)]
    [TestCase(SiteStyle.Light)]
    public void TheTilt_IsTheDesignsCard(SiteStyle style) =>
        Assert.That(Value(style, "--web-tilt"), Is.EqualTo("7"));

    /// <summary>
    /// Glass is water only, for every panel (Louis: "can we switch to just water?", "for all"): no frost blurs what is
    /// behind it.
    /// </summary>
    [TestCase(SiteStyle.Dark)]
    [TestCase(SiteStyle.Light)]
    public void TheGlass_HasNoFrost(SiteStyle style) =>
        Assert.That(Value(style, "--web-lens-frost"), Is.EqualTo("0"));

    /// <summary>
    /// Glass ripples what is behind it like water (Louis: "can we replace the frost with a water", "can we increase
    /// the water effect?"): sixteen pixels of ripple.
    /// </summary>
    [TestCase(SiteStyle.Dark)]
    [TestCase(SiteStyle.Light)]
    public void TheWater_RipplesWhatIsBehind(SiteStyle style) =>
        Assert.That(Value(style, "--web-lens-water"), Is.EqualTo("16"));

    /// <summary>
    /// The page behind the glass is a picture of silk ribbons of light, one for each style, laid over the whole
    /// screen (Louis's pick C of three), with no lines of its own.
    /// </summary>
    [TestCase(SiteStyle.Dark, "dark")]
    [TestCase(SiteStyle.Light, "light")]
    public void ThePageBehind_IsTheSilkRibbons(SiteStyle style, string folder)
    {
        Assert.Multiple(() =>
        {
            Assert.That(Value(style, "--web-page-lights"),
                Is.EqualTo(
                    $"url(\"/_content/louis-agent.web.themes/glass/{folder}/silk.webp\") center / cover no-repeat"));
            Assert.That(Value(style, "--web-page-lines"), Is.EqualTo("none"));
        });
    }

    /// <summary>
    /// The header has no blur behind it: a plain band by night, and white glass by day that the page shows through
    /// (FX159, Louis: "make this top part white glass").
    /// </summary>
    [TestCase(SiteStyle.Dark, "#120e26")]
    [TestCase(SiteStyle.Light, "linear-gradient(180deg, rgba(255, 255, 255, .82), rgba(255, 255, 255, .56))")]
    public void TheHeader_HasNoBlur(SiteStyle style, string fill)
    {
        Assert.Multiple(() =>
        {
            Assert.That(Value(style, "--web-bar-fill"), Is.EqualTo(fill));
            Assert.That(Value(style, "--web-bar-blur"), Is.EqualTo("none"));
        });
    }

    /// <summary>
    /// The agent's bar is glass with a body: a see-through violet-dark under its drawing by night and a milky white
    /// by day, and no line round it.
    /// </summary>
    [TestCase(SiteStyle.Dark, "rgba(16, 12, 34, .55)")]
    [TestCase(SiteStyle.Light, "rgba(246, 244, 255, .8)")]
    public void TheAgentsBar_IsGlassWithABody(SiteStyle style, string fill)
    {
        Assert.Multiple(() =>
        {
            Assert.That(Value(style, "--web-dock-fill"), Is.EqualTo(fill));
            Assert.That(Value(style, "--web-dock-edge"), Is.EqualTo("none"));
        });
    }

    /// <summary>Where the words go is sunk into the bar: a shadow under its upper edge, drawn inside it.</summary>
    [TestCase(SiteStyle.Dark)]
    [TestCase(SiteStyle.Light)]
    public void TheBox_IsSunkIntoTheBar(SiteStyle style) =>
        Assert.That(Value(style, "--web-sunk-shadow"), Does.StartWith("inset 0 3px"));

    /// <summary>A chip floats over anything, so it has a milky body under its drawing.</summary>
    [TestCase(SiteStyle.Dark, "rgba(34, 34, 44, .82)")]
    [TestCase(SiteStyle.Light, "rgba(255, 255, 255, .82)")]
    public void AChip_HasAMilkyBody(SiteStyle style, string fill) =>
        Assert.That(Value(style, "--web-chip-fill"), Is.EqualTo(fill));

    /// <summary>
    /// The agent's answer that floats over the page is set apart from it (Louis, 2026-11-16, of the answer over a
    /// card: "just the overlay one need more frosting", then of the final glass page, "just give the overlay this
    /// white", "with glass", "clear"): clear glass with no blur, over the soft body a quiet press has, thick enough
    /// that the page's words under it don't read through.
    /// </summary>
    [TestCase(SiteStyle.Dark, "rgba(22, 20, 32, .9)")]
    [TestCase(SiteStyle.Light, "rgba(204, 203, 218, .9)")]
    public void TheAnswerOverThePage_IsClearGlassOnASoftBody(SiteStyle style, string body)
    {
        Assert.Multiple(() =>
        {
            Assert.That(Value(style, "--web-frost-fill"), Is.EqualTo(body));
            Assert.That(Value(style, "--web-frost-blur"), Is.EqualTo("none"));
        });
    }

    /// <summary>
    /// A card at rest carries a painted still bend at its edge (Louis's final glass page): the light the lens gathers
    /// at the thick rim, drawn once as the pane's light, so a card looks like glass before the pointer reaches it.
    /// </summary>
    [TestCase(SiteStyle.Dark)]
    [TestCase(SiteStyle.Light)]
    public void ACardAtRest_CarriesAStillBend(SiteStyle style)
    {
        Assert.Multiple(() =>
        {
            Assert.That(Value(style, "--web-pane-light"), Does.StartWith("radial-gradient(120% 70% at 0% 0%"));
            Assert.That(Value(style, "--web-pane-shadow"), Does.Contain("inset 14px 16px 18px -14px"));
        });
    }
}
