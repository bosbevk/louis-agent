using Bunit.TestDoubles;
using LouisAgent.Web.Docs.Contents;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Docs.Tests.Contents;

/// <summary>
/// F160.1: the docs layout's left column lists the site's pages under their groups and marks the one being read; a
/// page's foot leads to the page before and the page after.
/// </summary>
public class DocsMenuTests
{
    /// <summary>Two groups, three pages, in reading order.</summary>
    private static readonly DocsContents Book = new([
        new DocsGroup("Start", [new DocsEntry("Overview", "/"), new DocsEntry("Getting started", "/getting-started")]),
        new DocsGroup("Understand", [new DocsEntry("How it works", "/how-it-works")])
    ]);

    /// <summary>A context with the contents, at this address.</summary>
    private static BunitContext At(string address)
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddSingleton(Book);
        ctx.Services.GetRequiredService<NavigationManager>().NavigateTo(address);
        return ctx;
    }

    /// <summary>The menu shows each group's name and its pages, in reading order.</summary>
    [Test]
    public void TheMenu_ListsEachGroupAndItsPages()
    {
        using var ctx = At("/");

        var cut = ctx.Render<DocsMenu>();

        Assert.Multiple(() =>
        {
            Assert.That(cut.Find("nav.docs-menu").GetAttribute("aria-label"), Is.EqualTo("Pages"));
            Assert.That(cut.FindAll(".docs-menu-label").Select(label => label.TextContent.Trim()),
                Is.EqualTo(new[] { "Start", "Understand" }));
            Assert.That(cut.FindAll(".docs-menu a").Select(a => a.GetAttribute("href")),
                Is.EqualTo(new[] { "/", "/getting-started", "/how-it-works" }));
        });
    }

    /// <summary>The page being read is marked, also from one of its sections' own addresses.</summary>
    [TestCase("/getting-started")]
    [TestCase("/getting-started/your-program")]
    public void TheMenu_MarksThePageBeingRead(string address)
    {
        using var ctx = At(address);

        var cut = ctx.Render<DocsMenu>();

        var current = cut.Find(".docs-menu a[aria-current='page']");
        Assert.That(current.GetAttribute("href"), Is.EqualTo("/getting-started"));
    }

    /// <summary>A page's foot leads to the page before and the page after, each named.</summary>
    [Test]
    public void PageTurn_LeadsToBeforeAndAfter()
    {
        using var ctx = At("/getting-started");

        var cut = ctx.Render<PageTurn>();

        Assert.Multiple(() =>
        {
            Assert.That(cut.Find("a.page-turn-before").GetAttribute("href"), Is.EqualTo("/"));
            Assert.That(cut.Find("a.page-turn-before").TextContent, Does.Contain("Overview"));
            Assert.That(cut.Find("a.page-turn-after").GetAttribute("href"), Is.EqualTo("/how-it-works"));
        });
    }

    /// <summary>The last page has no page after it, and says nothing of one.</summary>
    [Test]
    public void PageTurn_OnTheLastPage_HasNoAfter()
    {
        using var ctx = At("/how-it-works");

        var cut = ctx.Render<PageTurn>();

        Assert.That(cut.FindAll("a.page-turn-after"), Is.Empty);
    }
}
