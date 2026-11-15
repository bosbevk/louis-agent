using LouisAgent.Web.Testing.Sites;

namespace LouisAgent.Site.Tests.Layout;

/// <summary>
/// F151.1: louis-agent.dev's page opens in the look its areas give, the default theme they name (or none) and the
/// default style, written on its &lt;html&gt; before any script runs.
/// </summary>
public class IndexLookTests
{
    /// <summary>What the page writes is what the site's areas say.</summary>
    [Test]
    public void ThePage_WritesTheLookTheAreasGive() =>
        Assert.That(IndexLook.Of(SiteProject.Folder("louis-agent.site")),
            Is.EqualTo(IndexLook.For(LouisAgentDevAreas.Name)));
}
