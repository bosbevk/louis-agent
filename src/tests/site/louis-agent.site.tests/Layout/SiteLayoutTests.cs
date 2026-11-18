using LouisAgent.Site.Components.Landing;
using LouisAgent.Site.Content;
using LouisAgent.Web.Storage;
using LouisAgent.Web.Text;
using LouisAgent.Web.Usage;
using Microsoft.AspNetCore.Components;
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
    /// The menu marks the page the visitor is on (FX162, Louis: "must stay selected if I'm on that page"): its link,
    /// and only its, is the active one, in the menu's row or in a group's list, so the rail can draw it pressed.
    /// </summary>
    [TestCase("/contact", "Contact")]
    [TestCase("/demos", "All demos")]
    public void TheMenu_MarksThePageYouAreOn(string page, string link)
    {
        using var ctx = Context();
        ctx.Services.GetRequiredService<NavigationManager>().NavigateTo(page);

        var cut = ctx.Render<SiteLayout>();
        var active = cut.FindAll(".site-nav a.active").Select(a => a.TextContent.Trim()).ToList();

        Assert.That(active, Is.EqualTo(new[] { link }));
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
    /// The settings hold only the look switches, behind the three-line button (FX163). The CV mark that stood there
    /// went once the landing showed who builds it (Louis, 2026-11-18: "we can remove that now"): the face band leads
    /// to the CV instead.
    /// </summary>
    [Test]
    public void TheSettings_HoldOnlyTheLooks()
    {
        using var ctx = Context();

        var cut = ctx.Render<SiteLayout>();

        Assert.Multiple(() =>
        {
            Assert.That(cut.FindAll(".site-settings-button"), Has.Count.EqualTo(1), "the three-line button stays");
            Assert.That(cut.FindAll(".site-settings a"), Is.Empty, "no CV mark, and no other link");
        });
    }

    /// <summary>The menu leads to each page, from every page.</summary>
    [TestCase("/demos")]
    [TestCase("/contact")]
    [TestCase("/")]
    [TestCase("/framework/theme")]
    public void TheMenu_LeadsToThePage(string address)
    {
        using var ctx = Context();

        var cut = ctx.Render<SiteLayout>();

        Assert.That(cut.FindAll($".site-nav a[href='{address}']"), Has.Count.EqualTo(1));
    }

    /// <summary>
    /// The menu leads with what the site sells (F128.1, Louis's pick 5A): Demos, Theme and Contact; no Pricing (Louis:
    /// "remove pricing"), no What we build (FX171, Louis: "remove this page and everything of it"), and no framework
    /// group, since the framework's pages left for their own site (F162.1). Home comes first (Louis, 2026-11-18, on the
    /// privacy page: "there is no clear way from here back to main layout"), since the name alone isn't seen as a way
    /// home.
    /// </summary>
    [Test]
    public void TheMenu_LeadsWithWhatWeSell()
    {
        using var ctx = Context();

        var cut = ctx.Render<SiteLayout>();

        var items = cut.Find(".site-nav").Children
            .Select(item => (item.QuerySelector(".site-drop-label") ?? item).TextContent.Trim());
        Assert.That(items, Is.EqualTo(new[] { "Home", "Demos", "Theme", "Contact" }));
    }

    /// <summary>
    /// The Demos are back beside Contact once the demos are fixed (FX195, Louis: "when you are done with the demos,
    /// enabled the menu item again"), and nothing in the menu is hidden.
    /// </summary>
    [Test]
    public void TheDemosMenu_IsShown()
    {
        using var ctx = Context();

        var cut = ctx.Render<SiteLayout>();

        var shown = cut.Find(".site-nav").Children.Where(item => !item.HasAttribute("hidden"))
            .Select(item => (item.QuerySelector(".site-drop-label") ?? item).TextContent.Trim());
        Assert.That(shown, Is.EqualTo(new[] { "Home", "Demos", "Theme", "Contact" }));
    }

    /// <summary>No page of the site answers at /what-we-build any more (FX171).</summary>
    [Test]
    public void NoPage_AnswersAtWhatWeBuild()
    {
        var routes = typeof(SiteLayout).Assembly.GetTypes()
            .SelectMany(type => type.GetCustomAttributes(typeof(RouteAttribute), false).Cast<RouteAttribute>())
            .Select(route => route.Template);

        Assert.That(routes, Does.Not.Contain("/what-we-build"));
    }

    /// <summary>
    /// The header ends with the settings' three-line button and no ask button (Louis: "we should have a 3 line menu
    /// there which will open a sticky menu then we put those buttons there. ask button can go").
    /// </summary>
    [Test]
    public void TheHeader_EndsWithTheSettingsButton()
    {
        using var ctx = Context();

        var cut = ctx.Render<SiteLayout>();

        Assert.Multiple(() =>
        {
            Assert.That(cut.Find(".site-header").LastElementChild!.ClassList, Does.Contain("site-settings-button"));
            Assert.That(cut.FindAll(".site-header a.btn"), Is.Empty);
        });
    }

    /// <summary>
    /// The slogan sits under the brand (FX79, Louis: "this is our slogan under our brand"), and says what the site
    /// is to a business owner when the name doesn't (the brand research of 2026-11-18).
    /// </summary>
    [Test]
    public void TheSlogan_SitsUnderTheBrand()
    {
        using var ctx = Context();

        var cut = ctx.Render<SiteLayout>();

        Assert.That(cut.Find(".site-brand .site-tagline").TextContent, Is.EqualTo("The website that answers."));
    }

    /// <summary>
    /// Every page's footer leads to the privacy page (F145.1), so a visitor can find what happens to what they type
    /// from anywhere on the site.
    /// </summary>
    [Test]
    public void TheFooter_LeadsToThePrivacyPage()
    {
        using var ctx = Context();

        var cut = ctx.Render<SiteLayout>();

        var link = cut.FindAll(".site-footer a.footer-link").Single(found => found.GetAttribute("href") == "/privacy");
        Assert.That(link.TextContent.Trim(), Is.EqualTo("Privacy"));
    }

    /// <summary>The footer carries the same slogan as the header, so the site says one thing.</summary>
    [Test]
    public void TheFooter_CarriesTheSlogan()
    {
        using var ctx = Context();

        var cut = ctx.Render<SiteLayout>();

        Assert.That(cut.Find(".site-footer").TextContent, Does.Contain("The website that answers."));
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
            Assert.That(demos.QuerySelectorAll("a").Select(a => a.GetAttribute("href")),
                Is.EqualTo(new[]
                {
                    "/demos", "/guesthouse-demo", "/restaurant-demo", "/law-practice-demo"
                }));
            Assert.That(cut.Find(".site-header").TextContent, Does.Not.Contain("Watch it work"));
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
}
