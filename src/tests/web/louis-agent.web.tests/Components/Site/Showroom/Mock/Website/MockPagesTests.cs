using LouisAgent.Web.Components.Site.Showroom.Mock;
using LouisAgent.Web.Components.Site.Showroom.Mock.Places;
using LouisAgent.Web.Components.Site.Showroom.Mock.Website;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Tests.Components.Site.Showroom.Mock.Website;

/// <summary>
/// F127.2 (Louis: "3 pagers with working pages and sections"): a mock website of pages. Each page registers with its
/// mock, which draws its menu from them and shows the page the address it's on is on; the hero is home's, another page
/// opens with its heading, a page off the menu goes back to its parent, and a visitor can click through.
/// </summary>
public class MockPagesTests
{
    /// <summary>
    /// One page of the test website: its address, its menu word, whether it's in the menu, its parent, and what's on
    /// it (a line of its own words unless given).
    /// </summary>
    private sealed record TestPage(
        string Path,
        string Label,
        bool InMenu = true,
        string? Parent = null,
        RenderFragment? Content = null);

    /// <summary>The Stay page's card that opens the house rules, a page of their own.</summary>
    private static readonly RenderFragment RulesCard = builder =>
    {
        builder.OpenComponent<MockCard>(0);
        builder.AddComponentParameter(1, nameof(MockCard.Path), "/stay/the-house");
        builder.AddComponentParameter(2, nameof(MockCard.Title), "The house");
        builder.AddComponentParameter(3, nameof(MockCard.Opens), "/stay/rules");
        builder.CloseComponent();
    };

    private static readonly TestPage[] Guesthouse =
    [
        new("/", "Home"), new("/rooms", "Rooms"), new("/stay", "Stay", Content: RulesCard),
        new("/stay/rules", "House rules", InMenu: false, Parent: "/stay"),
    ];

    /// <summary>A line of a page's own words.</summary>
    private static RenderFragment WordsOf(string label) =>
        words => words.AddMarkupContent(0, $"<p class=\"words\">{label}'s words</p>");

    /// <summary>The pages as a site writes them, each with a line of its own words.</summary>
    private static RenderFragment Pages(IEnumerable<TestPage> pages) => builder =>
    {
        foreach (var page in pages)
        {
            builder.OpenComponent<MockPage>(0);
            builder.SetKey(page.Path);
            builder.AddComponentParameter(1, nameof(MockPage.Path), page.Path);
            builder.AddComponentParameter(2, nameof(MockPage.Label), page.Label);
            builder.AddComponentParameter(3, nameof(MockPage.InMenu), page.InMenu);
            builder.AddComponentParameter(4, nameof(MockPage.Parent), page.Parent);
            builder.AddComponentParameter(5, nameof(MockPage.ChildContent), page.Content ?? WordsOf(page.Label));
            builder.CloseComponent();
        }
    };

    /// <summary>
    /// The guesthouse's mock, at <paramref name="shown"/>, telling <paramref name="went"/> where a click went.
    /// </summary>
    private static IRenderedComponent<SiteMock> Mock(BunitContext ctx, string? shown = null, List<string>? went = null)
    {
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        return ctx.Render<SiteMock>(parameters => parameters
            .Add(mock => mock.Address, "seemeeu.example")
            .Add(mock => mock.Business, "Seemeeu")
            .Add(mock => mock.Colours, new MockColours("#0b3a4a", "#e9dcc3", "#fbf8f2", "#14262d"))
            .Add(mock => mock.HeroLine, "Four rooms by the beach.")
            .Add(mock => mock.Pages, Pages(Guesthouse))
            .Add(mock => mock.Shown, shown)
            .Add(mock => mock.Went, (string path) => went?.Add(path))
            .Add(mock => mock.Agent, "<div class=\"chat\">The chat</div>"));
    }

    /// <summary>The page on screen: the one view that isn't hidden.</summary>
    private static string OnScreen(IRenderedComponent<SiteMock> cut) =>
        cut.FindAll(".mock-view").Single(view => !view.HasAttribute("hidden")).GetAttribute("data-page")!;

    /// <summary>The menu lists the pages that are in it, in the order the site wrote them.</summary>
    [Test]
    public void TheMenu_ListsItsPages_InOrder()
    {
        using var ctx = new BunitContext();

        var menu = Mock(ctx).FindAll(".mock-nav button.mock-nav-item").Select(item => item.TextContent);

        Assert.That(menu, Is.EqualTo(new[] { "Home", "Rooms", "Stay" }));
    }

    /// <summary>
    /// The address the agent showed picks the page: only it shows, its menu word is marked current, and the bar
    /// shows the address.
    /// </summary>
    [Test]
    public void TheShownAddress_PicksItsPage()
    {
        using var ctx = new BunitContext();

        var cut = Mock(ctx, "/rooms/garden");

        Assert.Multiple(() =>
        {
            Assert.That(OnScreen(cut), Is.EqualTo("/rooms"));
            Assert.That(cut.Find(".mock-nav [aria-current='page']").TextContent, Is.EqualTo("Rooms"));
            Assert.That(cut.Find(".mock-address").TextContent, Is.EqualTo("seemeeu.example/rooms/garden"));
        });
    }

