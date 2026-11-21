using LouisAgent.Web.Docs.Links;

namespace LouisAgent.Web.Docs.Tests.Links;

/// <summary>
/// F166.4: a section can end with a link to another repository, such as a product built on the framework elsewhere,
/// beside its doc links and looking the same (Louis's pick A of finding 4).
/// </summary>
public class RepositoryLinkTests
{
    /// <summary>A repository link leads to the address it was given, in a tab of its own, with its words.</summary>
    [Test]
    public void ARepositoryLink_LeadsToItsAddress_LikeADocLink()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<RepositoryLink>(link => link
            .Add(p => p.Href, "https://github.com/bosbevk/puls")
            .AddChildContent("Puls"));

        var a = cut.Find("a.doc-link");
        Assert.Multiple(() =>
        {
            Assert.That(a.GetAttribute("href"), Is.EqualTo("https://github.com/bosbevk/puls"));
            Assert.That(a.GetAttribute("target"), Is.EqualTo("_blank"));
            Assert.That(a.GetAttribute("rel"), Is.EqualTo("noopener"));
            Assert.That(a.TextContent.Trim(), Is.EqualTo("Puls"));
        });
    }
}
