using System.Runtime.CompilerServices;
using System.Text.Json;
using LouisAgent.Evals.Api;
using LouisAgent.Evals.Protocol;
using LouisAgent.Evals.Protocol.Events.Conversations;
using LouisAgent.Evals.Running;
using LouisAgent.SessionStream;
using Microsoft.Extensions.Logging.Abstractions;
using LouisAgent.Evals.Runner.Evals.Stub;

namespace LouisAgent.Evals.Runner.Tests.Evals.Stub;

/// <summary>
/// The stub eval's run is its golden file, line for line: every event of protocol version 2, with only the clock's
/// readings (the time and the seconds) and the build's commit masked, since those change from run to run.
/// </summary>
public class StubEvalTests
{
    /// <summary>The golden file, as the build copied it.</summary>
    private static string[] ExpectedLines =>
        File.ReadAllLines(Path.Combine(AppContext.BaseDirectory, "Fixtures", "stub-run.txt"))
            .Where(line => line.Length > 0).ToArray();

    /// <summary>The golden file in the source tree, which the update switch writes.</summary>
    private static string SourceGoldenFile([CallerFilePath] string here = "") =>
        Path.Combine(Path.GetDirectoryName(here)!, "..", "..", "Fixtures", "stub-run.txt");

    /// <summary>
    /// A run is the golden file's lines, line for line. With <c>LOUIS_UPDATE_GOLDEN=1</c> the file is written again
    /// from the run, for its diff to be reviewed.
    /// </summary>
    [Test]
    public async Task ARunIsTheStubsExpectedLinesLineForLine()
    {
        string[] mine = await RunAsync(repeat: 1);

        // The switch every golden in the repo has: write the file again, then say so instead of comparing.
        if (Environment.GetEnvironmentVariable("LOUIS_UPDATE_GOLDEN") == "1")
        {
            File.WriteAllLines(SourceGoldenFile(), mine);
            Assert.Pass($"Wrote {SourceGoldenFile()}; review its diff before committing.");
        }

        string[] expected = ExpectedLines;
        Assert.That(mine, Has.Length.EqualTo(expected.Length));
        for (int i = 0; i < expected.Length; i++)
            Assert.That(Canonical(mine[i]), Is.EqualTo(Canonical(expected[i])), $"line {i + 1}");
    }

    /// <summary>Two modes, two repeats, two conversations: eight sessions, the repeat in each id.</summary>
    [Test]
    public async Task EachRepeatOpensItsOwnSessions()
    {
        string[] lines = await RunAsync(repeat: 2);

        var sessions = lines.Select(EvalProtocolJson.Read).OfType<QuestionEvent>()
            .Select(question => question.Session)
            .Distinct()
            .ToList();
        Assert.That(sessions, Has.Count.EqualTo(8));
        Assert.That(sessions, Does.Contain("sess_stub_stub-direct_2_bobotie"));
    }

    /// <summary>
    /// Every session is tagged with the run's name, so the ledger's rows find their run: {run}:{mode}:{repeat}.
    /// </summary>
    [Test]
    public async Task EverySessionIsTaggedWithTheRunsName()
    {
        var client = new RecordingClient(new StubSessionClient());
        var runner = new EvalRunner(client, new ProtocolWriter(new StringWriter(), TimeProvider.System),
            TimeProvider.System,
            NullLogger<EvalRunner>.Instance);

        await runner.RunAsync(new StubEval(), new EvalRun("portal-run-7", 1), CancellationToken.None);

        Assert.That(client.Tags, Has.Count.EqualTo(4));
        Assert.That(client.Tags, Does.Contain("portal-run-7:stub-delegating:1"));
        Assert.That(client.Tags.All(tag => tag.StartsWith("portal-run-7:", StringComparison.Ordinal)), Is.True);
    }

    /// <summary>A session client that records each open's tag and passes everything through.</summary>
    private sealed class RecordingClient(ISessionClient inner) : ISessionClient
    {
        /// <summary>The tags the sessions were opened with, in order.</summary>
        public List<string> Tags { get; } = [];

        /// <summary>The inner client's toolsets.</summary>
        public Task<IReadOnlyList<string>> ToolsetsAsync(CancellationToken cancellationToken) =>
            inner.ToolsetsAsync(cancellationToken);

        /// <summary>Notes the tag, and opens through the inner client.</summary>
        public async Task<string> OpenAsync(SessionRequest request, CancellationToken cancellationToken)
        {
            Tags.Add(request.RunTag);
            return await inner.OpenAsync(request, cancellationToken);
        }

        /// <summary>Sends through the inner client.</summary>
        public IAsyncEnumerable<SessionEvent> SendAsync(string session, string message,
            CancellationToken cancellationToken) =>
            inner.SendAsync(session, message, cancellationToken);

        /// <summary>Closes through the inner client.</summary>
        public Task CloseAsync(string session, CancellationToken cancellationToken) =>
            inner.CloseAsync(session, cancellationToken);
    }

    /// <summary>Runs the stub eval with that many repeats, and hands back the lines it wrote.</summary>
    private static async Task<string[]> RunAsync(int repeat)
    {
        var sink = new StringWriter();
        var runner = new EvalRunner(new StubSessionClient(), new ProtocolWriter(sink, TimeProvider.System),
            TimeProvider.System,
            NullLogger<EvalRunner>.Instance);

        await runner.RunAsync(new StubEval(), new EvalRun("run_test", repeat), CancellationToken.None);

        return sink.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries);
    }

    /// <summary>One line as its keys in order and its values, the clock's readings and the commit masked.</summary>
    private static string Canonical(string line) => Canonical(JsonDocument.Parse(line).RootElement, null);

    /// <summary>A value as text: an object's members in order, the masked ones as a mark.</summary>
    /// <remarks>A switch expression on the value's kind, one arm per case, read like a table.</remarks>
    private static string Canonical(JsonElement element, string? name) => element.ValueKind switch
    {
        JsonValueKind.Object => "{" + string.Join(",", element.EnumerateObject()
            .Select(p => JsonSerializer.Serialize(p.Name) + ":" + Canonical(p.Value, p.Name))) + "}",
        JsonValueKind.Array => "[" + string.Join(",", element.EnumerateArray().Select(v => Canonical(v, null))) + "]",
        _ when name is "t" or "seconds" or "commit" => "<masked>",
        JsonValueKind.String => JsonSerializer.Serialize(element.GetString()),
        _ => element.GetRawText(),
    };
}
