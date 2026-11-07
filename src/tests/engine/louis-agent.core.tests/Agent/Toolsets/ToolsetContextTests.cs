using LouisAgent.Composition.Web.Fetching;
using LouisAgent.Composition.Web.Rendering;
using LouisAgent.Composition.Web.Search;
using LouisAgent.Core.Agent.Toolsets;
using LouisAgent.Core.Config;
using LouisAgent.Core.Sessions.Replay;
using LouisAgent.Core.Web;

namespace LouisAgent.Core.Tests.Agent.Toolsets;

/// <summary>A pack can tell an eval's trial from a real chat: only recording or replaying the web counts.</summary>
public class ToolsetContextTrialTests
{
    private static ToolsetContext Context() => new(new AgentOptions(), new WebParts(new TrialNoSearch()));

    /// <summary>What counts as a trial, by how the session's web is set.</summary>
    [TestCase(WebReplayMode.Record, true)]
    [TestCase(WebReplayMode.Replay, true)]
    [TestCase(WebReplayMode.Off, false)]
    public void ATrial_IsAWebThatIsRecordedOrReplayed(WebReplayMode web, bool expected)
    {
        using var _ = SessionReplay.Use(new SessionReplay(web, "trial-1", false));

        Assert.That(Context().InATrial, Is.EqualTo(expected));
    }

    /// <summary>A real chat has none of it.</summary>
    [Test]
    public void ARealChat_IsNoTrial() => Assert.That(Context().InATrial, Is.False);

    /// <summary>A search provider these tests never ask.</summary>
    private sealed class TrialNoSearch : IWebSearchProvider
    {
        /// <inheritdoc />
        public string Name => "None";

        /// <inheritdoc />
        public Task<IReadOnlyList<WebSearchResult>> SearchAsync(WebSearchQuery query,
            CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<WebSearchResult>>([]);
    }
}

/// <summary>
/// The context reads the web through the seam (CL33): what the parts given say, and the "not set up" answers for
/// what a host gave none of, so a tool says why instead of failing on a null.
/// </summary>
public class ToolsetContextWebTests
{
    private sealed class NoSearch : IWebSearchProvider
    {
        public string Name => "none";

        public Task<IReadOnlyList<WebSearchResult>> SearchAsync(WebSearchQuery query,
            CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<WebSearchResult>>([]);
    }

    [Test]
    public void ThePartsGiven_AreWhatTheToolsGet()
    {
        var search = new NoSearch();

        var context = new ToolsetContext(new AgentOptions(), new WebParts(search));

        Assert.Multiple(() =>
        {
            Assert.That(context.WebSearch, Is.SameAs(search));
            Assert.That(context.Browser, Is.Null);
        });
    }

    [Test]
    public void WithoutAWebLibrary_EveryPartSaysItIsNotSetUp()
    {
        var context = new ToolsetContext(new AgentOptions(), NoWebAccess.Instance);

        Assert.Multiple(() =>
        {
            Assert.That(context.Browser, Is.Null);
            Assert.ThrowsAsync<WebSearchException>(() => context.WebSearch.SearchAsync(new WebSearchQuery("x", 1)));
            Assert.ThrowsAsync<PageRenderException>(() =>
                context.Renderer.RenderAsync(new Uri("https://example.test/")));
            Assert.ThrowsAsync<BlockedAddressException>(() =>
                context.NewFetcher().EnsurePublicAsync(new Uri("https://example.test/")));
        });
    }
}
