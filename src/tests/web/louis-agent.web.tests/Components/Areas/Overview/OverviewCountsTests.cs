using LouisAgent.Web.Components.Areas;
using LouisAgent.Web.Components.Areas.Overview;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Tests.Components.Areas.Overview;

/// <summary>
/// F127.8: the API counts every tool its talks called and names no pack; a site's admin says which of them its overview
/// shows and what it calls each, louis-agent.dev's SendLead as leads, through its areas (ADR 0120).
/// </summary>
public class OverviewCountsTests
{
    /// <summary>A layout the admin is named with.</summary>
    private sealed class Layout : LayoutComponentBase;

    /// <summary>The counts the site's areas register.</summary>
    private static OverviewCounts CountsOf(Action<SiteAreasBuilder> name) =>
        new ServiceCollection().AddSiteAreas(name).BuildServiceProvider().GetRequiredService<OverviewCounts>();

    /// <summary>The tools the admin names, each with what the overview calls it, in the order named.</summary>
    [Test]
    public void TheAdmin_NamesTheToolsItCounts()
    {
        var counts = CountsOf(areas => areas.Admin(typeof(Layout),
            counts: count => count.Tool("SendLead", "leads").Tool("AskFor", "forms")));

        Assert.That(counts.Tools,
            Is.EqualTo(new[] { new CountedTool("SendLead", "leads"), new CountedTool("AskFor", "forms") }));
    }

    /// <summary>An admin that names none counts none, and the overview shows talks and cost alone.</summary>
    [Test]
    public void AnAdminThatNamesNone_CountsNone() =>
        Assert.That(CountsOf(areas => areas.Admin(typeof(Layout))).Tools, Is.Empty);
}
