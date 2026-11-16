using LouisAgent.Site.Components.Landing;
using LouisAgent.Site.Content;
using LouisAgent.Web.Storage;
using LouisAgent.Web.Text;
using LouisAgent.Web.Usage;
using Microsoft.Extensions.DependencyInjection;
using LouisAgent.Site.Layout;

namespace LouisAgent.Site.Tests.Layout;

/// <summary>
/// The site's frame leads everywhere the site goes: to every page of the menu, to the coding agent's chat at
/// <c>/chat</c> once it opens (closed today, FX88), and, in its corner, to the guide (F109.3).
/// </summary>
public class SiteLayoutTests
{
    /// <summary>A context with what the guide in the corner asks for, over the stub API.</summary>
    private static BunitContext Context()
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var settings = new ChatSettings();
        var api = FixtureApi.Client(settings);
        ctx.Services.AddSingleton(settings);
        ctx.Services.AddSingleton(api);
        ctx.Services.AddSingleton(new CurrencyState(api, ctx.JSInterop.JSRuntime));
        ctx.Services.AddSingleton(new ChatText());
        return ctx;
    }

    /// <summary>
    /// Every page's footer links to Louis's LinkedIn profile with LinkedIn's mark (FX103, Louis: "my linked in can also
    /// be added here"), after the rights line.
    /// </summary>
    [Test]
    public void TheFooter_LinksToLinkedIn()
    {
        using var ctx = Context();

        var cut = ctx.Render<SiteLayout>();

        var link = cut.Find(".site-footer a.footer-link");
        Assert.Multiple(() =>
        {
            Assert.That(link.GetAttribute("href"), Is.EqualTo("https://www.linkedin.com/in/louismarais86"));
            Assert.That(link.QuerySelector("svg[aria-label='LinkedIn']"), Is.Not.Null);
            Assert.That(link.TextContent.Trim(), Is.EqualTo("LinkedIn"));
        });
    }

    /// <summary>
    /// The menu ends with the CV mark (F142, Louis: "little icon CV there"): a small document and "CV", to /cv, with
    /// a name a screen reader says.
    /// </summary>
    [Test]
    public void TheMenu_EndsWithTheCvMark()
    {
        using var ctx = Context();

        var last = ctx.Render<SiteLayout>().FindAll(".site-nav > a, .site-nav > .site-drop").Last();

        Assert.Multiple(() =>
        {
            Assert.That(last.GetAttribute("href"), Is.EqualTo("/cv"));
            Assert.That(last.ClassList, Does.Contain("cv-mark"));
            Assert.That(last.TextContent.Trim(), Is.EqualTo("CV"));
            Assert.That(last.GetAttribute("aria-label"), Is.EqualTo("Louis's CV"));
            Assert.That(last.QuerySelectorAll("svg"), Has.Count.EqualTo(1));
        });
    }

    /// <summary>The menu leads to each page, from every page.</summary>
    [TestCase("/framework/how-it-works")]
    [TestCase("/framework/architecture")]
    [TestCase("/framework/identities")]
    [TestCase("/framework/packs")]
    [TestCase("/framework/build-your-own")]
    [TestCase("/csharp-ai-agent-framework")]
    [TestCase("/framework/measured")]
    [TestCase("/framework/built-on-it")]
    [TestCase("/framework/whats-coming")]
    [TestCase("/contact")]
    [TestCase("/what-we-build")]
    [TestCase("/pricing")]
    public void TheMenu_LeadsToThePage(string address)
    {
        using var ctx = Context();

        var cut = ctx.Render<SiteLayout>();

        Assert.That(cut.FindAll($".site-nav a[href='{address}']"), Has.Count.EqualTo(1));
    }

    /// <summary>
    /// The menu leads with what the site sells (F128.1, Louis's pick 5A): What we build, Pricing, Demos, Contact, and
    /// the framework last of its pages, with the CV mark closing the bar (F142).
    /// </summary>
    [Test]
    public void TheMenu_LeadsWithWhatWeSell()
    {
        using var ctx = Context();

        var cut = ctx.Render<SiteLayout>();

        var items = cut.Find(".site-nav").Children
            .Select(item => (item.QuerySelector(".site-drop-label") ?? item).TextContent.Trim());
        Assert.That(items,
            Is.EqualTo(new[] { "What we build", "Pricing", "Demos", "Contact", "The framework", "CV" }));
    }

    /// <summary>
    /// The framework's pages sit in one group (FX73, Louis: "if the menu gets too long we should group some items"),
    /// since F128.1 with Measured, Built on it and What's coming inside it; the comparison with Microsoft's frameworks
    /// is the last (F123).
    /// </summary>
    [Test]
    public void TheFrameworksPages_AreOneGroup()
    {
        using var ctx = Context();

        var cut = ctx.Render<SiteLayout>();

        Assert.Multiple(() =>
        {
            var menu = cut.FindAll(".site-nav .site-drop")
                .Single(group => group.QuerySelector(".site-drop-label")!.TextContent == "The framework");
            Assert.That(menu.QuerySelectorAll("a").Select(a => a.GetAttribute("href")),
                Is.EqualTo(new[]
                {
                    "/framework/how-it-works", "/framework/architecture", "/framework/identities",
                    "/framework/packs", "/framework/build-your-own", "/framework/measured",
                    "/framework/built-on-it", "/framework/whats-coming", "/csharp-ai-agent-framework"
                }));
            Assert.That(cut.FindAll(".site-nav > a"), Has.Count.EqualTo(4),
                "the rest stand in the bar, the CV mark among them (F142)");
        });
    }

    /// <summary>From every page you can ask for a site of your own: the header ends with that button.</summary>
    [Test]
    public void TheHeader_EndsWithTheWayToAskForYours()
    {
        using var ctx = Context();

        var cut = ctx.Render<SiteLayout>();

        var ask = cut.Find(".site-header .site-actions > a.btn.btn-solid");
        Assert.Multiple(() =>
        {
            Assert.That(ask.TextContent, Is.EqualTo("Ask for yours"));
            Assert.That(ask.GetAttribute("href"), Is.EqualTo("/contact"));
        });
    }

    /// <summary>
    /// The header's two buttons are glass (FX159, Louis: "all glas buttons"): the way to ask is the theme's main button
    /// and the CV its quiet one, so they wear the drawings where a theme has them.
    /// </summary>
    [Test]
    public void TheHeadersButtons_AreTheThemesPresses()
    {
        string site = Path.Combine(TestPaths.RepoRoot, "src/site/louis-agent.site");
        string elements = File.ReadAllText(Path.Combine(site, "Styles/site/base/_elements.scss"));
        string cv = File.ReadAllText(Path.Combine(site, "Components/Cv/CvLink.razor.scss"));

        Assert.Multiple(() =>
        {
            Assert.That(elements, Does.Contain(".btn-solid { height: 42px; @include surface.press(); }"));
            Assert.That(cv, Does.Contain("@include surface.quiet-press();"));
        });
    }

    /// <summary>The slogan sits under the brand (FX79, Louis: "this is our slogan under our brand").</summary>
    [Test]
    public void TheSlogan_SitsUnderTheBrand()
    {
        using var ctx = Context();

        var cut = ctx.Render<SiteLayout>();

        Assert.That(cut.Find(".site-brand .site-tagline").TextContent, Is.EqualTo("100% C#, because you can"));
    }

    /// <summary>The footer says why it is all C#, in Louis's words (FX72).</summary>
    [Test]
    public void TheFooter_SaysWhyItIsCSharp()
    {
        using var ctx = Context();

        var cut = ctx.Render<SiteLayout>();

        Assert.That(cut.Find(".site-footer").TextContent, Does.Contain("100% C#, because you can"));
    }

    /// <summary>
    /// The brand is the name with the mark as its o (F151.6), and a screen reader hears "louis-agent". The old
    /// mark's picture is gone from the name.
    /// </summary>
    [Test]
    public void TheBrand_IsTheNameWithTheMarkAsItsO()
    {
        using var ctx = Context();

        var cut = ctx.Render<SiteLayout>();

        Assert.Multiple(() =>
        {
            Assert.That(cut.Find(".site-brand .wordmark .wordmark-name").TextContent, Is.EqualTo("louis-agent"));
            Assert.That(cut.Find(".site-brand .wordmark .wordmark-seen").TextContent, Is.EqualTo("luis-agent"));
            Assert.That(cut.FindAll(".site-brand img"), Is.Empty);
        });
    }

    /// <summary>Each of the site's three frames draws the name that way, the showroom's and the admin's too.</summary>
    [TestCase("SiteLayout")]
    [TestCase("ShowroomLayout")]
    [TestCase("AdminLayout")]
    public void EachFrame_DrawsTheNameWithTheMark(string layout)
    {
        string markup = File.ReadAllText(Path.Combine(TestPaths.RepoRoot,
            "src/site/louis-agent.site/Layout", $"{layout}.razor"));

        Assert.That(markup, Does.Contain("<Brand><Wordmark Before=\"l\" After=\"uis-agent\" /></Brand>"));
    }

    /// <summary>The footer's name is drawn the same way.</summary>
    [Test]
    public void TheFootersName_IsDrawnWithTheMark()
    {
        using var ctx = Context();

        var cut = ctx.Render<SiteLayout>();

        Assert.That(cut.Find(".site-footer .wordmark .wordmark-name").TextContent, Is.EqualTo("louis-agent"));
    }

    /// <summary>
    /// The demos are a menu of their own (F122, Louis: "add this to a menu called demos and list them there, then take
    /// these buttons out"): the index and each demo, and no "Watch it work" beside the menu; never /chat, which stays
    /// closed to visitors (FX88).
    /// </summary>
    [Test]
    public void TheHeader_ListsTheDemos_InAMenu()
    {
        using var ctx = Context();

        var cut = ctx.Render<SiteLayout>();

        var demos = cut.FindAll(".site-nav .site-drop")
            .Single(menu => menu.QuerySelector(".site-drop-label")!.TextContent == "Demos");
        Assert.Multiple(() =>
        {
            Assert.That(SiteDoors.CodingAgent, Is.False, "the live chat is closed today");
            Assert.That(demos.QuerySelectorAll("a").Select(a => a.GetAttribute("href")),
                Is.EqualTo(new[]
                {
                    "/demos", "/guesthouse-demo", "/restaurant-demo", "/law-practice-demo", "/coding-agent",
                    "/triage-demo"
                }));
            Assert.That(cut.Find(".site-actions").TextContent, Does.Not.Contain("Watch it work"));
            Assert.That(cut.FindAll("a[href='/chat']"), Is.Empty);
        });
    }

    /// <summary>
    /// The guide stands on every page as the bar at its foot, the one the showroom has (F151.6, Louis: "we will use
    /// the same guide as in design chat widget everywhere"): no tour button floats in the corner.
    /// </summary>
    [Test]
    public void TheCorner_HoldsTheGuide_AsTheBar()
    {
        using var ctx = Context();

        var cut = ctx.Render<SiteLayout>();

        Assert.That(cut.FindAll(".site-corner .chat-widget.is-bar .agent-bar"), Has.Count.EqualTo(1));
        Assert.That(cut.FindAll("button.widget-tour"), Is.Empty);
        Assert.That(cut.FindAll("button.widget-button"), Is.Empty);
    }

    /// <summary>
    /// While the chat is closed, a chat link leads to the recorded session with its own words, wherever it was told
    /// to go, so no door on the site is a dead end.
    /// </summary>
    [Test]
    public void AChatLink_LeadsToTheRecording_WhileTheChatIsClosed()
    {
        using var ctx = Context();

        var cut = ctx.Render<ChatLink>(parameters => parameters
            .Add(link => link.Address, "https://agent.example/chat")
            .Add(link => link.Class, "btn btn-fill")
            .Add(link => link.Watch, "Watch it work")
            .AddChildContent("Try it"));

        Assert.Multiple(() =>
        {
            Assert.That(cut.Find("a.btn.btn-fill").GetAttribute("href"), Is.EqualTo("/coding-agent"));
            Assert.That(cut.Find("a").TextContent, Is.EqualTo("Watch it work"));
            Assert.That(cut.Markup, Does.Not.Contain("Coming soon"));
        });
    }
}
