using LouisAgent.Web.Components.Areas.Themes;
using LouisAgent.Web.Testing.Styles;
using LouisAgent.Web.Themes.Flat;

namespace LouisAgent.Web.Themes.Tests.Flat;

/// <summary>
/// F151.2: the flat theme sets every name the framework sets, in each style, so no framework value shows through
/// when a page wears flat. Read from what the themes project's build compiles (Louis's pick A of three).
/// </summary>
public class FlatSheetTests
{
    /// <summary>In each style, flat leaves none of the framework's names unset.</summary>
    [TestCase(SiteStyle.Dark)]
    [TestCase(SiteStyle.Light)]
    public void EachStyle_SetsEveryNameTheFrameworkDoes(SiteStyle style)
    {
        string css = CompiledThemes.Css();
        var names = ThemeSheet.NamesToSet(css);
        Assert.That(names, Has.Count.GreaterThan(50), "the framework's names were read");

        Assert.That(ThemeSheet.Missing(css, FlatTheme.Theme, style, names), Is.Empty);
    }
}
