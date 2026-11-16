using LouisAgent.Diagrams.Engine.Svg.Theme;
using LouisAgent.Diagrams.Tones;

namespace LouisAgent.Diagrams.Tests.Svg.Theme;

/// <summary>
/// F151.7: the pictures' violet is the brand's, Ultraviolet (Louis's pick for both themes, "and the diagrams' violet
/// follows"). On a dark screen a deep tint, the second step for the line and the first for the words; on a light one
/// a pale tint of the first step, the brand's own violet for the line and the fourth step for the words.
/// </summary>
public class SvgThemeTests
{
    /// <summary>The violet a dark screen gets.</summary>
    [Test]
    public void TheVioletOnADarkScreen_IsUltraviolet() =>
        Assert.That(SvgTheme.Site.Dark.Tones[DiagramTone.Violet],
            Is.EqualTo(new SvgTone("#110a40", "#9282ff", "#e2dfff")));

    /// <summary>The violet a light screen gets.</summary>
    [Test]
    public void TheVioletOnALightScreen_IsUltraviolet() =>
        Assert.That(SvgTheme.Site.Light.Tones[DiagramTone.Violet],
            Is.EqualTo(new SvgTone("#e2dfff", "#6b4dff", "#3a22c4")));
}
