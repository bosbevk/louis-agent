using LouisAgent.Api.Contracts.Admin.Overview;
using LouisAgent.Web.Components.Admin.Overview;
using LouisAgent.Web.Components.Areas;
using LouisAgent.Web.Components.Areas.Overview;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Tests.Components.Areas.Overview;

/// <summary>
/// F127.8: the API counts every tool its talks called and names no pack; a site's overview says which of them it shows
/// and what it calls each, louis-agent.dev's SendLead as leads. F144: the overview is an area of its own inside the
/// admin's frame, with its link in the sidebar and its card on the home.
/// </summary>
public class OverviewCountsTests
{
    /// <summary>A layout the admin is named with.</summary>
    private sealed class Layout : LayoutComponentBase;

    /// <summary>What the site's areas register.</summary>
    private static IServiceProvider Named(Action<SiteAreasBuilder> name) =>
        new ServiceCollection().AddSiteAreas(name).BuildServiceProvider();

    /// <summary>The tools the overview names, each with what it calls it, in the order named.</summary>
    [Test]
    public void TheOverview_NamesTheToolsItCounts()
    {
        var counts = Named(areas => areas.Admin(typeof(Layout))
                .Overview(count => count.Tool("SendLead", "leads").Tool("AskFor", "forms")))
            .GetRequiredService<OverviewCounts>();

        Assert.That(counts.Tools,
            Is.EqualTo(new[] { new CountedTool("SendLead", "leads"), new CountedTool("AskFor", "forms") }));
    }

    /// <summary>An overview that names none counts none, and shows talks and cost alone.</summary>
    [Test]
    public void AnOverviewThatNamesNone_CountsNone() =>
        Assert.That(Named(areas => areas.Admin(typeof(Layout)).Overview()).GetRequiredService<OverviewCounts>().Tools,
            Is.Empty);

    /// <summary>The overview plugs into the admin: its link in the sidebar, its card on the home.</summary>
    [Test]
    public void TheOverview_IsAnAdminArea_WithItsLinkAndItsCard()
    {
        var areas = Named(named => named.Admin(typeof(Layout)).Overview()).GetRequiredService<SiteAreas>();

        Assert.Multiple(() =>
        {
            Assert.That(areas.Names, Is.EqualTo(new[] { "admin", "overview" }));
            Assert.That(areas.AdminLinks, Is.EqualTo(new[] { typeof(OverviewLinks) }));
            Assert.That(areas.AdminCards, Is.EqualTo(new[] { typeof(OverviewCard) }));
        });
    }

    /// <summary>The admin alone has no overview: nothing counted, no link, no card.</summary>
    [Test]
    public void TheAdminAlone_HasNoOverview()
    {
        var services = Named(areas => areas.Admin(typeof(Layout)));

        Assert.Multiple(() =>
        {
            Assert.That(services.GetService<OverviewCounts>(), Is.Null);
            Assert.That(services.GetRequiredService<SiteAreas>().AdminCards, Is.Empty);
        });
    }

    /// <summary>A period's talks for each counted tool, by the site's name, and none for a tool not called.</summary>
    [Test]
    public void InAPeriod_EachCountedToolHasItsTalks()
    {
        var counts = new OverviewCounts([new CountedTool("SendLead", "leads"), new CountedTool("AskFor", "forms")]);
        var today = new OverviewPeriod(3, null, [new OverviewBar("Offer", 3), new OverviewBar("SendLead", 1)]);

        Assert.That(counts.In(today), Is.EqualTo(new[] { new OverviewBar("leads", 1), new OverviewBar("forms", 0) }));
    }
}
