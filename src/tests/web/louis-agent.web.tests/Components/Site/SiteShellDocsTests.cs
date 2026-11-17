using LouisAgent.Web.Components.Site;
using LouisAgent.Web.Components.Site.Bands;
using LouisAgent.Web.Components.Site.Slides;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Tests.Components.Site;

/// <summary>
/// F160.1: the frame's third layout, for docs-type content (Louis's pick of three designs, "A, with B's hero on the
/// Overview", and "SiteShell gains a docs mode"): the site's menu on the left, the page in a readable middle column,
/// and the page's sections on the right as "On this page", read from the same list the floating index reads.
/// </summary>
public class SiteShellDocsTests
{
    /// <summary>A context whose browser answers the frame's modules with nothing.</summary>
    private static BunitContext Context()
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        return ctx;
    }

    /// <summary>A page of two bands, each putting itself on the frame's list.</summary>
    private static readonly RenderFragment TwoBands = builder =>
    {
        builder.OpenComponent<Band>(0);
        builder.AddAttribute(1, nameof(Band.Id), "one");
        builder.AddAttribute(2, nameof(Band.Topic), "One");
        builder.CloseComponent();
        builder.OpenComponent<Band>(3);
        builder.AddAttribute(4, nameof(Band.Id), "two");
        builder.AddAttribute(5, nameof(Band.Topic), "Two");
        builder.CloseComponent();
    };

    /// <summary>In docs, the menu is on the left, the page in the middle, its sections on the right, once.</summary>
    [Test]
    public void InDocs_TheMenuThePageAndItsSections_StandInThreeColumns()
    {
        using var ctx = Context();

        var cut = ctx.Render<SiteShell>(shell => shell
            .Add(s => s.Layout, PageLayout.Docs)
            .Add(s => s.Sidebar, "<nav class=\"the-menu\">Pages</nav>")
            .Add(s => s.ChildContent, TwoBands));

        cut.WaitForAssertion(() =>
        {
            var docs = cut.Find(".site.layout-docs .site-docs");
            Assert.Multiple(() =>
            {
                Assert.That(docs.QuerySelector(".site-docs-menu .the-menu"), Is.Not.Null, "the site's menu, left");
                Assert.That(docs.QuerySelector("main.site-main section#one"), Is.Not.Null, "the page, middle");
                Assert.That(docs.QuerySelector(".site-docs-sections nav.site-index"), Is.Not.Null, "on this page");
                Assert.That(cut.FindAll("nav.site-index"), Has.Count.EqualTo(1), "the sections drawn once");
            });
        });
    }

    /// <summary>The right column is named for what it holds, so a reader and a screen reader know it.</summary>
    [Test]
    public void InDocs_TheSections_AreOnThisPage()
    {
        using var ctx = Context();

        var cut = ctx.Render<SiteShell>(shell => shell
            .Add(s => s.Layout, PageLayout.Docs)
            .Add(s => s.ChildContent, TwoBands));

        cut.WaitForAssertion(() =>
            Assert.That(cut.Find(".site-docs-sections").GetAttribute("aria-label"), Is.EqualTo("On this page")));
    }

    /// <summary>An ordinary page keeps the flowing frame: no columns, the index floating as before.</summary>
    [Test]
    public void AFlowingPage_HasNoColumns()
    {
        using var ctx = Context();

        var cut = ctx.Render<SiteShell>(shell => shell.Add(s => s.ChildContent, TwoBands));

        Assert.That(cut.FindAll(".site-docs"), Is.Empty);
    }
}
