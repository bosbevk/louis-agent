using LouisAgent.Core.Agent.Catalog;
using LouisAgent.Identities.Guide;
using LouisAgent.Packs.Docs;
using LouisAgent.Packs.Leads;
using LouisAgent.SiteApi;

namespace LouisAgent.Site.Tests.Host;

/// <summary>
/// The framework's site runs on a host of its own (F124), and the public site reaches only what that host names: the
/// guide in the corner and its three packs, never the coding agent's shell, files or git.
/// </summary>
public class SiteCatalogTests
{
    /// <summary>
    /// The guide is the first identity, so it is the default, and the packs are exactly its three: the showroom's
    /// businesses (F128.2) use only the leads pack, one of them, and open only with the site's key.
    /// </summary>
    [Test]
    public void TheCatalogue_IsTheGuideFirst_OnItsThreePacks()
    {
        var packs = SiteCatalog.Packs(new DocsOptions { Root = "test-docs" },
            new LeadsOptions { To = "a@example.test" });
        var guide = new GuideIdentity();

        Assert.Multiple(() =>
        {
            Assert.That(SiteCatalog.Identities[0].Name, Is.EqualTo(guide.Name));
            Assert.That(packs.Select(pack => pack.Name), Is.EquivalentTo(guide.Packs), "every pack it names, no more");
            Assert.That(SiteCatalog.Identities.SelectMany(identity => identity.Packs).Distinct(),
                Is.SubsetOf(guide.Packs), "no identity on the site reaches a pack the guide doesn't load");
        });
    }

    /// <summary>
    /// The catalogue builds as the host builds it, so the host starts (F128.2: the showroom's three voices once shared
    /// one prompts folder, each prompt was registered three times, and the host stopped at start-up).
    /// </summary>
    [Test]
    public void TheCatalogue_Builds_AsTheHostBuildsIt()
    {
        var packs = SiteCatalog.Packs(new DocsOptions { Root = "test-docs" },
            new LeadsOptions { To = "a@example.test" });

        Assert.DoesNotThrow(() => _ = new AgentCatalog(SiteCatalog.Identities, packs));
    }

    /// <summary>
    /// The program serves the site as HTML and the API the guide's chat uses, and opts into nothing else: no
    /// accounts, no eval studio.
    /// </summary>
    [Test]
    public void TheProgram_ServesTheSite_AndNothingElse()
    {
        string program = File.ReadAllText(Path.Combine(TestPaths.RepoRoot,
            "src/site/louis-agent.site-api/Program.cs"));

        Assert.Multiple(() =>
        {
            Assert.That(program, Does.Contain("MapLouisAgentSite(typeof(LouisAgent.Site.App).Assembly"));
            Assert.That(program, Does.Contain("MapLouisAgentApi(host)").And.Contain("SiteCatalog.Create()"));
            Assert.That(program, Does.Not.Contain("Accounts").And.Not.Contain("Evals"));
        });
    }
}
