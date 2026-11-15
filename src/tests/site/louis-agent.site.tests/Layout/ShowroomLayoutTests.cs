using LouisAgent.Site.Layout;
using LouisAgent.Site.Tests.Pages;

namespace LouisAgent.Site.Tests.Layout;

/// <summary>
/// F134.3: in the showroom the guide is the bar at the foot of the page, with the site's suggestions as its chips;
/// on the rest of the site it keeps its corner.
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

    /// <summary>The rest of the site keeps the guide in its corner.</summary>
    [Test]
    public void TheSitesGuide_KeepsItsCorner()
    {
        using var ctx = SitePageContext.Create();

        var cut = ctx.Render<SiteLayout>();

        Assert.That(cut.FindAll(".chat-widget.is-bar"), Is.Empty);
    }
}
