using LouisAgent.Web.Components.Site.Showroom.Mock;
using LouisAgent.Web.Components.Site.Showroom.Mock.Places;
using LouisAgent.Web.Scripts;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Tests.Components.Site.Showroom.Mock;

/// <summary>
/// F127.1 (Louis's pick 3a): a business's agent plays where its customers would meet it, on a mock of the business's
/// own website: a browser bar with its address, its header and one hero line in its own colours, and the agent in a
/// widget in its corner.
/// </summary>
public class SiteMockTests
{
    private static IRenderedComponent<SiteMock> Mock(BunitContext ctx) =>
        ctx.Render<SiteMock>(parameters => parameters
            .Add(mock => mock.Address, "bakery.example")
            .Add(mock => mock.Business, "The Bakery")
            .Add(mock => mock.Colours, new MockColours("#7a3b1d", "#fff8ef", "#fff8ef", "#3b2a1d"))
            .Add(mock => mock.NavItems, ["Bread", "Cakes"])
            .Add(mock => mock.HeroLine, "Fresh before seven.")
            .Add(mock => mock.HeroNote, "Open every day from six.")
            .Add(mock => mock.AgentLabel, "The Bakery · ask us")
            .Add(mock => mock.Agent, "<div class=\"chat\">The recorded chat</div>"));

    /// <summary>
    /// A business's sample brand (Louis: "can we have sample branding in the demos also"): its mark before its name in
    /// the header and before the label in the widget's head, its tagline under the name, and a typeface of its own.
    /// </summary>
    [Test]
    public void ABusinesssSampleBrand_IsWornOnItsSite()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<SiteMock>(parameters => parameters
            .Add(mock => mock.Address, "bakery.example")
            .Add(mock => mock.Business, "The Bakery")
            .Add(mock => mock.Colours, new MockColours("#7a3b1d", "#fff8ef", "#fff8ef", "#3b2a1d"))
            .Add(mock => mock.Mark, "<svg class=\"mark\"></svg>")
            .Add(mock => mock.Tagline, "Baked at dawn")
            .Add(mock => mock.Font, MockFont.Slab)
            .Add(mock => mock.HeroLine, "Fresh before seven.")
            .Add(mock => mock.Agent, "<div class=\"chat\">The recorded chat</div>"));

