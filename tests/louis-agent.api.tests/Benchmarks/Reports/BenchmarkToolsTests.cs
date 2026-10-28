using LouisAgent.Testing;
using LouisAgent.Api.Benchmarks.Store;
using LouisAgent.Api.Benchmarks.Reports;
using System.Text.Json;
using static LouisAgent.Api.Benchmarks.Store.BenchmarkDocuments;
using static LouisAgent.Api.Benchmarks.Runs.BenchmarkProtocol;

namespace LouisAgent.Api.Tests.Benchmarks.Reports;

/// <summary>
/// What a turn's tools did, read from its stored stream events: the calls Puls and its specialist made, which ones
/// failed, and why, grouped by tool and reason. Once a run had every web search refused by the search provider's
/// limit, and nothing on the screen said so.
/// </summary>
public class BenchmarkToolsTests
{
    /// <summary>A stored stream event of this type, with its data as JSON.</summary>
    private static StreamEvent E(string type, object data) => new(type, JsonSerializer.SerializeToElement(data), null);

    /// <summary>The event where the model starts calling a tool.</summary>
    private static StreamEvent Use(string id, string name) =>
        E("content_block_start",
            new
            {
                type = "content_block_start",
                content_block = new
                {
                    type = "tool_use",
                    id,
                    name,
                    input = new { }
                }
            });

    /// <summary>The event that brings a tool call's result, failed or not.</summary>
    private static StreamEvent Result(string id, bool error, string content) =>
        E("content_block_start",
            new
            {
                type = "content_block_start",
                content_block = new
                {
                    type = "tool_result",
                    tool_use_id = id,
                    content,
                    is_error = error
                }
            });

    /// <summary>Wraps an event as one that happened inside the specialist's sub-session.</summary>
    private static StreamEvent Inner(StreamEvent e) => E("sub_session",
        new
        {
            tool_use_id = "outer",
            agent = SampleIdentity.Alpha,
            @event = e.Type,
            data = e.Data
        });

    /// <summary>The refusal a search provider sends when its plan's limit is reached, over two lines.</summary>
    private const string Limit =
        "Error: Tavily's limit was reached (This request exceeds your plan's set usage limit.): the plan's searches.\nTry later.";

    /// <summary>The calls Puls and the specialist made are counted together, with how many of them failed.</summary>
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

    /// <summary>Failures are grouped by tool and the first line of their reason.</summary>
    [Test]
    public void TheFailures_AreGroupedByToolAndTheFirstLineOfTheirReason()
    {
        var tools = BenchmarkTools.Of(
        [
            Use("a", "WebSearch"), Result("a", true, Limit),
            Use("b", "WebSearch"), Result("b", true, Limit),
            Use("c", "FetchUrl"), Result("c", true, "Error: 404 Not Found for https://x"),
        ]);

        Assert.That(tools.Reasons.Select(r => (r.Tool, r.Count)),
            Is.EqualTo(new[] { ("WebSearch", 2), ("FetchUrl", 1) }));
        Assert.That(tools.Reasons[0].Reason,
            Does.StartWith("Error: Tavily's limit was reached").And.Not.Contain("Try later"));
    }

    /// <summary>A turn that called no tools has none to show.</summary>
    [Test]
    public void ATurnWithoutTools_HasNone() =>
        Assert.That(BenchmarkTools.Of([E("message_start", new { type = "message_start" })]),
            Is.EqualTo(BenchmarkTools.None));

    // Counting from the stream on every read was slow (a run page and every trend read each turn's whole stream), so
    // the recorder keeps the counts on the turn. An older turn is counted from its events once, and the counts saved.

    /// <summary>The cook's first turn, with or without tool counts already kept on it.</summary>
    private static Turn ATurn(BenchmarkTools.Summary? tools = null) =>
        new(new TurnKey(SampleMode.Beta, 1, "bobotie", 1), "s", "?", "!", SampleRole.Alpha, null, 1, 2, [], 10, null,
            tools);

    /// <summary>Counts already kept on a turn are used as they are, not counted again from its events.</summary>
    [Test]
    public async Task KeptCounts_AreUsedAsTheyAre()
    {
        var store = new InMemoryBenchmarkStore();
        var kept = new BenchmarkTools.Summary(4, 0, []);
        await store.SaveTurnAsync("r", ATurn(kept), [Use("u1", "WebSearch"), Result("u1", true, "Error: limit")]);

        var tools = await BenchmarkTools.OfTurnAsync(store, "r", ATurn(kept));

        Assert.That((tools.Calls, tools.Failed), Is.EqualTo((4, 0)), "the kept counts, not the events");
    }

    /// <summary>
    /// An older turn is counted from its events once, and the counts are saved on it while its events stay for the
    /// replay.
    /// </summary>
    [Test]
    public async Task ATurnFromBefore_IsCountedOnce_AndItsCountsKept()
    {
        var store = new InMemoryBenchmarkStore();
        await store.SaveTurnAsync("r", ATurn(), [Use("u1", "WebSearch"), Result("u1", true, "Error: limit")]);

        var tools = await BenchmarkTools.OfTurnAsync(store, "r", ATurn());

        Assert.That((tools.Calls, tools.Failed), Is.EqualTo((1, 1)));
        var stored = (await store.TurnsAsync("r", new ConversationKey(SampleMode.Beta, 1, "bobotie"))).Single();
        Assert.That(stored.Tools?.Failed, Is.EqualTo(1), "written back, so the next read needs no events");
        Assert.That(await store.TurnEventsAsync("r", stored.Key), Has.Count.EqualTo(2),
            "its events kept for the replay");
    }
}
