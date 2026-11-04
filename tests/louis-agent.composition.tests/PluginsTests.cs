using LouisAgent.Packs.Recipes;
using LouisAgent.Packs.ShopPrices;
using LouisAgent.Packs.WhereToBuy;
using LouisAgent.Choices;
using LouisAgent.Core.Agent.Toolsets;
using LouisAgent.Core.Agent.Catalog;
using LouisAgent.Core.Config;
using LouisAgent.Core.Skills.Headers;
using LouisAgent.Core.Tools.Web.Search;
using LouisAgent.Identities.Puls;
using Microsoft.Extensions.FileProviders;

namespace LouisAgent.Composition.Tests;

/// <summary>
/// Checks that the composition root chooses: <c>IDENTITIES</c> and <c>PACKS</c>, parsed once as enums (ADR 0024),
/// become one catalogue; an unknown name is refused before anything is built; unset loads nothing, so the code-review
/// stack never carries Puls. The tests that touch the environment set a process variable and clear it again.
/// </summary>
public class PluginsTests
{
    /// <summary>The identities named are loaded, once each, and unset loads none.</summary>
    [Test]
    public void Identities_LoadsTheNamedOnes_AndUnsetNone()
    {
        AgentCatalog puls = Plugins.FromSettings(IdentityKind.Puls.Wire(), null);
        Assert.That(puls.Identities.Select(i => i.Name), Is.EqualTo(new[] { IdentityKind.Puls.Wire() }));
        Assert.That(puls.Packs.Select(p => p.Name),
            Is.EqualTo(new[]
            {
                WhereToBuyToolset.WhereToBuy.Wire(), ShopPricesToolset.ShopPrices.Wire(), RecipesToolset.Recipes.Wire()
            }),
            "Puls names the where-to-buy and shop-prices packs (F50.2) and the recipes pack (F35.2 step 1)");

        Assert.That(Plugins.FromSettings($"{IdentityKind.Puls.Wire()}, PULS", null).Identities, Has.Count.EqualTo(1),
            "a name twice, any case, is one identity");
        Assert.That(Plugins.FromSettings(null, null).Identities, Is.Empty);
        Assert.That(Plugins.FromSettings("", "").Identities, Is.Empty);
    }

    /// <summary>A name that is no identity or pack is refused, and the message lists the ones there are.</summary>
    [Test]
    public void AnUnknownIdentity_OrPack_IsRefused_NamingTheSupportedOnes()
    {
        var identity = Assert.Throws<AgentCatalogException>(() => Plugins.FromSettings("nope", null))!;
        Assert.That(identity.Message,
            Does.Contain("nope").And.Contain("IDENTITIES").And.Contain(IdentityKind.Puls.Wire()));

        var pack = Assert.Throws<AgentCatalogException>(() => Plugins.FromSettings(null, "nope"))!;
        Assert.That(pack.Message, Does.Contain("nope").And.Contain("PACKS"));
    }

    /// <summary>With no pack named, none is loaded.</summary>
    [Test]
    public void Packs_UnsetLoadsNone()
    {
        Assert.That(Plugins.FromSettings(null, null).Packs, Is.Empty);
        Assert.That(Plugins.FromSettings(null, " ").Packs, Is.Empty);
    }

    /// <summary>
    /// <c>PACKS=where-to-buy,shop-prices</c> loads both packs on their own; neither needs a setting or the browser, so
    /// both toolsets resolve on a host with nothing set.
    /// </summary>
    [Test]
    public void PacksWhereToBuyAndShopPrices_LoadOnTheirOwn_WithNothingSet()
    {
        AgentCatalog catalog = Plugins.FromSettings(null,
            $"{WhereToBuyToolset.WhereToBuy.Wire()},{ShopPricesToolset.ShopPrices.Wire()}");

        Assert.That(catalog.Identities, Is.Empty);
        Assert.That(catalog.Packs.Select(p => p.Name), Is.EqualTo(new[] { WhereToBuyToolset.WhereToBuy.Wire(), ShopPricesToolset.ShopPrices.Wire() }));
        var bare = new ToolsetContext(new AgentOptions(), new NoSearch());
        Assert.That(catalog.Resolve([WhereToBuyToolset.WhereToBuy.Wire(), ShopPricesToolset.ShopPrices.Wire()], bare).Select(t => t.Name),
            Is.EqualTo(new[] { WhereToBuyToolset.WhereToBuy.Wire(), ShopPricesToolset.ShopPrices.Wire() }));
    }

