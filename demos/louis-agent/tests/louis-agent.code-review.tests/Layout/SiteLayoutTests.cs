using LouisAgent.CodeReview.Components.Landing;
using LouisAgent.CodeReview.Content;
using LouisAgent.CodeReview.Layout;
using LouisAgent.Web.Api;
using LouisAgent.Web.Storage;
using LouisAgent.Web.Text;
using LouisAgent.Web.Usage;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.CodeReview.Tests.Layout;

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

    /// <summary>The menu leads to each page, from every page.</summary>
    [TestCase("/how-it-works")]
    [TestCase("/architecture")]
    [TestCase("/identities")]
    [TestCase("/packs")]
    [TestCase("/build-your-own")]
    [TestCase("/csharp-ai-agent-framework")]
    [TestCase("/proof")]
    [TestCase("/built-on-it")]
    [TestCase("/whats-coming")]
    [TestCase("/contact")]
    public void TheMenu_LeadsToThePage(string address)
    {
        using var ctx = Context();

        var cut = ctx.Render<SiteLayout>();

        Assert.That(cut.FindAll($".site-nav a[href='{address}']"), Has.Count.EqualTo(1));
    }

    /// <summary>
    /// The framework's pages sit in one group (FX73, Louis: "if the menu gets too long we should group some items"), so
    /// the bar stays one line with What's coming on it; the comparison with Microsoft's frameworks is the last (F123).
    /// </summary>
    [Test]
    public void TheFrameworksPages_AreOneGroup()
    {
        using var ctx = Context();

        var cut = ctx.Render<SiteLayout>();

        Assert.Multiple(() =>
        {
            var menu = cut.FindAll(".site-nav .site-menu")[0];
            Assert.That(menu.QuerySelector(".site-menu-label")!.TextContent, Is.EqualTo("The framework"));
            Assert.That(menu.QuerySelectorAll("a").Select(a => a.GetAttribute("href")),
                Is.EqualTo(new[]
                {
                    "/how-it-works", "/architecture", "/identities", "/packs", "/build-your-own",
                    "/csharp-ai-agent-framework"
                }));
            Assert.That(cut.FindAll(".site-nav > a"), Has.Count.EqualTo(4), "the rest stand in the bar");
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

    /// <summary>The brand is the lockup (F110): the mark, its copy, then the name, as the brand guide says.</summary>
    [Test]
    public void TheBrand_IsTheLockup()
    {
        using var ctx = Context();

        var cut = ctx.Render<SiteLayout>();
        var brand = cut.Find(".site-brand");

        Assert.Multiple(() =>
        {
            Assert.That(brand.QuerySelector("img.site-mark")?.GetAttribute("src"), Is.EqualTo("brand/mark.svg"));
            Assert.That(brand.QuerySelector("img.site-mark")?.GetAttribute("alt"), Is.EqualTo(""));
            Assert.That(brand.QuerySelector(".site-brand-name")!.TextContent.Trim(), Is.EqualTo("louis-agent"));
            Assert.That(File.Exists(Path.Combine(TestPaths.RepoRoot,
                    "demos/louis-agent/web/louis-agent.code-review/wwwroot/brand/mark.svg")),
                "the mark is the app's own copy");
        });
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

        var demos = cut.FindAll(".site-nav .site-menu")
            .Single(menu => menu.QuerySelector(".site-menu-label")!.TextContent == "Demos");
        Assert.Multiple(() =>
        {
            Assert.That(SiteDoors.CodingAgent, Is.False, "the live chat is closed today");
            Assert.That(demos.QuerySelectorAll("a").Select(a => a.GetAttribute("href")),
                Is.EqualTo(new[] { "/demos", "/coding-agent", "/triage-demo" }));
            Assert.That(cut.FindAll(".site-actions"), Is.Empty, "no Watch it work");
            Assert.That(cut.FindAll("a[href='/chat']"), Is.Empty);
        });
    }

    /// <summary>
    /// The guide sits in the frame's corner, folded, on every page, offered as the page's tour alone (FX114).
    /// </summary>
    [Test]
    public void TheCorner_HoldsTheGuide_Folded()
    {
        using var ctx = Context();

        var cut = ctx.Render<SiteLayout>();

        Assert.That(cut.FindAll(".site-corner .chat-widget button.widget-tour"), Has.Count.EqualTo(1));
        Assert.That(cut.FindAll(".site-corner button.widget-button"), Is.Empty);
        Assert.That(cut.FindAll(".widget-panel"), Is.Empty);
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
