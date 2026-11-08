using LouisAgent.CodeReview.Components.Landing;
using LouisAgent.CodeReview.Layout;
using LouisAgent.Web.Api;
using LouisAgent.Web.Storage;
using LouisAgent.Web.Text;
using LouisAgent.Web.Usage;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.CodeReview.Tests.Layout;

/// <summary>
/// The site's frame leads everywhere the site goes: to every page of the menu, to the coding agent's chat at
/// <c>/chat</c> unless a link is told the chat lives somewhere else, and, in its corner, to the guide (F109.3).
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

    /// <summary>The menu leads to each page, from every page.</summary>
    [TestCase("/how-it-works")]
    [TestCase("/architecture")]
    [TestCase("/identities")]
    [TestCase("/packs")]
    [TestCase("/build-your-own")]
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
    /// The framework's five pages sit in one group (FX73, Louis: "if the menu gets too long we should group some items"),
    /// so the bar stays one line with What's coming on it.
    /// </summary>
    [Test]
    public void TheFrameworksPages_AreOneGroup()
    {
        using var ctx = Context();

        var cut = ctx.Render<SiteLayout>();

        Assert.Multiple(() =>
        {
            Assert.That(cut.Find(".site-nav .site-menu-label").TextContent, Is.EqualTo("The framework"));
            Assert.That(cut.FindAll(".site-nav .site-menu a").Select(a => a.GetAttribute("href")),
                Is.EqualTo(new[] { "/how-it-works", "/architecture", "/identities", "/packs", "/build-your-own" }));
            Assert.That(cut.FindAll(".site-nav > a"), Has.Count.EqualTo(4), "the rest stand in the bar");
        });
    }

    /// <summary>The footer says why it is all C#, in Louis's words (FX72).</summary>
    [Test]
    public void TheFooter_SaysWhyItIsCSharp()
    {
        using var ctx = Context();

        var cut = ctx.Render<SiteLayout>();

        Assert.That(cut.Find(".site-footer").TextContent, Does.Contain("100% C#, because I can"));
    }

    /// <summary>The brand is the lockup (F110): the mark, its own copy, then the name, as the brand README says.</summary>
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
            Assert.That(brand.TextContent.Trim(), Is.EqualTo("louis-agent"));
            Assert.That(File.Exists(Path.Combine(TestPaths.RepoRoot,
                    "demos/louis-agent/web/louis-agent.code-review/wwwroot/brand/mark.svg")),
                "the mark is the app's own copy");
        });
    }

    /// <summary>The header's way into the chat leads to the app at /chat.</summary>
    [Test]
    public void TheHeader_LeadsToTheChat()
    {
        using var ctx = Context();

        var cut = ctx.Render<SiteLayout>();

        Assert.That(cut.FindAll(".site-actions a[href='/chat']"), Has.Count.EqualTo(1));
    }

    /// <summary>The guide sits in the frame's corner, folded, on every page.</summary>
    [Test]
    public void TheCorner_HoldsTheGuide_Folded()
    {
        using var ctx = Context();

        var cut = ctx.Render<SiteLayout>();

        Assert.That(cut.FindAll(".site-corner .chat-widget button.widget-button"), Has.Count.EqualTo(1));
        Assert.That(cut.Find(".site-corner button.widget-button").GetAttribute("aria-label"),
            Is.EqualTo("Ask the guide"));
        Assert.That(cut.FindAll(".widget-panel"), Is.Empty);
    }

    /// <summary>Where the chat runs elsewhere, the link is told so.</summary>
    [Test]
    public void AChatLink_GoesWhereItsTold()
    {
        using var ctx = Context();

        var cut = ctx.Render<ChatLink>(parameters => parameters
            .Add(link => link.Address, "https://agent.example/chat")
            .AddChildContent("Try it"));

        Assert.That(cut.Find("a").GetAttribute("href"), Is.EqualTo("https://agent.example/chat"));
    }
}
