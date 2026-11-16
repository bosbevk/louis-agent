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
                    "Walk me through this page", "Which demo is like my business?", "What does it cost?",
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
                    "Walk me through this page", "Which demo is like my business?", "What does it cost?",
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
}