    /// <summary>
    /// <c>PACKS=recipes</c> loads the recipes pack on its own, reading its own <c>RECIPE_SOURCES</c>; the toolset
    /// needs the browser as well, and names <c>BROWSER_URL</c> when there is none.
    /// </summary>
    [Test]
    public void PacksRecipes_LoadsTheRecipesPack_WhenItsSettingsAreThere()
    {
        Assume.That(Environment.GetEnvironmentVariable("RECIPE_SOURCES"), Is.Null,
            "the process has no RECIPE_SOURCES of its own");
        Environment.SetEnvironmentVariable("RECIPE_SOURCES", "foodandhome");
        try
        {
            AgentCatalog catalog = Plugins.FromSettings(null, RecipesToolset.Recipes.Wire());
            Assert.That(catalog.Identities, Is.Empty);
            Assert.That(catalog.Packs.Select(p => p.Name), Is.EqualTo(new[] { RecipesToolset.Recipes.Wire() }));

            var withBrowser = new ToolsetContext(new AgentOptions { BrowserUrl = "http://puls-browser:9222" },
                new NoSearch());
            Assert.That(catalog.Resolve([RecipesToolset.Recipes.Wire()], withBrowser).Select(t => t.Name),
                Is.EqualTo(new[] { RecipesToolset.Recipes.Wire() }));

            var withoutBrowser = new ToolsetContext(new AgentOptions(), new NoSearch());
            var refused = Assert.Throws<ToolsetException>(() =>
                catalog.Resolve([RecipesToolset.Recipes.Wire()], withoutBrowser))!;
            Assert.That(refused.Message, Does.Contain(RecipesToolset.Recipes.Wire()).And.Contain("BROWSER_URL"));
        }
        finally
        {
            Environment.SetEnvironmentVariable("RECIPE_SOURCES", null);
        }
    }

    /// <summary>
    /// With Puls and its recipes sites set, the recipes pack loads once beside where to buy and the shop prices, and the
    /// toolsets the cook declares in its header resolve to them.
    /// </summary>
    [Test]
    public void PulsWithRecipeSources_LoadsTheRecipesPackOnce_AndTheCookResolvesIt()
    {
        Assume.That(Environment.GetEnvironmentVariable("RECIPE_SOURCES"), Is.Null,
            "the process has no RECIPE_SOURCES of its own");
        Environment.SetEnvironmentVariable("RECIPE_SOURCES", "*");
        try
        {
            // Named twice, once as a pack and once by the identity: still one instance of each.
            AgentCatalog catalog = Plugins.FromSettings(IdentityKind.Puls.Wire(), RecipesToolset.Recipes.Wire());
            Assert.That(catalog.Packs.Select(p => p.Name),
                Is.EquivalentTo(new[]
                {
                    WhereToBuyToolset.WhereToBuy.Wire(), ShopPricesToolset.ShopPrices.Wire(),
                    RecipesToolset.Recipes.Wire()
                }));

            // The cook's header names its three toolsets; with the browser given, the catalogue resolves them.
            var withBrowser = new ToolsetContext(new AgentOptions { BrowserUrl = "http://puls-browser:9222" },
                new NoSearch());
            Assert.That(catalog.Resolve(CookToolsets(catalog), withBrowser).Select(t => t.Name),
                Is.EqualTo(new[]
                {
                    RecipesToolset.Recipes.Wire(), WhereToBuyToolset.WhereToBuy.Wire(),
                    ShopPricesToolset.ShopPrices.Wire()
                }));
        }
        finally
        {
            Environment.SetEnvironmentVariable("RECIPE_SOURCES", null);
        }
    }

    /// <summary>
    /// The toolsets the cook's header declares, read from the prompts the catalogue holds with the same reader the
    /// engine uses. The composition root registers no prompts with the engine, so the catalogue's own folders are read.
    /// </summary>
    private static IReadOnlyList<string> CookToolsets(AgentCatalog catalog)
    {
        string path = $"specialists/{PulsRole.Cook.Wire()}.md";
        IFileInfo file = catalog.Prompts.Select(provider => provider.GetFileInfo(path)).First(info => info.Exists);
        using var reader = new StreamReader(file.CreateReadStream());
        return PromptHeaders.Split(path, reader.ReadToEnd()).Header!.Toolsets;
    }

    /// <summary>A search provider that finds nothing; these tests only need one to hand over.</summary>
    private sealed class NoSearch : IWebSearchProvider
    {
        /// <summary>What it calls itself.</summary>
        public string Name => "none";

        /// <summary>Answers every question with no results.</summary>
        public Task<IReadOnlyList<WebSearchResult>> SearchAsync(WebSearchQuery query,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<WebSearchResult>>([]);
    }

    /// <summary>
    /// The composition root loads <c>config/.env</c> itself before it reads the settings, because the hosts only load
    /// it later.
    /// </summary>
    [Test]
    public void FromEnvironment_ReadsConfigEnv_BeforeTheSettings()
    {
        // The hosts load config/.env inside AgentHost.Build today; the composition root runs before that, so it loads
        // the file itself first, or IDENTITIES set only there would be read too late.
        string root = Directory.CreateTempSubdirectory("plugins-").FullName;
        string before = Environment.CurrentDirectory;
        Assume.That(Environment.GetEnvironmentVariable("IDENTITIES"), Is.Null,
            "the process has no IDENTITIES of its own");
        try
        {
            Directory.CreateDirectory(Path.Combine(root, "config"));
            File.WriteAllText(Path.Combine(root, "config", ".env"), "IDENTITIES=puls\n");
            Environment.CurrentDirectory = root;

            Assert.That(Plugins.FromEnvironment().Identities.Select(i => i.Name),
                Is.EqualTo(new[] { IdentityKind.Puls.Wire() }));
        }
        finally
        {
            Environment.CurrentDirectory = before;
            Environment.SetEnvironmentVariable("IDENTITIES", null);
            Directory.Delete(root, recursive: true);
        }
    }
}
