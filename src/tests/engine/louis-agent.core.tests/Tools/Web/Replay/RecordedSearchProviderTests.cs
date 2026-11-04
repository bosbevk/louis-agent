using System.Text.Json;
using LouisAgent.Core.Sessions.Replay;
using LouisAgent.Core.Tools.Web.Replay;
using LouisAgent.Core.Tools.Web.Search;
using static LouisAgent.Core.Tests.Tools.Web.FakeWeb;

namespace LouisAgent.Core.Tests.Tools.Web.Replay;

// F18 step 4b.2: a search, recorded and replayed by the session's SessionReplay. Written first: RecordedSearchProvider didn't exist on
// the red run, and ReplaySearchMiss had no text.
public class RecordedSearchProviderTests
{
    private static readonly WebSearchResult[] Eggs =
    [
        new("Large Eggs 6 Pack | Checkers", "https://www.checkers.co.za/eggs", "R34.99"),
        new("Eggs | PnP", "https://www.pnp.co.za/eggs", ""),
    ];

    private static IDisposable Using(WebReplayMode mode, string? of) =>
        SessionReplay.Use(new SessionReplay(mode, of, Model: false));

    private static async Task<string> SearchAsync(IWebSearchProvider provider, string query = "eggs", int max = 5,
        string site = "", string freshness = "") =>
        await WebToolsOn(_ => Html(""), provider).WebSearch(query, max, site, freshness);

    [Test]
    public async Task RecordingThenReplaying_WithAnInnerProviderThatThrows_GivesTheSameToolText()
    {
        using var folder = new ReplayFolder();
        var live = new CountingSearch(Eggs);
        var dead = new CountingSearch(throws: new InvalidOperationException("the network must not be used"));

        string recorded, replayed;
        using (Using(WebReplayMode.Record, "t"))
            recorded = await SearchAsync(new RecordedSearchProvider(live, folder.Store), freshness: "week");
        using (Using(WebReplayMode.Replay, "t"))
            replayed = await SearchAsync(new RecordedSearchProvider(dead, folder.Store), freshness: "week");

        Assert.That(recorded, Does.Contain("Large Eggs 6 Pack"));
        Assert.That(replayed, Is.EqualTo(recorded));
        Assert.That((live.Calls, dead.Calls), Is.EqualTo((1, 0)));
    }

    [TestCase(3, "", "week", "FakeSearch")]
    [TestCase(5, "checkers.co.za", "week", "FakeSearch")]
    [TestCase(5, "", "day", "FakeSearch")]
    [TestCase(5, "", "week", "Other")]
    public async Task AnotherMaxResultsSiteRecencyOrProvider_IsAMiss_WithTheNewText(int max, string site,
        string freshness, string provider)
    {
        using var folder = new ReplayFolder();
        using (Using(WebReplayMode.Record, "t"))
            await SearchAsync(new RecordedSearchProvider(new CountingSearch(Eggs), folder.Store), max: 5,
                freshness: "week");
        var inner = new CountingSearch(Eggs, name: provider);

        string text;
        using (Using(WebReplayMode.Replay, "t"))
            text = await SearchAsync(new RecordedSearchProvider(inner, folder.Store), max: max, site: site,
                freshness: freshness);

        Assert.That(text,
            Does.Contain("'eggs'").And.Contain("was not run in the recorded run").And
                .Contain("Treat what it would find as unknown"));
        Assert.That(inner.Calls, Is.Zero);
    }

    [Test]
    public async Task AWebSearchException_WhileRecording_ReplaysAsTheSameText()
    {
        using var folder = new ReplayFolder();
        var refused =
            new CountingSearch(
                throws: new WebSearchException("Google search quota exceeded (daily). Try again later."));

        string recorded, replayed;
        using (Using(WebReplayMode.Record, "t"))
            recorded = await SearchAsync(new RecordedSearchProvider(refused, folder.Store));
        var dead = new CountingSearch(Eggs);
        using (Using(WebReplayMode.Replay, "t"))
            replayed = await SearchAsync(new RecordedSearchProvider(dead, folder.Store));

        Assert.That(recorded, Does.Contain("quota exceeded"));
        Assert.That(replayed, Is.EqualTo(recorded));
        Assert.That(dead.Calls, Is.Zero);
    }

    [Test]
    public async Task AnHttpRequestException_WhileRecording_IsNotSaved_SoItsReplayIsAMiss()
    {
        using var folder = new ReplayFolder();
        var broken = new CountingSearch(throws: new HttpRequestException("connection reset"));

        using (Using(WebReplayMode.Record, "t")) await SearchAsync(new RecordedSearchProvider(broken, folder.Store));

        Assert.That(folder.Entries, Is.Empty);
        string text;
        using (Using(WebReplayMode.Replay, "t"))
            text = await SearchAsync(new RecordedSearchProvider(new CountingSearch(Eggs), folder.Store));
        Assert.That(text, Does.Contain("was not run in the recorded run"));
    }

    [Test]
    public async Task ACancel_IsRethrown_AndNeverRecorded()
    {
        using var folder = new ReplayFolder();
        var provider =
            new RecordedSearchProvider(new CountingSearch(throws: new OperationCanceledException()), folder.Store);

        using (Using(WebReplayMode.Record, "t"))
            Assert.ThrowsAsync<OperationCanceledException>(async () =>
                await provider.SearchAsync(new WebSearchQuery("eggs", 5)));

        Assert.That(folder.Entries, Is.Empty);
        await Task.CompletedTask;
    }

    [Test]
    public async Task WithNoTrial_ItPassesThrough_AndTheFolderStaysEmpty()
    {
        using var folder = new ReplayFolder();
        var inner = new CountingSearch(Eggs);
        var provider = new RecordedSearchProvider(inner, folder.Store);

        var none = await provider.SearchAsync(new WebSearchQuery("eggs", 5));
        IReadOnlyList<WebSearchResult> off;
        using (Using(WebReplayMode.Off, null)) off = await provider.SearchAsync(new WebSearchQuery("eggs", 5));

        Assert.That((none, off), Is.EqualTo((Eggs, Eggs)));
        Assert.That(inner.Calls, Is.EqualTo(2));
        Assert.That(folder.Entries, Is.Empty);
    }

    [Test]
    public void TheName_PassesThrough_AndAroundWithNoStoreIsTheInnerProvider()
    {
        using var folder = new ReplayFolder();
        var inner = new CountingSearch(name: "Tavily");

        Assert.That(new RecordedSearchProvider(inner, folder.Store).Name, Is.EqualTo("Tavily"));
        Assert.That(RecordedSearchProvider.Around(inner, null), Is.SameAs(inner));
        Assert.That(RecordedSearchProvider.Around(inner, folder.Store), Is.TypeOf<RecordedSearchProvider>());
    }

    [Test]
    public async Task TheIndexLineKey_HoldsTheRecencyAsAName()
    {
        using var folder = new ReplayFolder();

        using (Using(WebReplayMode.Record, "t"))
            await new RecordedSearchProvider(new CountingSearch(Eggs), folder.Store).SearchAsync(
                new WebSearchQuery("eggs", 5, "checkers.co.za", SearchRecency.Week));

        var line = JsonDocument.Parse(File.ReadAllLines(folder.Trials.Single()).Single()).RootElement;
        Assert.That(line.GetProperty("kind").GetString(), Is.EqualTo("search"));
        Assert.That(line.GetProperty("key").GetString(), Is.EqualTo(
            "{\"provider\":\"FakeSearch\",\"text\":\"eggs\",\"max_results\":5,\"site\":\"checkers.co.za\",\"recency\":\"week\"}"));
    }
}
