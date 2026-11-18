using LouisAgent.Site.Layout;
using LouisAgent.Site.Tests.Pages;

namespace LouisAgent.Site.Tests.Layout;

/// <summary>
/// F134.3: in the showroom the guide is the bar at the foot of the page, with the site's suggestions as its chips,
/// and since F151.6 the rest of the site has the same bar.
/// </summary>
public class ShowroomLayoutTests
{
    /// <summary>
    /// The showroom's guide is a bar, offering the site's suggestions while it waits. The same chips stand on the
    /// start and on every demo's page, so the first asks for a walk through "this page", which is true on each.
    /// </summary>
    [Test]
    public void TheShowroomsGuide_IsTheBar_WithTheSitesSuggestions()
    {
        using var ctx = SitePageContext.Create();

        var cut = ctx.Render<ShowroomLayout>();

        Assert.Multiple(() =>
        {
            Assert.That(cut.Find(".showroom-corner .chat-widget").ClassList, Does.Contain("is-bar"));
            Assert.That(cut.FindAll(".agent-chips button").Select(chip => chip.TextContent),
                Is.EqualTo(new[]
                {
                    "Walk me through this page", "Which demo is like my business?", "How do I get one?",
                    "Tell me about the framework"
                }));
        });
    }

    /// <summary>
    /// The rest of the site has the same bar with the same chips (F151.6), so the guide looks and asks alike on
    /// every page.
    /// </summary>
    [Test]
    public void TheSitesGuide_IsTheSameBar()
    {
        using var ctx = SitePageContext.Create();

        var cut = ctx.Render<SiteLayout>();

        Assert.Multiple(() =>
        {
            Assert.That(cut.Find(".site-corner .chat-widget").ClassList, Does.Contain("is-bar"));
            Assert.That(cut.FindAll(".agent-chips button").Select(chip => chip.TextContent),
                Is.EqualTo(new[]
                {
                    "Walk me through this page", "Which demo is like my business?", "How do I get one?",
                    "Tell me about the framework"
                }));
        });
    }

    /// <summary>
    /// FX173: Louis's LinkedIn stands in the screen's bottom-right corner, and the brand beside the bar is gone for now
    /// (Louis: "put the linked in in the bottom right corner and remove the branding for now").
    /// </summary>
    [Test]
    public void TheLinkedIn_IsInTheCorner_AndNoBrandStandsBesideTheBar()
    {
        using var ctx = SitePageContext.Create();

        var cut = ctx.Render<SiteLayout>();

        Assert.Multiple(() =>
        {
            Assert.That(cut.Find(".site-corner a.corner-link").GetAttribute("href"),
                Is.EqualTo(LouisAgent.Site.Content.SiteFacts.LinkedIn));
            Assert.That(cut.FindAll(".agent-side"), Is.Empty);
        });
    }

    /// <summary>
    /// FX175: the site's pages show no section index for now, switched off and kept (Louis: "we must just hide this
    /// disabled, not use it, not delete it").
    /// </summary>
    [Test]
    public void TheSite_ShowsNoSectionIndex()
    {
        using var ctx = SitePageContext.Create();

        var cut = ctx.Render<SiteLayout>();

        Assert.That(cut.FindComponent<LouisAgent.Web.Components.Site.SiteShell>().Instance.ShowIndex, Is.False);
    }

    /// <summary>FX177: the site's pages show no way back up for now, switched off and kept (Louis: "disable").</summary>
    [Test]
    public void TheSite_ShowsNoWayUp()
    {
        using var ctx = SitePageContext.Create();

        var cut = ctx.Render<SiteLayout>();

        Assert.That(cut.FindAll(".site-up"), Is.Empty);
    }

    /// <summary>
    /// FX192: what the showroom's start draws of the site's own, the page, its frame, its guide and its three
    /// windows, is configured framework controls and holds no HTML tag of the site's, as ADR 0125 has it (Louis:
    /// "this must be all frame work controls, only the site in the actual demo is not framework"). The offer is the framework's
    /// button, and the thing each window's agent showed is a card the framework draws.
    /// </summary>
    [TestCase("Pages/Agents/DemosPage.razor")]
    [TestCase("Layout/ShowroomLayout.razor")]
    [TestCase("Components/Guide/ExpertChat.razor")]
    [TestCase("Components/Showroom/Windows/SeemeeuWindow.razor")]
    [TestCase("Components/Showroom/Windows/WheatfieldWindow.razor")]
    [TestCase("Components/Showroom/Windows/KoringlandWindow.razor")]
    public void TheShowroomsOwnMarkup_IsFrameworkControlsOnly(string file)
    {
        string markup = File.ReadAllText(Path.Combine(TestPaths.RepoRoot, "src/site/louis-agent.site", file));

        // Every tag is a component's, named with a capital, and never a plain element's such as a link or an image.
        var tags = markup.Split('<').Skip(1).Select(tag => tag.TrimStart('/')).Where(tag => tag.Length > 0);
        var plain = tags.Where(tag => char.IsLower(tag[0])).Select(tag => tag.Split(' ', '>', '/')[0]);

        Assert.That(plain, Is.Empty);
    }

    /// <summary>
    /// FX170: the header says the site is at alpha-1, and a press says what that means (Louis: "flag in the header
    /// version: alpha disclosure", "running version alpha-1 may experience inconsistencies").
    /// </summary>
    [Test]
    public void TheHeader_FlagsAlphaOne()
    {
        using var ctx = SitePageContext.Create();

        var cut = ctx.Render<SiteLayout>();

        Assert.Multiple(() =>
        {
            Assert.That(cut.Find(".site-header .site-stage button.stage-flag").TextContent.Trim(), Is.EqualTo("alpha-1"));
            Assert.That(cut.Find(".site-header .stage-note").TextContent, Does.Contain("inconsistencies"));
        });
    }
}
