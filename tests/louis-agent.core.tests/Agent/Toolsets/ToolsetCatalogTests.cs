using LouisAgent.Choices;
using LouisAgent.Testing;
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
    public void CoresCatalogue_IsExactlyTheCoreToolsetEnum_TheShopsAreAPack()
    {
        // CL15 step 4: the shops (Checkers' catalogue, the town geocoder) are packs/louis-agent.pack.shops; core names no shop. The enum is core's whole list (ADR 0043).
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
        Assert.That(ToolsetCatalog.All.Where(t => t.ParallelSafe).Select(t => t.Name),
            Is.EquivalentTo(new[] { CoreToolset.Web.Wire() }));
        Assert.That(ToolsetCatalog.RunsInParallel([ToolsetCatalog.Find(CoreToolset.Web.Wire())!, shops]), Is.True,
            "the cook");
        Assert.That(
            ToolsetCatalog.RunsInParallel([
                ToolsetCatalog.Find(CoreToolset.Specialists.Wire())!, ToolsetCatalog.Find(CoreToolset.Web.Wire())!
            ]), Is.False, "Puls");
        Assert.That(
            ToolsetCatalog.RunsInParallel([
                ToolsetCatalog.Find(CoreToolset.Files.Wire())!, ToolsetCatalog.Find(CoreToolset.Web.Wire())!
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
            PaymoApiKey = "key",
            DevopsApiKey = "key",
            DevopsOrganization = "org",
            DevopsProject = "project",
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

    [Test]
    public async Task TheWebToolset_FetchesThroughTheContextsStore()
    {
        // F18 step 4b.3: ToolsetCatalog hands ctx.Replay to the fetcher. In a replay of a trial that fetched nothing, an address that
        // cannot resolve is a recorded-run miss, not a lookup that fails.
        string folder = Path.Combine(Path.GetTempPath(), "louis-replay-" + Guid.NewGuid().ToString("N"));
        var context = new ToolsetContext(new AgentOptions(), new NoSearch(),
            Replay: new LouisAgent.Core.Sessions.Replay.Store.ReplayStore(folder));
        var web = (LouisAgent.Core.Tools.Web.WebTools)ToolsetCatalog
            .BuildTools([ToolsetCatalog.Find(CoreToolset.Web.Wire())!], context)
            .Single(t => t is LouisAgent.Core.Tools.Web.WebTools);

        using (LouisAgent.Core.Sessions.Replay.SessionReplay.Use(
                   new LouisAgent.Core.Sessions.Replay.SessionReplay(
                       LouisAgent.Core.Sessions.Replay.WebReplayMode.Replay, "t", Model: false)))
            Assert.That(await web.FetchUrl("https://no-such-host.invalid/page"),
                Does.Contain("was not read in the recorded run"));
    }

    private sealed class NoSearch : IWebSearchProvider
    {
        public string Name => "none";

        public Task<IReadOnlyList<WebSearchResult>> SearchAsync(WebSearchQuery query,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<WebSearchResult>>([]);
    }

    // CL15: the shops are a pack, so core's own optional toolset here is Paymo, which needs a key the same way.
    private static readonly ToolsetContext WithoutPaymo = new(new AgentOptions(), new NoSearch());
    private static readonly ToolsetContext WithPaymo = new(new AgentOptions { PaymoApiKey = "key" }, new NoSearch());

    private static string[] Resolved(IEnumerable<string> names, ToolsetContext context) =>
        ToolsetCatalog.Resolve(names, context).Select(t => t.Name).ToArray();

    [Test]
    public void AnOptionalToolsetThisInstanceHas_Loads() =>
        Assert.That(Resolved([CoreToolset.Web.Wire(), "paymo?"], WithPaymo),
            Is.EqualTo(new[] { CoreToolset.Web.Wire(), CoreToolset.Paymo.Wire() }));

    [Test]
    public void AnOptionalToolsetThisInstanceLacks_IsSkipped() =>
        Assert.That(Resolved([CoreToolset.Web.Wire(), "paymo?"], WithoutPaymo),
            Is.EqualTo(new[] { CoreToolset.Web.Wire() }));

    [Test]
    public void ARequiredToolsetThisInstanceLacks_IsStillRefused() =>
        Assert.That(() => Resolved([CoreToolset.Web.Wire(), CoreToolset.Paymo.Wire()], WithoutPaymo),
            Throws.TypeOf<ToolsetException>().With.Message.Contain("PAYMO_API_KEY"));

    [Test]
    public void AnUnknownName_IsRefused_EvenOptional() =>
        Assert.That(() => Resolved([CoreToolset.Web.Wire(), "shop?"], WithPaymo),
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
