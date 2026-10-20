using LouisAgent.Core.Agent.Toolsets;
using LouisAgent.Core.Agent.Catalog;
using LouisAgent.Core.Config;
using LouisAgent.Core.Tools.Web.Search;

namespace LouisAgent.Core.Tests.Agent.Toolsets;

// F33 step 4 (Louis: optional toolsets): Puls hands the cook `web` and `shops?`. A name ending in `?` loads when this
// instance has the toolset and is skipped when it doesn't, so cooking works on a deployment without SHOP_CATALOGUES
// (the sold endpoint: development and benchmarks first, ADR 0023). An unknown name stays an error, `?` or not: a typo
// is never skipped in silence.
public class ToolsetCatalogTests
{
    [Test]
    public void Core_HasNoShopsToolset_ItIsAPack()
    {
        // CL15 step 4: the shops (Checkers' catalogue, the town geocoder) are packs/louis-agent.pack.shops; core names no shop.
        Assert.That(ToolsetCatalog.Find("shops"), Is.Null);
        Assert.That(ToolsetCatalog.All.Select(t => t.Name), Has.None.EqualTo("shops"));
        Assert.That(AgentCatalog.CoreOnly.ToolNames, Has.None.EqualTo("FindProducts"));
    }

    // F34: only toolsets whose calls don't depend on each other's order run in parallel: reading the web and the shops
    // (a pack since CL15, so a parallel-safe definition stands in for it here). Files, shells, git and the specialists keep their order.
    [Test]
    public void OnlyTheWebAndTheShops_RunInParallel()
    {
        var shops = new ToolsetDefinition("shops", "a pack's", GuideFile: null, MissingSettings: _ => [], CreateTools: _ => [], ParallelSafe: true);
        Assert.That(ToolsetCatalog.All.Where(t => t.ParallelSafe).Select(t => t.Name), Is.EquivalentTo(new[] { "web" }));
        Assert.That(ToolsetCatalog.RunsInParallel([ToolsetCatalog.Find("web")!, shops]), Is.True, "the cook");
        Assert.That(ToolsetCatalog.RunsInParallel([ToolsetCatalog.Find("specialists")!, ToolsetCatalog.Find("web")!]), Is.False, "Puls");
        Assert.That(ToolsetCatalog.RunsInParallel([ToolsetCatalog.Find("files")!, ToolsetCatalog.Find("web")!]), Is.False);
        Assert.That(ToolsetCatalog.RunsInParallel([]), Is.False, "no toolsets: the coding agent's default tools");
    }

    // S (CL15): a toolset's ToolTypes is how its tool names are known without building it, so the objects CreateTools
    // builds must be exactly those classes, or the catalogue advertises tools the engine never registers (or the reverse).
    [Test]
    public void EveryToolset_BuildsExactlyTheClassesItAdvertises()
    {
        // The options every toolset needs to build (as the model-request golden builds them): keys, peers, minion models, a browser.
        var options = new AgentOptions
        {
            WorkspaceRoot = Path.GetTempPath(), SkillsDirectory = Path.GetTempPath(), RatesUrl = null,
            PaymoApiKey = "key", DevopsApiKey = "key", DevopsOrganization = "org", DevopsProject = "project",
            AgentPeers = new Dictionary<string, Uri> { ["coder"] = new("http://coder.test/") },
            MinionLlms = new Dictionary<string, IReadOnlyList<string>> { ["*"] = ["instance"] },
            BrowserUrl = "http://browser.test:9222",
        };
        var context = new ToolsetContext(options, new NoSearch());

        Assert.Multiple(() =>
        {
            foreach (var toolset in ToolsetCatalog.All.Where(t => !t.IsEngineProvided))
            {
                var built = toolset.CreateTools!(context).Select(o => o.GetType()).ToList();
                Assert.That(built, Is.EquivalentTo(toolset.ToolTypes ?? []), $"{toolset.Name}: CreateTools and ToolTypes disagree");
            }
        });
    }

    private sealed class NoSearch : IWebSearchProvider
    {
        public string Name => "none";
        public Task<IReadOnlyList<WebSearchResult>> SearchAsync(WebSearchQuery query, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<WebSearchResult>>([]);
    }

    // CL15: the shops are a pack, so core's own optional toolset here is Paymo, which needs a key the same way.
    private static readonly ToolsetContext WithoutPaymo = new(new AgentOptions(), new NoSearch());
    private static readonly ToolsetContext WithPaymo = new(new AgentOptions { PaymoApiKey = "key" }, new NoSearch());

    private static string[] Resolved(IEnumerable<string> names, ToolsetContext context) =>
        ToolsetCatalog.Resolve(names, context).Select(t => t.Name).ToArray();

    [Test]
    public void AnOptionalToolsetThisInstanceHas_Loads() =>
        Assert.That(Resolved(["web", "paymo?"], WithPaymo), Is.EqualTo(new[] { "web", "paymo" }));

    [Test]
    public void AnOptionalToolsetThisInstanceLacks_IsSkipped() =>
        Assert.That(Resolved(["web", "paymo?"], WithoutPaymo), Is.EqualTo(new[] { "web" }));

    [Test]
    public void ARequiredToolsetThisInstanceLacks_IsStillRefused() =>
        Assert.That(() => Resolved(["web", "paymo"], WithoutPaymo),
            Throws.TypeOf<ToolsetException>().With.Message.Contain("PAYMO_API_KEY"));

    [Test]
    public void AnUnknownName_IsRefused_EvenOptional() =>
        Assert.That(() => Resolved(["web", "shop?"], WithPaymo),
            Throws.TypeOf<ToolsetException>().With.Message.Contain("shop").And.Message.Contain("Valid names"));

    [TestCase(new[] { "shops?", " Shops? " }, new[] { "shops?" })]
    [TestCase(new[] { "shops?", "shops" }, new[] { "shops" })]
    [TestCase(new[] { "web", "shops?" }, new[] { "shops?", "web" })]
    public void Normalise_KeepsTheMark_AndARequiredNameWins(string[] names, string[] expected) =>
        Assert.That(ToolsetCatalog.Normalise(names), Is.EqualTo(expected));

    [TestCase("shops?", "shops")]
    [TestCase("web", "web")]
    public void TheNameOfAnOptionalToolset_IsWithoutItsMark(string name, string expected) =>
        Assert.That(ToolsetCatalog.NameOf(name), Is.EqualTo(expected));
}
