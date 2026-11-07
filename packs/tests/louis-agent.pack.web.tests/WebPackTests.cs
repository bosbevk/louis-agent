using LouisAgent.Composition.Web.Search;
using LouisAgent.Core.Agent.Toolsets;
using LouisAgent.Core.Config;
using LouisAgent.Core.Sessions.Replay;
using LouisAgent.Core.Sessions.Replay.Store;

namespace LouisAgent.Packs.Web.Tests;

/// <summary>The pack's tools, built from what the engine hands over.</summary>
public class WebPackTests
{
    /// <summary>A trial that fetched nothing can't fetch now: the pack's fetcher has the engine's recordings around it.</summary>
    [Test]
    public async Task TheFetcher_ComesFromTheContext_WithItsStore()
    {
        string folder = Path.Combine(Path.GetTempPath(), "louis-replay-" + Guid.NewGuid().ToString("N"));
        var context = new ToolsetContext(new AgentOptions(), new NoSearch(), Replay: new ReplayStore(folder));
        var web = new WebPack().Toolsets.Single().CreateTools(context).OfType<WebTools>().Single();

        using (SessionReplay.Use(new SessionReplay(WebReplayMode.Replay, "t", Model: false)))
            Assert.That(await web.FetchUrl("https://no-such-host.invalid/page"),
                Does.Contain("was not read in the recorded run"));
    }

    /// <summary>A search that finds nothing, so the pack can be built without one.</summary>
    private sealed class NoSearch : IWebSearchProvider
    {
        public string Name => "none";

        public Task<IReadOnlyList<WebSearchResult>> SearchAsync(WebSearchQuery query,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<WebSearchResult>>([]);
    }
}
