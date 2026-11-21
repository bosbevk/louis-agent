using LouisAgent.Web.Components.Areas.Themes;
using LouisAgent.Web.Testing.Styles;
using LouisAgent.Web.Themes.Flat;
using LouisAgent.Web.Themes.Glass;

namespace LouisAgent.Web.Themes.Tests;

/// <summary>
/// FX193: in a light look a demo's colour reads on the page. Each accent carries white words on a demo's button and
/// is the colour of its icon on the dock, so it is deep enough for white words on it and to stand out on white.
/// The accents the dark looks share were near invisible on glass's violet page (Louis: "I cannot see the colors on
/// that ribbon").
/// </summary>
public class LightAccentsTests
{
    /// <summary>White, the words on a demo's button and the ground its icon must stand out on.</summary>
    private const string White = "#ffffff";

    /// <summary>An accent, in a theme's light look, holds white words and stands out on white.</summary>
    [TestCase("glass", "--bs-info")]
    [TestCase("glass", "--web-ok")]
    [TestCase("glass", "--bs-warning")]
    [TestCase("glass", "--bs-danger")]
    [TestCase("glass", "--web-purple")]
    [TestCase("flat", "--bs-info")]
    [TestCase("flat", "--web-ok")]
    [TestCase("flat", "--bs-warning")]
    [TestCase("flat", "--bs-danger")]
    [TestCase("flat", "--web-purple")]
    public void AnAccent_HoldsWhiteWords_InTheLight(string theme, string name)
    {
        var picked = theme == "glass" ? GlassTheme.Theme : FlatTheme.Theme;
        string value = ThemeSheet.Values(CompiledThemes.Css(), picked, SiteStyle.Light)[name];

        Assert.That(Contrast.Between(value, White), Is.GreaterThanOrEqualTo(Contrast.ForWords), $"{name} is {value}");
    }
}
