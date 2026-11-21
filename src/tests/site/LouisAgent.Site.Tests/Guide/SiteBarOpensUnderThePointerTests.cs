using LouisAgent.Web.Components.Areas;
using LouisAgent.Web.Components.Widget.Bar.Opening;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Site.Tests.Guide;

/// <summary>
/// FX172: louis-agent.dev's guide bar opens under the pointer (Louis: "then we should use hover", "site picks it"),
/// named through its areas.
/// </summary>
public class SiteBarOpensUnderThePointerTests
{
    /// <summary>The site names hover.</summary>
    [Test]
    public void TheSitesBar_OpensUnderThePointer() =>
        Assert.That(new ServiceCollection().AddSiteAreas(LouisAgentDevAreas.Name).BuildServiceProvider()
            .GetService<BarOpening>()?.Opens, Is.EqualTo(BarOpens.Hover));
}
