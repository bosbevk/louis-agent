using LouisAgent.Web.Components.Areas.Themes;
using LouisAgent.Web.Testing.Styles;
using LouisAgent.Web.Themes.Glass;

namespace LouisAgent.Web.Themes.Tests.Glass;

/// <summary>
/// F151.6: glass is the design page's glass. A pane and a button are their drawing alone, with nothing painted
/// under it, the page behind is fine slanted lines with three lights, and the header is a plain band.
/// </summary>
public class GlassValuesTests
{
    /// <summary>What glass sets a name to, in one style.</summary>
    private static string Value(SiteStyle style, string name) =>
        ThemeSheet.Values(CompiledThemes.Css(), GlassTheme.Theme, style).GetValueOrDefault(name, "");

    /// <summary>Nothing is painted under a drawing: a pane, what is set in, a veil and a button are see-through.</summary>
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

    /// <summary>A pane shows the page through it a little blurred, as the design's glass does.</summary>
    [TestCase(SiteStyle.Dark, "blur(4px) brightness(1.18) saturate(1.4)")]
    [TestCase(SiteStyle.Light, "blur(5px) saturate(1.5)")]
    public void APane_ShowsThePageThroughIt(SiteStyle style, string blur) =>
        Assert.That(Value(style, "--web-pane-blur"), Is.EqualTo(blur));

    /// <summary>The page behind the glass is lines that slant both ways and stay still, under lights.</summary>
    [TestCase(SiteStyle.Dark)]
    [TestCase(SiteStyle.Light)]
    public void ThePageBehind_IsSlantedLinesAndLights(SiteStyle style)
    {
        string lines = Value(style, "--web-page-lines");

        Assert.Multiple(() =>
        {
            Assert.That(lines, Does.Contain("repeating-linear-gradient(45deg"));
            Assert.That(lines, Does.Contain("repeating-linear-gradient(-45deg"));
            Assert.That(lines, Does.Contain("fixed"));
            Assert.That(Value(style, "--web-page-lights"), Does.Contain("radial-gradient("));
        });
    }

    /// <summary>The header is a plain band with a hairline, never glass: no blur behind it.</summary>
    [TestCase(SiteStyle.Dark, "#120e26")]
    [TestCase(SiteStyle.Light, "#f3f0ff")]
    public void TheHeader_IsAPlainBand(SiteStyle style, string fill)
    {
        Assert.Multiple(() =>
        {
            Assert.That(Value(style, "--web-bar-fill"), Is.EqualTo(fill));
            Assert.That(Value(style, "--web-bar-blur"), Is.EqualTo("none"));
        });
    }
}
