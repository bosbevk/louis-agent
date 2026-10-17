using System.Text.Json;
using LouisAgent.Api.Services.Benchmarks;
using static LouisAgent.Api.Services.Benchmarks.BenchmarkDocuments;

namespace LouisAgent.Api.Tests;

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

    private static StreamEvent Inner(StreamEvent e) => E("sub_session", new { tool_use_id = "outer", agent = "puls", @event = e.Type, data = e.Data });

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
}