    /// <summary>The hero and the facts are home's; another page opens with its own heading instead.</summary>
    [Test]
    public void TheHero_IsHomes_AndAnotherPageHasItsHeading()
    {
        using var ctx = new BunitContext();

        var home = Mock(ctx);
        var rooms = Mock(ctx, "/rooms");

        Assert.Multiple(() =>
        {
            Assert.That(home.FindAll(".mock-hero"), Has.Count.EqualTo(1));
            Assert.That(rooms.FindAll(".mock-hero"), Is.Empty);
            Assert.That(rooms.Find(".mock-view[data-page='/rooms'] h2.mock-page-heading").TextContent,
                Is.EqualTo("Rooms"));
        });
    }

    /// <summary>
    /// A visitor clicks through (the websites are working examples): a menu word opens its page, the bar says where,
    /// the site hears of it, and the mock goes to the page's top.
    /// </summary>
    [Test]
    public void AMenuClick_OpensItsPage()
    {
        using var ctx = new BunitContext();
        var went = new List<string>();
        var cut = Mock(ctx, went: went);

        cut.FindAll(".mock-nav button.mock-nav-item").Single(item => item.TextContent == "Stay").Click();

        Assert.Multiple(() =>
        {
            Assert.That(OnScreen(cut), Is.EqualTo("/stay"));
            Assert.That(cut.Find(".mock-address").TextContent, Is.EqualTo("seemeeu.example/stay"));
            Assert.That(went, Is.EqualTo(new[] { "/stay" }));
            Assert.That(ctx.JSInterop.Invocations.Select(call => call.Identifier), Does.Contain("toTop"));
        });
    }

    /// <summary>
    /// A page change loads like a browser (Louis: "we need a clear transition effect when this happens"; his pick of
    /// three): the new address glows in the bar and a loading line runs under it, each drawn afresh on every change so
    /// its animation plays again; a website's first page loads without them.
    /// </summary>
    [Test]
    public void APageChange_LoadsLikeABrowser()
    {
        using var ctx = new BunitContext();
        var cut = Mock(ctx);
        int atFirst = cut.FindAll(".mock-chrome .mock-loading").Count;

        cut.FindAll(".mock-nav button.mock-nav-item").Single(item => item.TextContent == "Stay").Click();
        var line = cut.Find(".mock-chrome .mock-loading");
        cut.FindAll(".mock-nav button.mock-nav-item").Single(item => item.TextContent == "Rooms").Click();
        var again = cut.Find(".mock-chrome .mock-loading");

        Assert.Multiple(() =>
        {
            Assert.That(atFirst, Is.Zero);
            Assert.That(cut.Find(".mock-chrome .mock-address").ClassList, Does.Contain("is-new"));
            Assert.That(again, Is.Not.SameAs(line), "drawn afresh, so its animation plays again");
        });
    }

    /// <summary>A page off the menu has a way back to its parent, by the parent's name, which goes there.</summary>
    [Test]
    public void APageOffTheMenu_GoesBackToItsParent()
    {
        using var ctx = new BunitContext();
        var cut = Mock(ctx, "/stay/rules");

        var back = cut.Find(".mock-view[data-page='/stay/rules'] button.mock-back");
        string label = back.TextContent;
        back.Click();

        Assert.That((label, OnScreen(cut)), Is.EqualTo(("← Stay", "/stay")));
    }

    /// <summary>
    /// A card can open a page of its own (Louis: "navigate to the will and estates page"): its Read more goes there,
    /// as a visitor's click, and the page's way back returns.
    /// </summary>
    [Test]
    public void ACardThatOpensAPage_GoesThere()
    {
        using var ctx = new BunitContext();
        var went = new List<string>();
        var cut = Mock(ctx, "/stay", went);

        var open = cut.Find(".mock-view[data-page='/stay'] article.mock-card button.mock-card-open");
        string label = open.TextContent;
        open.Click();

        Assert.Multiple(() =>
        {
            Assert.That(label, Is.EqualTo("Read more →"));
            Assert.That(OnScreen(cut), Is.EqualTo("/stay/rules"));
            Assert.That(went, Is.EqualTo(new[] { "/stay/rules" }));
        });
    }

    /// <summary>A place the agent shows after a visitor's click wins: the website goes where the agent says.</summary>
    [Test]
    public void TheAgentsNextPlace_WinsOverAClick()
    {
        using var ctx = new BunitContext();
        var cut = Mock(ctx);
        cut.FindAll(".mock-nav button.mock-nav-item").Single(item => item.TextContent == "Stay").Click();

        cut.Render(parameters => parameters.Add(mock => mock.Shown, "/rooms/garden"));

        Assert.That(OnScreen(cut), Is.EqualTo("/rooms"));
    }
}
