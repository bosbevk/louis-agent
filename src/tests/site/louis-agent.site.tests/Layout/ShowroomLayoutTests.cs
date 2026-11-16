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
                    "Walk me through this page", "Which demo is like my business?", "What does it cost?"
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
                    "Walk me through this page", "Which demo is like my business?", "What does it cost?"
                }));
        });
    }
}
