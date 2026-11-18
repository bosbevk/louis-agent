using LouisAgent.Diagrams.Engine.Svg.Theme;
using LouisAgent.Web.Components.Areas.Themes;
using LouisAgent.Web.Themes.Flat;
using LouisAgent.Web.Themes.Glass;

namespace LouisAgent.Web.Themes.Tests;

/// <summary>
/// F161.1: a picture on a page wears the page's theme as well as its style (Louis's pick C: "a palette per theme in
/// the engine"). The engine can't depend on the themes for their names, so it names the attribute and the two themes
/// itself, and this test, which sees both, holds them equal to the ids the themes give themselves.
/// </summary>
public class PicturesWearTheThemesTests
{
    /// <summary>The engine asks for the theme's attribute the page's <c>&lt;html&gt;</c> carries.</summary>
    [Test]
    public void ThePicturesRule_AsksForThePagesThemeAttribute() =>
        Assert.That(PageScheme.ThemeAttribute, Is.EqualTo(SiteLook.ThemeAttribute));

    /// <summary>The engine's two themes are the ids the themes give themselves.</summary>
    [Test]
    public void ThePicturesThemes_AreTheThemesIds() => Assert.Multiple(() =>
    {
        Assert.That(FlatTheme.Theme.Id, Is.EqualTo(PageScheme.Flat));
        Assert.That(GlassTheme.Theme.Id, Is.EqualTo(PageScheme.Glass));
    });
}