        Assert.Multiple(() =>
        {
            Assert.That(cut.Find(".mock-header .mock-mark svg.mark"), Is.Not.Null);
            Assert.That(cut.Find(".mock-widget-head .mock-mark svg.mark"), Is.Not.Null);
            Assert.That(cut.Find(".mock-header .mock-tagline").TextContent, Is.EqualTo("Baked at dawn"));
            Assert.That(cut.Find(".site-mock").ClassList, Does.Contain("font-slab"));
        });
    }

    /// <summary>A business with no mark, tagline or typeface of its own gets none, and the serif.</summary>
    [Test]
    public void WithoutABrand_TheMockIsPlain()
    {
        using var ctx = new BunitContext();

        var cut = Mock(ctx);

        Assert.That((cut.FindAll(".mock-mark").Count, cut.FindAll(".mock-tagline").Count,
            cut.Find(".site-mock").ClassList.Contains("font-serif")), Is.EqualTo((0, 0, true)));
    }

    /// <summary>The browser bar shows the business's own address.</summary>
    [Test]
    public void TheBrowserBar_ShowsTheBusinesssAddress()
    {
        using var ctx = new BunitContext();

        Assert.That(Mock(ctx).Find(".mock-chrome .mock-address").TextContent, Is.EqualTo("bakery.example"));
    }

    /// <summary>The business's header has its name and its menu, and its hero line follows.</summary>
    [Test]
    public void TheBusinesssHeader_AndHero_AreItsOwn()
    {
        using var ctx = new BunitContext();

        var cut = Mock(ctx);

        Assert.Multiple(() =>
        {
            Assert.That(cut.Find(".mock-header .mock-name").TextContent, Is.EqualTo("The Bakery"));
            Assert.That(cut.FindAll(".mock-header .mock-nav span").Select(s => s.TextContent),
                Is.EqualTo(new[] { "Bread", "Cakes" }));
            Assert.That(cut.Find(".mock-hero .mock-hero-line").TextContent, Is.EqualTo("Fresh before seven."));
            Assert.That(cut.Find(".mock-hero .mock-hero-note").TextContent, Is.EqualTo("Open every day from six."));
        });
    }

    /// <summary>The business's colours are the mock's own, set on it as variables its styles read.</summary>
    [Test]
    public void TheBusinesssColours_AreSetOnTheMock()
    {
        using var ctx = new BunitContext();

        string style = Mock(ctx).Find(".site-mock").GetAttribute("style")!;

        Assert.That(style, Does.Contain("--mock-brand: #7a3b1d").And.Contain("--mock-on-brand: #fff8ef")
            .And.Contain("--mock-page: #fff8ef").And.Contain("--mock-ink: #3b2a1d"));
    }

    /// <summary>
    /// A fuller website (F127.1, Louis: "a bit bland, we need to sell it ... give each one a different look"): a page
    /// that scrolls inside the mock, its hero in the business's look with the scene the site drew and a call to action,
    /// a strip of facts, then the places the site wrote.
    /// </summary>
    [Test]
    public void ThePage_HasTheHeroInItsLook_TheFacts_AndThePlaces()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<SiteMock>(parameters => parameters
            .Add(mock => mock.Address, "bakery.example")
            .Add(mock => mock.Business, "The Bakery")
            .Add(mock => mock.Colours, new MockColours("#7a3b1d", "#fff8ef", "#fff8ef", "#3b2a1d"))
            .Add(mock => mock.Look, MockLook.Banner)
            .Add(mock => mock.HeroLine, "Fresh before seven.")
            .Add(mock => mock.HeroAction, "Order bread")
            .Add(mock => mock.Scene, "<svg class=\"oven\"></svg>")
            .Add(mock => mock.Facts, ["Open from six", "Rye on Fridays"])
            .Add(mock => mock.Places, "<section class=\"bread\">Bread</section>")
            .Add(mock => mock.Agent, "<div class=\"chat\">The recorded chat</div>"));

        var page = cut.Find(".site-mock > .mock-page");
        Assert.Multiple(() =>
        {
            Assert.That(cut.Find(".site-mock").ClassList, Does.Contain("look-banner"));
            Assert.That(page.Children.Select(child => child.ClassName),
                Is.EqualTo(new[] { "mock-hero", "mock-facts", "bread" }));
            Assert.That(page.QuerySelector(".mock-hero .mock-action")!.TextContent, Is.EqualTo("Order bread"));
            Assert.That(page.QuerySelector(".mock-hero .mock-scene svg.oven"), Is.Not.Null);
            Assert.That(page.QuerySelectorAll(".mock-facts li").Select(li => li.TextContent),
                Is.EqualTo(new[] { "Open from six", "Rye on Fridays" }));
        });
    }

    /// <summary>
    /// A real photo for the hero, worn with the business's own brand (F127.1, Louis: "real images with real branding,
    /// but also not real"): the photo, and over it a badge with the business's mark and name.
    /// </summary>
    [Test]
    public void AHeroPhoto_WearsTheBusinesssBrand()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<SiteMock>(parameters => parameters
            .Add(mock => mock.Address, "bakery.example")
            .Add(mock => mock.Business, "The Bakery")
            .Add(mock => mock.Colours, new MockColours("#7a3b1d", "#fff8ef", "#fff8ef", "#3b2a1d"))
            .Add(mock => mock.Mark, "<svg class=\"mark\"></svg>")
            .Add(mock => mock.HeroLine, "Fresh before seven.")
            .Add(mock => mock.HeroPhoto, "images/bakery.webp")
            .Add(mock => mock.HeroPhotoAlt, "The bakery's counter at dawn")
            .Add(mock => mock.Agent, "<div class=\"chat\">The recorded chat</div>"));

        var scene = cut.Find(".mock-hero .mock-scene");
        Assert.Multiple(() =>
        {
            Assert.That(scene.QuerySelector("img.mock-photo")!.GetAttribute("src"), Is.EqualTo("images/bakery.webp"));
            Assert.That(scene.QuerySelector("img.mock-photo")!.GetAttribute("alt"),
                Is.EqualTo("The bakery's counter at dawn"));
            Assert.That(scene.QuerySelector(".mock-photo-badge svg.mark"), Is.Not.Null);
            Assert.That(scene.QuerySelector(".mock-photo-badge")!.TextContent, Does.Contain("The Bakery"));
        });
    }

    /// <summary>A website with no look of its own is the split one; with no facts or scene, it draws none.</summary>
    [Test]
    public void WithoutALook_TheMockIsSplit_AndDrawsNoEmptyParts()
    {
        using var ctx = new BunitContext();

        var cut = Mock(ctx);

        Assert.That((cut.Find(".site-mock").ClassList.Contains("look-split"), cut.FindAll(".mock-facts").Count,
            cut.FindAll(".mock-scene").Count, cut.FindAll(".mock-action").Count), Is.EqualTo((true, 0, 0, 0)));
    }

    /// <summary>The mock's places for the tests: a section, and a card in it.</summary>
    private static readonly RenderFragment Places = builder =>
    {
        builder.OpenComponent<MockSection>(0);
        builder.AddComponentParameter(1, nameof(MockSection.Path), "/bread");
        builder.AddComponentParameter(2, nameof(MockSection.Heading), "Our bread");
        builder.AddComponentParameter(3, nameof(MockSection.Cards), (RenderFragment)(cards =>
        {
            cards.OpenComponent<MockCard>(0);
            cards.AddComponentParameter(1, nameof(MockCard.Path), "/bread/rye");
            cards.AddComponentParameter(2, nameof(MockCard.Title), "Rye");
            cards.CloseComponent();
        }));
        builder.CloseComponent();
    };

    /// <summary>The bakery's mock with its places, showing <paramref name="shown"/>.</summary>
    private static IRenderedComponent<SiteMock> Showing(BunitContext ctx, string? shown) =>
        ctx.Render<SiteMock>(parameters => parameters
            .Add(mock => mock.Address, "bakery.example")
            .Add(mock => mock.Business, "The Bakery")
            .Add(mock => mock.Colours, new MockColours("#7a3b1d", "#fff8ef", "#fff8ef", "#3b2a1d"))
            .Add(mock => mock.HeroLine, "Fresh before seven.")
            .Add(mock => mock.Places, Places)
            .Add(mock => mock.Shown, shown)
            .Add(mock => mock.Agent, "<div class=\"chat\">The recorded chat</div>"));

    /// <summary>
    /// A place its agent showed (F127.1, Louis: "the agent can navigate the website basically"): its address in the
    /// mock's bar, the place lit, and the mock's own page scrolled to it.
    /// </summary>
    [Test]
    public void AShownPlace_IsInTheBar_LitAndScrolledTo()
    {
        using var ctx = new BunitContext();
        var module = ctx.JSInterop.SetupModule(FrameworkScripts.Mock);
        module.SetupVoid("showPlace", _ => true);

        var cut = Showing(ctx, "/bread/rye");

        Assert.Multiple(() =>
        {
            Assert.That(cut.Find(".mock-address").TextContent, Is.EqualTo("bakery.example/bread/rye"));
            Assert.That(cut.FindAll(".is-shown").Select(place => place.GetAttribute("data-place")),
                Is.EqualTo(new[] { "/bread/rye" }));
            Assert.That(module.Invocations.Where(call => call.Identifier == "showPlace")
                .Select(call => call.Arguments[1]), Is.EqualTo(new[] { "/bread/rye" }));
        });
    }

    /// <summary>With nothing shown, the bar has the website's address, nothing is lit, and its page stays.</summary>
    [Test]
    public void NothingShown_LightsNothing()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        var cut = Showing(ctx, null);

        Assert.That((cut.Find(".mock-address").TextContent, cut.FindAll(".is-shown").Count,
                ctx.JSInterop.Invocations.Count(call => call.Identifier == "showPlace")),
            Is.EqualTo(("bakery.example", 0, 0)));
    }

    /// <summary>The agent sits in a widget in the corner, under its label.</summary>
    [Test]
    public void TheAgent_SitsInTheCornersWidget()
    {
        using var ctx = new BunitContext();

        var widget = Mock(ctx).Find(".site-mock > .mock-widget");

        Assert.That((widget.QuerySelector(".mock-widget-head .mock-widget-label")!.TextContent,
                widget.QuerySelector(".mock-widget-body .chat")!.TextContent),
            Is.EqualTo(("The Bakery · ask us", "The recorded chat")));
    }

    /// <summary>A colour that isn't one (a closing quote and a rule smuggled in) is refused, so a mock's style holds
    /// only colours.</summary>
    [TestCase("#7a3b1d; background: url(x)")]
    [TestCase("red")]
    [TestCase("")]
    public void AColourThatIsntOne_IsRefused(string brand) =>
        Assert.Throws<ArgumentException>(() => _ = new MockColours(brand, "#ffffff", "#ffffff", "#000000"));
}
