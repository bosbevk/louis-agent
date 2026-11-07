using LouisAgent.Composition.Web.Search;
using LouisAgent.Choices;
using LouisAgent.Testing;
using LouisAgent.Core.Agent.Toolsets;
using LouisAgent.Core.Agent.Catalog;
using LouisAgent.Core.Config;
using LouisAgent.Core.Web.Search;

namespace LouisAgent.Core.Tests.Agent.Toolsets;

// F33 step 4 (Louis: optional toolsets): Puls hands the cook `web` and `shops?`. A name ending in `?` loads when this
// instance has the toolset and is skipped when it doesn't, so cooking worked on a deployment without the shops
// pack's settings (F33; the pack is gone since F50.2, the rule stays). An unknown name stays an error, `?` or not: a typo
// is never skipped in silence.
public class ToolsetCatalogTests
{
    [Test]
    public void CoresCatalogue_IsExactlyTheCoreToolsetEnum_TheShopsAreAPack()
    {
        // CL15 step 4: the shops (Checkers' catalogue, the town geocoder) became a pack; F50.2: packs/tools/louis-agent.pack.where-to-buy and shop-prices; core names no shop. The enum is core's whole list (ADR 0043).
        Assert.That(ToolsetCatalog.All.Select(t => t.Name),
            Is.EquivalentTo(Enum.GetValues<CoreToolset>().Select(t => t.Wire())));
        Assert.That(AgentCatalog.CoreOnly.ToolNames, Has.None.EqualTo("FindProducts"));
    }

    // F34: only toolsets whose calls don't depend on each other's order run in parallel: reading the web and the shops
    // (a pack since CL15, so a parallel-safe definition stands in for it here). Files, shells, git and the specialists keep their order.
    [Test]
    public void OnlyTheWebAndTheShops_RunInParallel()
    {
        var shops = new ToolsetDefinition(SampleToolset.Beta, "a pack's", GuideFile: null, MissingSettings: _ => [],
            CreateTools: _ => [], ParallelSafe: true);
        Assert.That(ToolsetCatalog.All.Where(t => t.ParallelSafe), Is.Empty, "core's own hand work on");
        Assert.That(ToolsetCatalog.RunsInParallel([TestTools.Catalog.Toolsets.Single(t => t.Name == TestToolset.Web.Wire()), shops]), Is.True,
            "the cook");
        Assert.That(
            ToolsetCatalog.RunsInParallel([
                ToolsetCatalog.Find(CoreToolset.Specialists.Wire())!, TestTools.Catalog.Toolsets.Single(t => t.Name == TestToolset.Web.Wire())
            ]), Is.False, "Puls");
        Assert.That(
            ToolsetCatalog.RunsInParallel([
                ToolsetCatalog.Find(CoreToolset.Skills.Wire())!, TestTools.Catalog.Toolsets.Single(t => t.Name == TestToolset.Web.Wire())
            ]), Is.False);
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
            WorkspaceRoot = Path.GetTempPath(),
            SkillsDirectory = Path.GetTempPath(),
            RatesUrl = null,
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
                Assert.That(built, Is.EquivalentTo(toolset.ToolTypes ?? []),
                    $"{toolset.Name}: CreateTools and ToolTypes disagree");
            }
        });
    }

    private sealed class NoSearch : IWebSearchProvider
    {
        public string Name => "none";

        public Task<IReadOnlyList<WebSearchResult>> SearchAsync(WebSearchQuery query,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<WebSearchResult>>([]);
    }

    // CL15: the shops are a pack, so core's own optional toolset here is specialists, which needs a peer the same way.
    private static readonly ToolsetContext WithoutPeers = new(new AgentOptions(), new NoSearch());
    private static readonly ToolsetContext WithPeers = new(new AgentOptions { AgentPeers = new Dictionary<string, Uri> { ["coder"] = new("http://coder.test/") } }, new NoSearch());

    private static string[] Resolved(IEnumerable<string> names, ToolsetContext context) =>
        TestTools.Catalog.Resolve(names, context).Select(t => t.Name).ToArray();

    [Test]
    public void AnOptionalToolsetThisInstanceHas_Loads() =>
        Assert.That(Resolved([TestToolset.Web.Wire(), "specialists?"], WithPeers),
            Is.EqualTo(new[] { TestToolset.Web.Wire(), CoreToolset.Specialists.Wire() }));

    [Test]
    public void AnOptionalToolsetThisInstanceLacks_IsSkipped() =>
        Assert.That(Resolved([TestToolset.Web.Wire(), "specialists?"], WithoutPeers),
            Is.EqualTo(new[] { TestToolset.Web.Wire() }));

    [Test]
    public void ARequiredToolsetThisInstanceLacks_IsStillRefused() =>
        Assert.That(() => Resolved([TestToolset.Web.Wire(), CoreToolset.Specialists.Wire()], WithoutPeers),
            Throws.TypeOf<ToolsetException>().With.Message.Contain("AGENT_PEERS"));

    [Test]
    public void AnUnknownName_IsRefused_EvenOptional() =>
        Assert.That(() => Resolved([TestToolset.Web.Wire(), "shop?"], WithPeers),
            Throws.TypeOf<ToolsetException>().With.Message.Contain("shop").And.Message.Contain("Valid names"));

    [TestCase(new[] { SampleToolset.Beta + "?", " " + SampleToolset.Beta + "? " }, new[] { SampleToolset.Beta + "?" })]
    [TestCase(new[] { SampleToolset.Beta + "?", SampleToolset.Beta }, new[] { SampleToolset.Beta })]
    [TestCase(new[] { SampleToolset.Alpha, SampleToolset.Beta + "?" },
        new[] { SampleToolset.Alpha, SampleToolset.Beta + "?" })]
    public void Normalise_KeepsTheMark_AndARequiredNameWins(string[] names, string[] expected) =>
        Assert.That(ToolsetCatalog.Normalise(names), Is.EqualTo(expected));

    [TestCase(SampleToolset.Beta + "?", SampleToolset.Beta)]
    [TestCase(SampleToolset.Alpha, SampleToolset.Alpha)]
    public void TheNameOfAnOptionalToolset_IsWithoutItsMark(string name, string expected) =>
        Assert.That(ToolsetCatalog.NameOf(name), Is.EqualTo(expected));
}
