using LouisAgent.Core.Agent.Catalog;
using LouisAgent.Identities.Framework;
using LouisAgent.Packs.Docs;
using LouisAgent.SiteFrameworkApi;

namespace LouisAgent.SiteFramework.Tests.Host;

/// <summary>
/// The framework's host runs the framework Orbi (F164.2 step 2) and nothing else: its one identity, on exactly the two
/// packs it names, the docs and the site, so a visitor reaches no shell, no files and no leads.
/// </summary>
public class FrameworkCatalogTests
{
    /// <summary>The framework Orbi is the only identity, and the packs are its two, no more.</summary>
    [Test]
    public void TheCatalogue_IsTheFrameworkOrbi_OnItsTwoPacks()
    {
        var packs = FrameworkCatalog.Packs(new DocsOptions { Root = "test-docs" });
        var orbi = new FrameworkIdentity();

        Assert.Multiple(() =>
        {
            Assert.That(FrameworkCatalog.Identities.Select(identity => identity.Name), Is.EqualTo(new[] { orbi.Name }));
            Assert.That(packs.Select(pack => pack.Name), Is.EquivalentTo(orbi.Packs), "every pack it names, no more");
        });
    }

    /// <summary>The catalogue builds as the host builds it, so the host starts.</summary>
    [Test]
    public void TheCatalogue_Builds_AsTheHostBuildsIt()
    {
        var packs = FrameworkCatalog.Packs(new DocsOptions { Root = "test-docs" });

        Assert.DoesNotThrow(() => _ = new AgentCatalog(FrameworkCatalog.Identities, packs));
    }

    /// <summary>The program runs the framework's catalogue in place of core alone.</summary>
    [Test]
    public void TheProgram_RunsTheFrameworkOrbi()
    {
        string program = File.ReadAllText(Path.Combine(TestPaths.RepoRoot,
            "src/site/LouisAgent.SiteFrameworkApi/Program.cs"));

        Assert.Multiple(() =>
        {
            Assert.That(program, Does.Contain("FrameworkCatalog.Create()"));
            Assert.That(program, Does.Not.Contain("AgentCatalog.CoreOnly"));
        });
    }
}
