using System.Net;
using System.Reflection;
using LouisAgent.Site.Components.Showroom;
using LouisAgent.Site.Layout;
using LouisAgent.Site.Pages.Agents.Showroom;
using LouisAgent.Web.Components.Recorded;
using LouisAgent.Web.Components.Site.Addresses;
using LouisAgent.Web.Components.Site.Showroom;
using LouisAgent.Web.Components.Site.Tiles;
using LouisAgent.Web.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Site.Tests.Showroom;

/// <summary>
/// F127.1: the demos live in the showroom. The site registers its five, each in its group and colour, and a business's
/// demo plays on a mock of its own website, with its sample brand and the cues of what to watch.
/// </summary>
public class ShowroomPagesTests
{
    /// <summary>
    /// The site's five demos, in the rail's order, each in its group and its own colour (Louis's pick 1a).
    /// </summary>
    [Test]
    public void TheSite_RegistersItsFiveDemos_EachInItsColour()
    {
        var demos = SiteDemos.All.All.Select(d => (d.Address, d.Group, d.Accent));

        Assert.That(demos,
            Is.EqualTo(new[]
            {
                ("/guesthouse-demo", DemoGroup.ForYourBusiness, TileAccent.Yellow),
                ("/restaurant-demo", DemoGroup.ForYourBusiness, TileAccent.Green),
                ("/law-practice-demo", DemoGroup.ForYourBusiness, TileAccent.Blue),
                ("/coding-agent", DemoGroup.AgentsAtWork, TileAccent.Red),
                ("/triage-demo", DemoGroup.AgentsAtWork, TileAccent.Purple),
            }));
    }

    /// <summary>A business's demo page draws in the showroom, not the landing's frame.</summary>
    [TestCase(typeof(RestaurantDemoPage))]
    [TestCase(typeof(LawPracticeDemoPage))]
    [TestCase(typeof(GuesthouseDemoPage))]
    public void ABusinessDemo_DrawsInTheShowroom(Type page) =>
        Assert.That(page.GetCustomAttribute<LayoutAttribute>()?.LayoutType, Is.EqualTo(typeof(ShowroomLayout)));

    /// <summary>
    /// A place on a demo page, opened afresh (Louis: "when you refresh the page it loses the demo container"), draws
    /// in the showroom too: the site's router gives its place page the layout of the page the address names.
    /// </summary>
    [TestCase("restaurant-demo/the-demo")]
    [TestCase("law-practice-demo/what-to-watch")]
    [TestCase("guesthouse-demo/ask")]
    public void APlaceOnADemo_DrawsInTheShowroom(string path)
    {
        var place = new RouteData(typeof(SitePlaces), new Dictionary<string, object?> { ["Path"] = path });

        Assert.That(PlaceLayout.Of(place, typeof(SiteLayout), SiteAddresses.From("home", typeof(App).Assembly)),
            Is.EqualTo(typeof(ShowroomLayout)));
    }

    /// <summary>
    /// A business's demo plays on a mock of its own website, in its colour, with its own address, its sample brand's
    /// mark, and three cues of what to watch beside it.
    /// </summary>
    [TestCase(typeof(RestaurantDemoPage), "accent-green", "wheatfieldkitchen.example")]
    [TestCase(typeof(LawPracticeDemoPage), "accent-blue", "koringland.example")]
    [TestCase(typeof(GuesthouseDemoPage), "accent-yellow", "seemeeu.example")]
    public void ABusinessDemo_PlaysOnItsOwnWebsite(Type page, string accent, string address)
    {
        using var ctx = new BunitContext();

        var cut = Render(ctx, page);

        Assert.Multiple(() =>
        {
            Assert.That(cut.Find(".demo-stage").ClassList, Does.Contain(accent));
            Assert.That(cut.Find(".site-mock .mock-address").TextContent, Is.EqualTo(address));
            Assert.That(cut.Find(".site-mock .mock-header .mock-mark svg"), Is.Not.Null, "its sample brand's mark");
            Assert.That(cut.Find(".site-mock .mock-tagline").TextContent, Is.Not.Empty, "its tagline");
            Assert.That(cut.FindAll(".stage-cues .cues-list > li"), Has.Count.EqualTo(3));
            Assert.That(cut.FindAll("h1"), Has.Count.EqualTo(1));
            var chat = cut.FindComponent<RecordedChat>().Instance;
            Assert.That((chat.Pace, chat.TurnGap), Is.EqualTo((2.5, 2200)),
                "at a person's pace (Louis: \"slow it down like realtime\"), a breath before each question");
        });
    }

