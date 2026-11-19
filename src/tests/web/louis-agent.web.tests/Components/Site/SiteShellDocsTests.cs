using LouisAgent.Web.Components.Site;
using LouisAgent.Web.Components.Site.Bands;
using LouisAgent.Web.Components.Site.Docs;
using LouisAgent.Web.Components.Site.Slides;
using LouisAgent.Web.Components.Widget.Door;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Tests.Components.Site;

/// <summary>
/// F164.1: the frame's third layout, for docs-type content (Louis's pick of three designs, "A, with B's hero on the
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

    /// <summary>
    /// F164.2: what stands beside the page, such as a docked chat, has the right column on every screen, so a phone
    /// still has its button; "On this page" keeps to an extra-large screen, as before.
    /// </summary>
    [Test]
    public void InDocs_WhatStandsBeside_HasTheRightColumn_OnEveryScreen()
    {
        using var ctx = Context();
        var cut = ctx.Render<SiteShell>(shell => shell
            .Add(s => s.Layout, PageLayout.Docs)
            .Add(s => s.Beside, "<div class=\"the-chat\">Ask</div>")
            .Add(s => s.ChildContent, TwoBands));

        cut.WaitForAssertion(() =>
        {
            var beside = cut.Find(".site-docs .site-docs-beside");
            Assert.Multiple(() =>
            {
                Assert.That(beside.QuerySelector(".the-chat"), Is.Not.Null, "what stands beside, in the right column");
                Assert.That(beside.ClassList, Does.Not.Contain("d-none"), "drawn on every screen");
                Assert.That(beside.QuerySelector(".site-docs-sections")!.ClassList,
                    Is.SupersetOf(new[] { "d-none", "d-xl-block" }), "On this page, from an extra-large screen");
            });
        });
    }

    /// <summary>
    /// F164.2 (Louis's pick A): "Ask about this page" stands at the top of "On this page", so it hides where the list
    /// hides, and a press asks the site's question through the door.
    /// </summary>
    [Test]
    public void InDocs_AskAboutThisPage_StandsOverOnThisPage_AndAsksThroughTheDoor()
    {
        using var ctx = Context();
        var door = new GuideDoor();
        ctx.Services.AddSingleton(door);
        var heard = new List<string?>();
        door.Asked += heard.Add;
        var ask = new PageAsk("Ask about this page", "What is this page about?");

        var cut = ctx.Render<SiteShell>(shell => shell
            .Add(s => s.Layout, PageLayout.Docs)
            .Add(s => s.AskAboutPage, ask)
            .Add(s => s.ChildContent, TwoBands));
        cut.WaitForAssertion(() => Assert.That(cut.FindAll(".site-docs-sections button"), Has.Count.EqualTo(1)));
        var button = cut.Find(".site-docs-sections button");
        button.Click();

        Assert.Multiple(() =>
        {
            Assert.That(button.TextContent, Does.Contain("Ask about this page"));
            Assert.That(heard, Is.EqualTo(new string?[] { "What is this page about?" }));
        });
    }

    /// <summary>A site that gives no question has no button over "On this page".</summary>
    [Test]
    public void InDocs_WithNoQuestion_NothingStandsOverOnThisPage()
    {
        using var ctx = Context();

        var cut = ctx.Render<SiteShell>(shell => shell
            .Add(s => s.Layout, PageLayout.Docs)
            .Add(s => s.ChildContent, TwoBands));

        cut.WaitForAssertion(() =>
            Assert.That(cut.FindAll(".site-docs-sections nav.site-index"), Has.Count.EqualTo(1)));
        Assert.That(cut.FindAll(".site-docs-sections button"), Is.Empty);
    }
}
