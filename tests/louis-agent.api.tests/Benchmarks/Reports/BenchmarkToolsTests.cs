using LouisAgent.Testing;
using LouisAgent.Api.Benchmarks.Store;
using LouisAgent.Api.Benchmarks.Reports;
using System.Text.Json;
using static LouisAgent.Api.Benchmarks.Store.BenchmarkDocuments;
using static LouisAgent.Api.Benchmarks.Runs.BenchmarkProtocol;

namespace LouisAgent.Api.Tests.Benchmarks.Reports;

// F31 (Louis: a tools ring; step 11c's first half): what a turn's tools did, read from its stored stream events: the
// calls Puls and its specialist made, the ones that failed, and why, grouped by tool and reason. 11b's run had every
// WebSearch refused by Tavily's limit, and nothing on the screen said so.
public class BenchmarkToolsTests
{
    private static StreamEvent E(string type, object data) => new(type, JsonSerializer.SerializeToElement(data), null);

    private static StreamEvent Use(string id, string name) =>
        E("content_block_start", new { type = "content_block_start", content_block = new { type = "tool_use", id, name, input = new { } } });

    private static StreamEvent Result(string id, bool error, string content) =>
        E("content_block_start", new { type = "content_block_start", content_block = new { type = "tool_result", tool_use_id = id, content, is_error = error } });

    private static StreamEvent Inner(StreamEvent e) => E("sub_session", new { tool_use_id = "outer", agent = SampleIdentity.Alpha, @event = e.Type, data = e.Data });

    private const string Limit = "Error: Tavily's limit was reached (This request exceeds your plan's set usage limit.): the plan's searches.\nTry later.";

    [Test]
    public void PulssAndTheSpecialistsCalls_AreCounted_WithTheFailedOnes()
    {
        var tools = BenchmarkTools.Of(
        [
            Use("t1", "StartSubSession"),
            Inner(Use("s1", "WebSearch")), Inner(Result("s1", true, Limit)),
            Inner(Use("s2", "WebSearch")), Inner(Result("s2", true, Limit)),
            Inner(Use("s3", "FetchUrl")), Inner(Result("s3", false, "URL: https://…")),
            Result("t1", false, "the cook's answer"),
        ]);

        Assert.That((tools.Calls, tools.Failed), Is.EqualTo((4, 2)));
    }

    [Test]
    public void TheFailures_AreGroupedByToolAndTheFirstLineOfTheirReason()
    {
        var tools = BenchmarkTools.Of(
        [
            Use("a", "WebSearch"), Result("a", true, Limit),
            Use("b", "WebSearch"), Result("b", true, Limit),
            Use("c", "FetchUrl"), Result("c", true, "Error: 404 Not Found for https://x"),
        ]);

        Assert.That(tools.Reasons.Select(r => (r.Tool, r.Count)), Is.EqualTo(new[] { ("WebSearch", 2), ("FetchUrl", 1) }));
        Assert.That(tools.Reasons[0].Reason, Does.StartWith("Error: Tavily's limit was reached").And.Not.Contain("Try later"));
    }

    [Test]
    public void ATurnWithoutTools_HasNone() =>
        Assert.That(BenchmarkTools.Of([E("message_start", new { type = "message_start" })]), Is.EqualTo(BenchmarkTools.None));

    // Review of PR #55: opening a run and every trend read each turn's whole stream to count its tools. The recorder keeps
    // the counts on the turn; a turn recorded before that is counted from its events once, and the counts written back.

    private static Turn ATurn(BenchmarkTools.Summary? tools = null) =>
        new(new TurnKey(SampleMode.Beta, 1, "bobotie", 1), "s", "?", "!", SampleRole.Alpha, null, 1, 2, [], 10, null, tools);

    [Test]
    public async Task KeptCounts_AreUsedAsTheyAre()
    {
        var store = new InMemoryBenchmarkStore();
        var kept = new BenchmarkTools.Summary(4, 0, []);
        await store.SaveTurnAsync("r", ATurn(kept), [Use("u1", "WebSearch"), Result("u1", true, "Error: limit")]);

        var tools = await BenchmarkTools.OfTurnAsync(store, "r", ATurn(kept));

        Assert.That((tools.Calls, tools.Failed), Is.EqualTo((4, 0)), "the kept counts, not the events");
    }

    [Test]
    public async Task ATurnFromBefore_IsCountedOnce_AndItsCountsKept()
    {
        var store = new InMemoryBenchmarkStore();
        await store.SaveTurnAsync("r", ATurn(), [Use("u1", "WebSearch"), Result("u1", true, "Error: limit")]);

        var tools = await BenchmarkTools.OfTurnAsync(store, "r", ATurn());

        Assert.That((tools.Calls, tools.Failed), Is.EqualTo((1, 1)));
        var stored = (await store.TurnsAsync("r", new ConversationKey(SampleMode.Beta, 1, "bobotie"))).Single();
        Assert.That(stored.Tools?.Failed, Is.EqualTo(1), "written back, so the next read needs no events");
        Assert.That(await store.TurnEventsAsync("r", stored.Key), Has.Count.EqualTo(2), "its events kept for the replay");
    }
}
