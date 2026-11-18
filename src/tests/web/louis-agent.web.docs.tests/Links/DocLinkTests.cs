using LouisAgent.Web.Docs.Links;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Docs.Tests.Links;

/// <summary>
/// F164.1: a section ends with links to its docs by their paths, each leading where the site's docs' address says,
/// opened beside the page.
/// </summary>
public class DocLinkTests
{
    /// <summary>A doc link leads to its doc at the site's docs' address, in a tab of its own, with its words.</summary>
    [Test]
    public void ADocLink_LeadsToItsDoc()
    {
        using var ctx = new BunitContext();
        ctx.Services.AddSingleton(DocsAddress.GitHub("bosbevk/louis-agent", "main"));

        var cut = ctx.Render<DocLink>(link => link
            .Add(p => p.Path, "src/docs/adr/0126-a-name-says-what-it-is-by-its-kind.md")
            .AddChildContent("The naming rule"));

        var a = cut.Find("a.doc-link");
        Assert.Multiple(() =>
        {
            Assert.That(a.GetAttribute("href"), Is.EqualTo(
                "https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0126-a-name-says-what-it-is-by-its-kind.md"));
            Assert.That(a.GetAttribute("target"), Is.EqualTo("_blank"));
            Assert.That(a.GetAttribute("rel"), Is.EqualTo("noopener"));
            Assert.That(a.TextContent.Trim(), Is.EqualTo("The naming rule"));
        });
    }

    /// <summary>A site that links to a doc but names no docs' address is told how to name one, not left guessing.</summary>
    [Test]
    public void ADocLink_WithNoDocsAddress_SaysHowToNameOne()
    {
        using var ctx = new BunitContext();

        var refused = Assert.Throws<InvalidOperationException>(() =>
            ctx.Render<DocLink>(link => link.Add(p => p.Path, "src/docs/README.md").AddChildContent("The docs")));

        Assert.That(refused!.Message, Does.Contain(".Docs("));
    }

    /// <summary>A section's doc links stand together under one name a screen reader says.</summary>
    [Test]
    public void DocLinks_StandTogether()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<DocLinks>(links => links.AddChildContent("<a>one</a>"));

        Assert.That(cut.Find("nav.doc-links").GetAttribute("aria-label"), Is.EqualTo("The docs"));
    }
}