    /// <summary>
    /// A business's website is a full one, each in a look of its own (Louis: "a bit bland, we need to sell it ... give
    /// each one a different look"): its scene, a call to action, four facts, then its places, each at its address on
    /// the business's site, the places its agent can show.
    /// </summary>
    [TestCase(typeof(RestaurantDemoPage), "look-split",
        new[]
        {
            "/menu", "/menu/sunday-roast", "/menu/lamb-shank", "/menu/chicken-pie", "/menu/vegetable-bake",
            "/menu/malva-pudding", "/children", "/wine", "/find-us",
        })]
    [TestCase(typeof(LawPracticeDemoPage), "look-editorial",
        new[]
        {
            "/practice", "/practice/wills", "/practice/property", "/practice/family", "/practice/contracts",
            "/consultation", "/what-to-bring",
        })]
    [TestCase(typeof(GuesthouseDemoPage), "look-banner",
        new[]
        {
            "/rooms", "/rooms/sea-view", "/rooms/family", "/rooms/garden", "/breakfast", "/the-beach", "/find-us"
        })]
    public void ABusinessWebsite_HasItsLook_AndItsPlaces(Type page, string look, string[] places)
    {
        using var ctx = new BunitContext();

        var mock = Render(ctx, page).Find(".site-mock");

        Assert.Multiple(() =>
        {
            Assert.That(mock.ClassList, Does.Contain(look));
            Assert.That(mock.QuerySelector(".mock-scene svg"), Is.Not.Null, "its scene");
            Assert.That(mock.QuerySelector(".mock-action")?.TextContent, Is.Not.Empty, "its call to action");
            Assert.That(mock.QuerySelectorAll(".mock-facts li"), Has.Length.EqualTo(4));
            Assert.That(mock.QuerySelectorAll("[data-place]").Select(place => place.GetAttribute("data-place")),
                Is.EqualTo(places));
        });
    }

    /// <summary>
    /// A website says only what its agent knows: every price and every time on it is in the business's voice, so the
    /// page and the agent never tell a visitor two different things.
    /// </summary>
    [TestCase(typeof(RestaurantDemoPage), "wheatfield-kitchen")]
    [TestCase(typeof(LawPracticeDemoPage), "koringland-attorneys")]
    [TestCase(typeof(GuesthouseDemoPage), "seemeeu-guesthouse")]
    public void EveryPriceAndTime_OnAWebsite_IsOneItsAgentKnows(Type page, string business)
    {
        using var ctx = new BunitContext();
        string voice = File.ReadAllText(Path.Combine(TestPaths.RepoRoot,
            $"src/site/louis-agent.identity.showroom/prompts/{business}/agent/{business}/voice.md"));

        // Each innermost element's words apart, so a fact never runs into the next one's.
        var texts = Render(ctx, page).Find(".site-mock .mock-page").QuerySelectorAll("*")
            .Where(element => element.ChildElementCount == 0).Select(element => element.TextContent);

        var figures = texts.SelectMany(text => text.Split([' ', '\n', '–'], StringSplitOptions.RemoveEmptyEntries))
            .Select(word => word.Trim('.', ',', ';', ':', '(', ')'))
            .Where(word => IsPrice(word) || IsTime(word))
            .ToList();
        Assert.That(figures, Is.Not.Empty);
        Assert.That(figures.Where(figure => !voice.Contains(figure, StringComparison.Ordinal)), Is.Empty);
    }

    /// <summary>A price in rand: R and a digit, such as <c>R1,450</c>.</summary>
    private static bool IsPrice(string word) => word.Length > 1 && word[0] == 'R' && char.IsAsciiDigit(word[1]);

    /// <summary>A time of day, such as <c>07:30</c>.</summary>
    private static bool IsTime(string word) =>
        word.Contains(':') && word.All(c => char.IsAsciiDigit(c) || c == ':');

    /// <summary>A demo page, its recordings missing: it shows its website and plays nothing.</summary>
    private static IRenderedComponent<IComponent> Render(BunitContext ctx, Type page)
    {
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddSingleton(new ChatText());
        ctx.Services.AddSingleton(new HttpClient(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound)))
        {
            BaseAddress = new Uri("http://site.test/"),
        });

        return ctx.Render(builder =>
        {
            builder.OpenComponent(0, page);
            builder.CloseComponent();
        });
    }
}
