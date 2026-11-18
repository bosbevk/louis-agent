using LouisAgent.Web.Components.Areas;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Docs.Tests;

/// <summary>
/// F162.1: a page links to a doc by its path in the repo, and the site says once where those paths lead: GitHub
/// today, the docs site later, by that one setting.
/// </summary>
public class DocsAddressTests
{
    /// <summary>On GitHub, a path is the file on the branch, whether it was written with a leading slash or not.</summary>
    [TestCase("src/docs/adr/0126-a-name-says-what-it-is-by-its-kind.md")]
    [TestCase("/src/docs/adr/0126-a-name-says-what-it-is-by-its-kind.md")]
    public void OnGitHub_APathIsTheFileOnTheBranch(string path)
    {
        var docs = DocsAddress.GitHub("bosbevk/louis-agent", "main");

        Assert.That(docs.For(path), Is.EqualTo(
            "https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0126-a-name-says-what-it-is-by-its-kind.md"));
    }

    /// <summary>On a docs site of its own, a path is under the site's address.</summary>
    [Test]
    public void OnADocsSite_APathIsUnderItsAddress()
    {
        var docs = new DocsAddress("https://docs.louis-agent.dev");

        Assert.That(docs.For("src/docs/README.md"), Is.EqualTo("https://docs.louis-agent.dev/src/docs/README.md"));
    }

    /// <summary>A site names its docs' address once, as an area, and every doc link reads it.</summary>
    [Test]
    public void ASite_NamesItsDocsOnce()
    {
        var docs = DocsAddress.GitHub("bosbevk/louis-agent", "main");
        var services = new ServiceCollection();

        services.AddSiteAreas(areas => areas.Docs(docs));

        var areas = (SiteAreas)services.Single(d => d.ServiceType == typeof(SiteAreas)).ImplementationInstance!;
        Assert.Multiple(() =>
        {
            Assert.That(areas.Names, Is.EqualTo(new[] { "docs" }));
            Assert.That(services.BuildServiceProvider().GetRequiredService<DocsAddress>(), Is.SameAs(docs));
        });
    }
}
