using LouisAgent.Diagrams.Icons;
using LouisAgent.Web.Components.Site.Addresses;
using LouisAgent.Web.Components.Site.Showroom;
using LouisAgent.Web.Components.Site.Showroom.Start;
using LouisAgent.Web.Components.Site.Tiles;
using LouisAgent.Web.Scripts;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Tests.Components.Site.Showroom.Start;

/// <summary>
/// F134.1: the showroom's start is a stage. Every demo is a window on it, one forward in the light and the rest
/// behind it. Under the stage are the forward demo's name, its line and its way in, and a dock with a button per
/// demo. The stage wears the forward demo's colour, so bringing another forward changes the light.
/// </summary>
public class ShowroomStartTests
{
    /// <summary>
    /// Three demos. The bakery names what fills its window and the word in its bar. The other two name neither.
    /// </summary>
    private static readonly ShowroomDemos Demos = new([
        new ShowroomDemo("/bakery-demo", "A bakery", "Bread at dawn", DemoGroup.ForYourBusiness, TileAccent.Green,
            DiagramIcon.Bed, typeof(BakeryWindow), "bakery.example"),
        new ShowroomDemo("/florist-demo", "A florist", "Flowers by noon", DemoGroup.ForYourBusiness, TileAccent.Blue,
            DiagramIcon.Dining),
        new ShowroomDemo("/coder-demo", "A coder", "Fixes a bug", DemoGroup.AgentsAtWork, TileAccent.Red,
            DiagramIcon.Code),
    ]);

