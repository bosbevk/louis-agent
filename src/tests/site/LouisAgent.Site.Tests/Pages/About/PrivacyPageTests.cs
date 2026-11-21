using LouisAgent.Site.Pages.About;
using LouisAgent.Web.Components.Site.Meta;

namespace LouisAgent.Site.Tests.Pages.About;

/// <summary>
/// The privacy page says who a visitor talks to by its name (F161, Orbi): the agent is Orbi, at the foot of the
/// page, not "an AI agent in its corner", in its opening words and in what a search shows of it.
/// </summary>
public class PrivacyPageTests
{
    /// <summary>The opening and the description both name Orbi, and neither speaks of a corner.</summary>
    [Test]
    public void ThePrivacyPage_NamesOrbi()
    {
        using var ctx = SitePageContext.Create();

        var cut = ctx.Render<PrivacyPage>();

        string opening = cut.Find("section#top").TextContent;
        string description = cut.FindComponent<PageHead>().Instance.Description;
        Assert.Multiple(() =>
        {
            Assert.That(opening, Does.Contain("Orbi").And.Not.Contain("corner"));
            Assert.That(description, Does.Contain("Orbi").And.Not.Contain("corner"));
        });
    }
}
