using System.Net.ServerSentEvents;
using System.Text.Json;
using LouisAgent.Web.Shared;
using LouisAgent.Web.Shared.Services;

namespace LouisAgent.Api.Tests;

// F31 step 7a: one recorded Puls turn (benchmarks/fixtures/session-stream.sse) is the contract between the session
// stream's readers. The Python harness (benchmarks/puls_check/stream.py) is held to the counts in
// session-stream.expected.json by its unittest; this holds the web app's TurnStream to the same ones, read the way the
// app reads them (SseParser, then TurnStream.Apply). A change to the stream re-records the fixture, and both fail
// until both readers follow (CL11: one C# reader under the three).
public class SessionStreamFixtureTests
{
    private static string Fixtures { get; } =
        Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "..", "benchmarks", "fixtures"));

    private static async Task<Turn> Replay()
    {
        var turn = new Turn();
        await using var stream = File.OpenRead(Path.Combine(Fixtures, "session-stream.sse"));
        await foreach (var item in SseParser.Create(stream).EnumerateAsync())
            TurnStream.Apply(turn, new AgentStreamEvent(item.EventType, JsonDocument.Parse(item.Data).RootElement));
        return turn;
    }

    private static JsonElement Expected() =>
        JsonDocument.Parse(File.ReadAllText(Path.Combine(Fixtures, "session-stream.expected.json"))).RootElement;

    private static string[] Strings(JsonElement e, string name) => e.GetProperty(name).EnumerateArray().Select(v => v.GetString()!).ToArray();

    [Test]
    public async Task TurnStream_ReadsPulssCallsAndTheSpecialistsAsTheHarnessDoes()
    {
        var turn = await Replay();
        var expected = Expected();

        var tools = turn.Blocks.Where(b => b.Kind == BlockKind.Tool).ToList();
        Assert.That(tools.Select(b => b.ToolName), Is.EqualTo(Strings(expected, "puls_tools")));
        var delegation = tools.Single(b => b.ToolName == "StartSubSession");
        Assert.That(delegation.ToolInput, Does.Contain(expected.GetProperty("role").GetString()));
        var specialist = delegation.SubTurn!.Blocks.Where(b => b.Kind == BlockKind.Tool).Select(b => b.ToolName);
        Assert.That(specialist, Is.EqualTo(Strings(expected, "specialist_tools")));
    }

    [Test]
    public async Task TurnStream_ReadsTheAnswerAsTheHarnessDoes()
    {
        var turn = await Replay();
        var expected = Expected();

        string answer = string.Concat(turn.Blocks.Where(b => b.Kind == BlockKind.Text).Select(b => b.Text)).Trim();
        Assert.That(answer, Does.StartWith(expected.GetProperty("answer_starts").GetString()));
        Assert.That(answer.Length, Is.EqualTo(expected.GetProperty("answer_length").GetInt32()));
        Assert.That(turn.Error, Is.Null);
        Assert.That(turn.StopReason, Is.Not.Null);
    }
}
