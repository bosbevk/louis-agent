using System.Reflection;
using LouisAgent.Choices;
using LouisAgent.Identities.Guide;
using LouisAgent.Site.Components.Showroom;
using LouisAgent.Site.Pages;
using LouisAgent.Web.Components.Areas;
using LouisAgent.Web.Components.Areas.Focuses;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Site.Tests.Guide;

/// <summary>
/// F127.7, Louis: "when on normal pages it talk about selling the product... /demos it is a demo guide...
/// /framework our agent should be more technical". louis-agent.dev names each part's focus through its areas, and every
/// page takes one the guide has a skill for.
/// </summary>
public class SiteNamesItsFocusesTests
{
    /// <summary>The focuses the site's areas name.</summary>
    private static SiteFocuses Focuses() => new ServiceCollection().AddSiteAreas(LouisAgentDevAreas.Name)
        .BuildServiceProvider().GetRequiredService<SiteFocuses>();

    /// <summary>Each part where Louis put it: the demos and every demo, the framework, and selling elsewhere.</summary>
    [TestCase("/", GuideFocus.Selling)]
    [TestCase("/pricing", GuideFocus.Selling)]
    [TestCase("/home/what-it-does", GuideFocus.Selling)]
    [TestCase("/demos", GuideFocus.Demos)]
    [TestCase("/guesthouse-demo", GuideFocus.Demos)]
    [TestCase("/coding-agent", GuideFocus.Demos)]
    [TestCase("/framework/packs/louis-agent-pack-git", GuideFocus.Framework)]
    [TestCase("/csharp-ai-agent-framework", GuideFocus.Framework)]
    public void APart_TakesItsFocus(string path, GuideFocus focus) =>
        Assert.That(Focuses().Of(path), Is.EqualTo(focus.Wire()));

    /// <summary>Every page of the site, and every demo, takes a focus the guide has a skill for.</summary>
    [Test]
    public void EveryPage_TakesAFocusWithASkill()
    {
        var known = Enum.GetValues<GuideFocus>().Select(focus => focus.Wire()).ToHashSet();
        var pages = typeof(HomePage).Assembly.GetTypes()
            .SelectMany(type => type.GetCustomAttributes<RouteAttribute>()).Select(route => route.Template)
            .Concat(SiteDemos.All.All.Select(demo => demo.Address)).ToList();
        var focuses = Focuses();

        Assert.That(pages.Where(page => !known.Contains(focuses.Of(page) ?? "")), Is.Empty);
    }
}
