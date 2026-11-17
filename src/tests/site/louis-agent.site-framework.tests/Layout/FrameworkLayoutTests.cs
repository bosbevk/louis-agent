using LouisAgent.SiteFramework;
using LouisAgent.SiteFramework.Layout;
using LouisAgent.Web.Components.Areas;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.SiteFramework.Tests.Layout;

/// <summary>
/// F160.1: the framework's site wears the docs layout Louis picked: its pages' menu on the left, the page in the
/// middle with the page before and after at its foot, and "On this page" on the right.
/// </summary>
public class FrameworkLayoutTests
{
    /// <summary>The frame is the docs layout, the framework's pages in its menu, a page turn at the page's foot.</summary>
    [Test]
    public void TheSite_WearsTheDocsLayout()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddSiteAreas(FrameworkAreas.Name);
        ctx.Services.AddSingleton(FrameworkAreas.Addresses());

        var cut = ctx.Render<FrameworkLayout>(layout => layout.Add(l => l.Body, "<section id=\"top\">Page</section>"));

        Assert.Multiple(() =>
        {
            Assert.That(cut.FindAll(".site.layout-docs"), Has.Count.EqualTo(1));
            Assert.That(cut.Find(".site-docs-menu nav.docs-menu a").GetAttribute("href"), Is.EqualTo("/"));
            Assert.That(cut.FindAll(".site-main nav.page-turn"), Has.Count.EqualTo(1));
        });
    }
}
