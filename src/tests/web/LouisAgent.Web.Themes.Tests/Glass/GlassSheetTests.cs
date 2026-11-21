using LouisAgent.Web.Components.Areas.Themes;
using LouisAgent.Web.Testing.Styles;
using LouisAgent.Web.Themes.Glass;

namespace LouisAgent.Web.Themes.Tests.Glass;

/// <summary>
/// F151.2: the glass theme sets every name the framework sets, so no framework value shows through when a page wears
/// glass. Read from what the themes project's build compiles: the framework's names first, then the theme's blocks
/// (Louis's pick A of three).
/// </summary>
public class GlassSheetTests
{
    /// <summary>In each style, glass leaves none of the framework's names unset.</summary>
    [TestCase(SiteStyle.Dark)]
    [TestCase(SiteStyle.Light)]
    public void EachStyle_SetsEveryNameTheFrameworkDoes(SiteStyle style)
    {
        string css = CompiledThemes.Css();
        var names = ThemeSheet.NamesToSet(css);
        Assert.That(names, Has.Count.GreaterThan(50), "the framework's names were read");

        Assert.That(ThemeSheet.Missing(css, GlassTheme.Theme, style, names), Is.Empty);
    }
}
