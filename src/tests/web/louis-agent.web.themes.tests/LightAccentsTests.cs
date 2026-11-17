using System.Globalization;
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
    /// <summary>The least contrast for words, as the web's guidelines ask of ordinary text.</summary>
    private const double ForWords = 4.5;

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
        string value = ThemeSheet.Values(CompiledThemes.Css(), picked, SiteStyle.Light).GetValueOrDefault(name, "");

        Assert.That(ContrastWithWhite(value), Is.GreaterThanOrEqualTo(ForWords), $"{name} is {value}");
    }

    /// <summary>How far a colour such as <c>#2563c9</c> stands from white, by the web's contrast sum.</summary>
    private static double ContrastWithWhite(string hex)
    {
        if (hex.Length != 7 || hex[0] != '#') return 0;
        double[] channels = [.. Enumerable.Range(0, 3)
            .Select(at => int.Parse(hex.AsSpan(1 + at * 2, 2), NumberStyles.HexNumber) / 255.0)
            .Select(c => c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4))];
        double light = 0.2126 * channels[0] + 0.7152 * channels[1] + 0.0722 * channels[2];
        return 1.05 / (light + 0.05);
    }
}
