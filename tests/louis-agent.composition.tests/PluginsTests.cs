using LouisAgent.Packs.Shops;
using LouisAgent.Choices;
using LouisAgent.Core.Agent.Toolsets;
using LouisAgent.Core.Agent.Catalog;
using LouisAgent.Composition;
using LouisAgent.Core.Config;
using LouisAgent.Core.Tools.Web.Search;

namespace LouisAgent.Composition.Tests;

// CL15 step 2: the composition root chooses. IDENTITIES and PACKS, parsed once as enums (ADR 0024), become one
// catalogue; an unknown name is refused before anything is built; unset loads nothing, so the code-review stack never
// carries Puls. Run in order where the environment is touched: the one test that reads config/.env sets a process
// variable, which it clears again.
public class PluginsTests
{
    [Test]
    public void Identities_LoadsTheNamedOnes_AndUnsetNone()
    {
        AgentCatalog puls = Plugins.FromSettings(IdentityKind.Puls.Wire(), null);
        Assert.That(puls.Identities.Select(i => i.Name), Is.EqualTo(new[] { IdentityKind.Puls.Wire() }));
        Assert.That(puls.Packs.Select(p => p.Name), Is.EqualTo(new[] { ShopsToolset.Shops.Wire() }), "Puls names the shops pack (step 4)");

        Assert.That(Plugins.FromSettings($"{IdentityKind.Puls.Wire()}, PULS", null).Identities, Has.Count.EqualTo(1), "a name twice, any case, is one identity");
        Assert.That(Plugins.FromSettings(null, null).Identities, Is.Empty);
        Assert.That(Plugins.FromSettings("", "").Identities, Is.Empty);
    }

    [Test]
    public void AnUnknownIdentity_OrPack_IsRefused_NamingTheSupportedOnes()
    {
        var identity = Assert.Throws<AgentCatalogException>(() => Plugins.FromSettings("nope", null))!;
        Assert.That(identity.Message, Does.Contain("nope").And.Contain("IDENTITIES").And.Contain(IdentityKind.Puls.Wire()));

        var pack = Assert.Throws<AgentCatalogException>(() => Plugins.FromSettings(null, "nope"))!;
        Assert.That(pack.Message, Does.Contain("nope").And.Contain("PACKS"));
    }

    [Test]
    public void Packs_UnsetLoadsNone()
    {
        Assert.That(Plugins.FromSettings(null, null).Packs, Is.Empty);
        Assert.That(Plugins.FromSettings(null, " ").Packs, Is.Empty);
    }

    [Test]
    public void PacksShops_GivesTheCodingAgentShops_WhenItsSettingsAreThere()
    {
        // CL15 step 4: the pack loads on its own, with no identity, and reads its own setting at the composition root.
        Assume.That(Environment.GetEnvironmentVariable("SHOP_CATALOGUES"), Is.Null, "the process has no SHOP_CATALOGUES of its own");
        Environment.SetEnvironmentVariable("SHOP_CATALOGUES", "checkers");
        try
        {
            AgentCatalog catalog = Plugins.FromSettings(null, ShopsToolset.Shops.Wire());
            Assert.That(catalog.Identities, Is.Empty);
            Assert.That(catalog.Packs.Select(p => p.Name), Is.EqualTo(new[] { ShopsToolset.Shops.Wire() }));

            var withBrowser = new ToolsetContext(new AgentOptions { BrowserUrl = "http://puls-browser:9222" }, new NoSearch());
            Assert.That(catalog.Resolve([ShopsToolset.Shops.Wire()], withBrowser).Select(t => t.Name), Is.EqualTo(new[] { ShopsToolset.Shops.Wire() }));

            var withoutBrowser = new ToolsetContext(new AgentOptions(), new NoSearch());
            var refused = Assert.Throws<ToolsetException>(() => catalog.Resolve([ShopsToolset.Shops.Wire()], withoutBrowser))!;
            Assert.That(refused.Message, Does.Contain(ShopsToolset.Shops.Wire()).And.Contain("BROWSER_URL"));
        }
        finally
        {
            Environment.SetEnvironmentVariable("SHOP_CATALOGUES", null);
        }
    }

    private sealed class NoSearch : IWebSearchProvider
    {
        public string Name => "none";
        public Task<IReadOnlyList<WebSearchResult>> SearchAsync(WebSearchQuery query, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<WebSearchResult>>([]);
    }

    [Test]
    public void FromEnvironment_ReadsConfigEnv_BeforeTheSettings()
    {
        // The hosts load config/.env inside AgentHost.Build today; the composition root runs before that, so it loads
        // the file itself first, or IDENTITIES set only there would be read too late.
        string root = Directory.CreateTempSubdirectory("plugins-").FullName;
        string before = Environment.CurrentDirectory;
        Assume.That(Environment.GetEnvironmentVariable("IDENTITIES"), Is.Null, "the process has no IDENTITIES of its own");
        try
        {
            Directory.CreateDirectory(Path.Combine(root, "config"));
            File.WriteAllText(Path.Combine(root, "config", ".env"), "IDENTITIES=puls\n");
            Environment.CurrentDirectory = root;

            Assert.That(Plugins.FromEnvironment().Identities.Select(i => i.Name), Is.EqualTo(new[] { IdentityKind.Puls.Wire() }));
        }
        finally
        {
            Environment.CurrentDirectory = before;
            Environment.SetEnvironmentVariable("IDENTITIES", null);
            Directory.Delete(root, recursive: true);
        }
    }
}
