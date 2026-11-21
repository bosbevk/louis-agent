using LouisAgent.Site.Pages.Framework;
using LouisAgent.Web.Components.Site.Guide;
using LouisAgent.Web.Components.Site.Meta;

namespace LouisAgent.Site.Tests.Pages.Framework;

/// <summary>
/// The theme's page (Louis, 2026-11-18: "move only this to a theme page on the menu ... and rewrite the page"): it
/// speaks to an owner about their own website, and holds the framework's guide, where every control works.
/// </summary>
public class ThemePageTests
{
    /// <summary>Its heading and description speak of your website, and the guide stands under them.</summary>
    [Test]
    public void ThePage_ShowsEveryPartOfYourWebsite_Working()
    {
        using var ctx = SitePageContext.Create();

        var cut = ctx.Render<ThemePage>();

        Assert.Multiple(() =>
        {
            Assert.That(cut.Find("h1.chrome").TextContent, Is.EqualTo("Every part of your website, working."));
            Assert.That(cut.FindComponent<PageHead>().Instance.Description, Does.StartWith("Every button, switch and card"));
            Assert.That(cut.FindComponents<ThemeGuide>(), Has.Count.EqualTo(1));
        });
    }
}
