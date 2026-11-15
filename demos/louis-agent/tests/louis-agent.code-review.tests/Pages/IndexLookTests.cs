using LouisAgent.Web.Testing.Sites;

namespace LouisAgent.CodeReview.Tests.Pages;

/// <summary>
/// F151.1: the code-review app's page opens in the look its areas give, the default theme they name (or none) and
/// the default style, written on its &lt;html&gt; before any script runs.
/// </summary>
public class IndexLookTests
{
    /// <summary>What the page writes is what the site's areas say.</summary>
    [Test]
    public void ThePage_WritesTheLookTheAreasGive() =>
        Assert.That(IndexLook.Of(SiteProject.Folder("louis-agent.code-review")),
            Is.EqualTo(IndexLook.For(CodeReviewAreas.Name)));
}
