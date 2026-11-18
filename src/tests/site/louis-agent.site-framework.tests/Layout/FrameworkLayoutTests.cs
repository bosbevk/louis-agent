using LouisAgent.SiteFramework;
using LouisAgent.SiteFramework.Layout;
using LouisAgent.Web.Components.Areas;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.SiteFramework.Tests.Layout;

/// <summary>
/// F164.1: the framework's site wears the docs layout Louis picked: its pages' menu on the left, the page in the
/// middle with the page before and after at its foot, and on the right the framework Orbi with "On this page".
/// </summary>
public class FrameworkLayoutTests
{
    /// <summary>
    /// The frame is the docs layout, the framework's pages in its menu, a page turn at the page's foot.
    /// </summary>
    [Test]
    public void TheSite_WearsTheDocsLayout()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddSiteAreas(FrameworkAreas.Name);

        var cut = ctx.Render<FrameworkLayout>(layout => layout.Add(l => l.Body, "<section id=\"top\">Page</section>"));

        Assert.Multiple(() =>
        {
            Assert.That(cut.FindAll(".site.layout-docs"), Has.Count.EqualTo(1));
            Assert.That(cut.Find(".site-docs-menu nav.docs-menu a").GetAttribute("href"), Is.EqualTo("/"));
            Assert.That(cut.FindAll(".site-main nav.page-turn"), Has.Count.EqualTo(1));
        });
    }

    /// <summary>
    /// F164.2: the framework Orbi stands docked beside the page, in the right column, and nothing floats in the
    /// corner.
    /// </summary>
    [Test]
    public void TheFrameworkOrbi_StandsDocked_BesideThePage()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddSingleton(new HttpClient { BaseAddress = new Uri("http://localhost/") });
        ctx.Services.AddSiteAreas(FrameworkAreas.Name);

        var cut = ctx.Render<FrameworkLayout>(layout => layout.Add(l => l.Body, "<section id=\"top\">Page</section>"));

        Assert.Multiple(() =>
        {
            Assert.That(cut.FindAll(".site-docs-beside .chat-widget.is-docked"), Has.Count.EqualTo(1));
            Assert.That(cut.FindAll(".site-corner"), Is.Empty, "nothing floats in the corner but its narrow button");
        });
    }
}
