using LouisAgent.Starter.Pages;
using LouisAgent.Web.Components.Site.Meta;

namespace LouisAgent.Starter.Tests;

/// <summary>
/// The starter's landing says what it is about (F121), so a site made from it is served to a search engine with its
/// own description from its first deploy; the site's owner rewrites it with the page.
/// </summary>
public class StarterLandingTests
{
    /// <summary>The landing declares one description, which the framework serves in the page's head.</summary>
    [Test]
    public void TheLanding_DeclaresItsDescription()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        var cut = ctx.Render<LandingPage>();

        Assert.That(cut.FindComponents<PageHead>().Single().Instance.Description, Does.Contain("louis-agent"));
    }
}