    /// <summary>
    /// What the bakery shows in its window: a site's own component, which the framework doesn't know.
    /// </summary>
    private sealed class BakeryWindow : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder) =>
            builder.AddMarkupContent(0, "<p class=\"bakery-window\">Loaves on a shelf</p>");
    }

    /// <summary>
    /// The start with these demos, the florist forward first. The browser answers the deck's module with nothing
    /// and lists what was asked.
    /// </summary>
    private static IRenderedComponent<ShowroomStart> Start(BunitContext ctx, ShowroomDemos? demos = null)
    {
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddSingleton(demos ?? Demos);
        return ctx.Render<ShowroomStart>(parameters => parameters
            .Add(start => start.Featured, "/florist-demo")
            .Add(start => start.Title, "Watch the agents at work.")
            .Add(start => start.Lede, "Each demo is a real run."));
    }

    /// <summary>The word in the bar of the window that is forward.</summary>
    private static string ForwardAddress(IRenderedComponent<ShowroomStart> start) =>
        start.Find(".deck-item.is-forward .window-address").TextContent;

    /// <summary>
    /// The stage is lit in the featured demo's colour, and holds the page's one heading and its line.
    /// </summary>
    [Test]
    public void TheStage_WearsTheFeaturedDemosColour_AndHoldsThePagesHeading()
    {
        using var ctx = new BunitContext();

        var start = Start(ctx);

        Assert.Multiple(() =>
        {
            Assert.That(start.Find(".lit-stage").ClassList, Does.Contain("accent-blue"), "the florist's colour");
            Assert.That(start.FindAll("h1").Select(heading => heading.TextContent),
                Is.EqualTo(new[] { "Watch the agents at work." }));
            Assert.That(start.Find("section#top .start-lede").TextContent, Is.EqualTo("Each demo is a real run."));
        });
    }

    /// <summary>
    /// Every demo is a framed window on the deck, in the site's order, and the featured one is forward.
    /// </summary>
    [Test]
    public void TheDeck_HoldsAWindowPerDemo_TheFeaturedOneForward()
    {
        using var ctx = new BunitContext();

        var start = Start(ctx);

        Assert.Multiple(() =>
        {
            Assert.That(start.FindAll(".deck .deck-item .framed-window"), Has.Count.EqualTo(3));
            Assert.That(start.FindAll(".deck-item.is-forward"), Has.Count.EqualTo(1));
            Assert.That(ForwardAddress(start), Is.EqualTo("/florist-demo"));
            Assert.That(start.FindAll(".framed-window.is-lit"), Has.Count.EqualTo(1), "only the one forward is lit");
            Assert.That(start.Find(".deck-item.is-forward .framed-window").ClassList, Does.Contain("is-lit"));
        });
    }

    /// <summary>
    /// A demo's own component fills its window. A demo that names none gets the plain body: its icon over its name
    /// and its line.
    /// </summary>
    [Test]
    public void ADemosWindow_IsItsOwn_OrThePlainBody()
    {
        using var ctx = new BunitContext();

        var windows = Start(ctx).FindAll(".deck-item .window-body");

        Assert.Multiple(() =>
        {
            Assert.That(windows[0].QuerySelector(".bakery-window")!.TextContent, Is.EqualTo("Loaves on a shelf"));
            Assert.That(windows[0].QuerySelector(".plain-body"), Is.Null);
            Assert.That(windows[2].QuerySelector(".plain-body .plain-name")!.TextContent, Is.EqualTo("A coder"));
            Assert.That(windows[2].QuerySelector(".plain-body .plain-line")!.TextContent, Is.EqualTo("Fixes a bug"));
            Assert.That(windows[2].QuerySelectorAll(".plain-body svg.line-icon"), Has.Count.EqualTo(1));
        });
    }

    /// <summary>A window's bar says the word its demo names, or the demo's own address when it names none.</summary>
    [Test]
    public void AWindowsBar_SaysItsDemosWord_OrItsAddress()
    {
        using var ctx = new BunitContext();

        var bars = Start(ctx).FindAll(".deck-item .window-address").Select(bar => bar.TextContent);

        Assert.That(bars, Is.EqualTo(new[] { "bakery.example", "/florist-demo", "/coder-demo" }));
    }

    /// <summary>
    /// Under the stage the forward demo is named, with its line and its way in. The way in's mark is a line drawing,
    /// never a character a system draws its own way.
    /// </summary>
    [Test]
    public void TheForwardDemo_IsNamed_WithItsWayIn()
    {
        using var ctx = new BunitContext();

        var caption = Start(ctx).Find(".start-caption");

        Assert.Multiple(() =>
        {
            Assert.That(caption.QuerySelector("h2.start-name")!.TextContent, Is.EqualTo("A florist"));
            Assert.That(caption.QuerySelector(".start-line")!.TextContent, Is.EqualTo("Flowers by noon"));
            Assert.That(caption.QuerySelector("a.start-play")!.GetAttribute("href"), Is.EqualTo("/florist-demo"));
            Assert.That(caption.QuerySelector("a.start-play")!.TextContent.Trim(), Is.EqualTo("Watch this demo"));
            Assert.That(caption.QuerySelectorAll("a.start-play svg.line-icon"), Has.Count.EqualTo(1));
        });
    }

    /// <summary>
    /// The dock has a button per demo, in the site's order, a divider where the group changes, and the forward one
    /// marked current. Each button's id is its demo's address without the slash, so the page can open it by address.
    /// </summary>
    [Test]
    public void TheDock_HasAButtonPerDemo_TheForwardOneCurrent()
    {
        using var ctx = new BunitContext();

        var dock = Start(ctx).Find("nav.dock");

        var buttons = dock.QuerySelectorAll("button.dock-item").ToList();
        Assert.Multiple(() =>
        {
            Assert.That(dock.GetAttribute("aria-label"), Is.EqualTo("All demos"));
            Assert.That(buttons.Select(button => button.Id),
                Is.EqualTo(new[] { "bakery-demo", "florist-demo", "coder-demo" }));
            Assert.That(buttons.Select(button => button.GetAttribute("aria-expanded")),
                Is.EqualTo(new[] { "false", "true", "false" }));
            Assert.That(buttons[0].ClassList, Does.Contain("accent-green"), "each button in its demo's colour");
            Assert.That(dock.QuerySelectorAll(".dock-divider"), Has.Count.EqualTo(1));
        });
    }

    /// <summary>
    /// A press on the dock brings that demo forward: its window, the stage's light, the name under it and the way
    /// in all follow.
    /// </summary>
    [Test]
    public void APressOnTheDock_BringsThatDemoForward()
    {
        using var ctx = new BunitContext();
        var start = Start(ctx);

        start.Find("nav.dock button#coder-demo").Click();

        Assert.Multiple(() =>
        {
            Assert.That(ForwardAddress(start), Is.EqualTo("/coder-demo"));
            Assert.That(start.Find(".lit-stage").ClassList, Does.Contain("accent-red"));
            Assert.That(start.Find(".start-caption h2").TextContent, Is.EqualTo("A coder"));
            Assert.That(start.Find(".start-caption a.start-play").GetAttribute("href"), Is.EqualTo("/coder-demo"));
            Assert.That(start.Find("nav.dock button#coder-demo").GetAttribute("aria-expanded"), Is.EqualTo("true"));
        });
    }

    /// <summary>The deck's arrows move the same thing the dock does, and go round at the ends.</summary>
    [Test]
    public void TheDecksArrows_BringTheNextAndTheOneBeforeForward()
    {
        using var ctx = new BunitContext();
        var start = Start(ctx);

        start.Find(".deck button.deck-next").Click();
        string next = ForwardAddress(start);
        start.Find(".deck button.deck-next").Click();
        string round = ForwardAddress(start);
        start.Find(".deck button.deck-before").Click();

        Assert.That((next, round, ForwardAddress(start)),
            Is.EqualTo(("/coder-demo", "bakery.example", "/coder-demo")));
    }

    /// <summary>The arrows say what they do in the showroom's words, for a screen reader.</summary>
    [Test]
    public void TheDecksArrows_AreNamed()
    {
        using var ctx = new BunitContext();

        var start = Start(ctx);

        Assert.That((start.Find(".deck button.deck-before").GetAttribute("aria-label"),
                start.Find(".deck button.deck-next").GetAttribute("aria-label")),
            Is.EqualTo(("The demo before", "The next demo")));
    }

    /// <summary>A site that registered no demos still gets its heading, and no empty stage under it.</summary>
    [Test]
    public void WithNoDemos_TheStartIsItsHeading()
    {
        using var ctx = new BunitContext();

        var start = Start(ctx, new ShowroomDemos([]));

        Assert.Multiple(() =>
        {
            Assert.That(start.Find("h1").TextContent, Is.EqualTo("Watch the agents at work."));
            Assert.That(start.FindAll(".deck"), Is.Empty);
            Assert.That(start.FindAll(".start-caption"), Is.Empty);
            Assert.That(start.FindAll("nav.dock"), Is.Empty);
        });
    }

    /// <summary>
    /// The start on a site whose places have addresses, opened at this one, with the bakery featured. The browser
    /// answers the start's module with nothing and lists what was asked.
    /// </summary>
    private static IRenderedComponent<ShowroomStart> StartAt(BunitContext ctx, string address)
    {
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddSingleton(Demos);
        ctx.Services.AddSingleton(new SiteAddresses("home", ["/demos"]));
        ctx.Services.GetRequiredService<NavigationManager>().NavigateTo(address);
        return ctx.Render<ShowroomStart>(parameters => parameters
            .Add(start => start.Featured, "/bakery-demo")
            .Add(start => start.Title, "Watch the agents at work."));
    }

    /// <summary>The addresses the start asked the address bar to show, in order.</summary>
    private static List<object?> Shown(BunitContext ctx) =>
        [.. ctx.JSInterop.Invocations.Where(call => call.Identifier == "show").Select(call => call.Arguments[0])];

    /// <summary>
    /// Each demo's window has its own address (ADR 0107). Opened at it, that demo is forward from the first draw, so
    /// the page someone was sent is the page they get, and the bar is not written again.
    /// </summary>
    [Test]
    public void OpenedAtADemosAddress_ThatDemoIsForward()
    {
        using var ctx = new BunitContext();

        var start = StartAt(ctx, "/demos/coder-demo");

        Assert.Multiple(() =>
        {
            Assert.That(ForwardAddress(start), Is.EqualTo("/coder-demo"));
            Assert.That(start.Find(".lit-stage").ClassList, Does.Contain("accent-red"));
            Assert.That(start.Find(".start-caption h2").TextContent, Is.EqualTo("A coder"));
            Assert.That(start.Find("nav.dock button#coder-demo").GetAttribute("aria-expanded"), Is.EqualTo("true"));
            Assert.That(Shown(ctx), Is.Empty);
        });
    }

    /// <summary>At the page's own address, or at a place that is no demo, the featured demo is forward.</summary>
    [TestCase("/demos")]
    [TestCase("/demos/somewhere-else")]
    public void OpenedAnywhereElseOnThePage_TheFeaturedDemoIsForward(string address)
    {
        using var ctx = new BunitContext();

        Assert.That(ForwardAddress(StartAt(ctx, address)), Is.EqualTo("bakery.example"));
    }

    /// <summary>
    /// A press on the dock puts that demo's address in the bar, through the start's own module, so the address a
    /// visitor copies opens what they see.
    /// </summary>
    [Test]
    public void APressOnTheDock_PutsThatDemosAddressInTheBar()
    {
        using var ctx = new BunitContext();
        var start = StartAt(ctx, "/demos");

        start.Find("nav.dock button#coder-demo").Click();

        start.WaitForAssertion(() => Assert.That(Shown(ctx), Is.EqualTo(new[] { "/demos/coder-demo" })));
        Assert.That(ctx.JSInterop.Invocations.Where(call => call.Identifier == "import")
            .Select(call => call.Arguments[0]), Does.Contain(FrameworkScripts.ShowroomStart));
    }

    /// <summary>An arrow moves the address too, since it brings a demo forward as the dock does.</summary>
    [Test]
    public void AnArrow_PutsThatDemosAddressInTheBar()
    {
        using var ctx = new BunitContext();
        var start = StartAt(ctx, "/demos");

        start.Find(".deck button.deck-next").Click();

        start.WaitForAssertion(() => Assert.That(Shown(ctx), Is.EqualTo(new[] { "/demos/florist-demo" })));
    }

    /// <summary>
    /// A site whose places have no addresses still has its start: a press brings the demo forward, and the address
    /// bar is asked to show nothing.
    /// </summary>
    [Test]
    public void OnASiteWithNoAddresses_APressWritesNothing()
    {
        using var ctx = new BunitContext();
        var start = Start(ctx);

        start.Find("nav.dock button#coder-demo").Click();

        Assert.Multiple(() =>
        {
            Assert.That(ForwardAddress(start), Is.EqualTo("/coder-demo"));
            Assert.That(Shown(ctx), Is.Empty);
        });
    }

    /// <summary>A site's own words replace the framework's English.</summary>
    [Test]
    public void ASitesWords_ReplaceTheEnglish()
    {
        using var ctx = new BunitContext();
        ctx.Services.AddSingleton(new ShowroomText { WatchThisDemo = "Kyk na hierdie een" });

        var start = Start(ctx);

        Assert.That(start.Find("a.start-play").TextContent.Trim(), Is.EqualTo("Kyk na hierdie een"));
    }
}
