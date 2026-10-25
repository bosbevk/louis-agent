using System.Text.Json;
using LouisAgent.Evals.Api;
using LouisAgent.Evals.Protocol;
using LouisAgent.Evals.Running;
using LouisAgent.SessionStream;
using Microsoft.Extensions.Logging.Abstractions;
using LouisAgent.Evals.Runner.Evals.Stub;

namespace LouisAgent.Evals.Runner.Tests.Evals.Stub;

// The stub (F18 step 4.1), the C# port of benchmarks/stub/run_check.py: its protocol lines are the Python's, line for line, with
// the clock readings masked (t, and the seconds, which both write as measured time), the conversation's grades and outcome left
// out (step 4.5a: the runner grades every trial, and the Python stub never did) and the header left out: the Python's first
// line is the version alone, and the runner's carries the eval, its graders and its models, which the store reads. Written first:
// the stub threw NotImplementedException on the red run.
public class StubEvalTests
{
    private static string[] PythonLines =>
        File.ReadAllLines(Path.Combine(AppContext.BaseDirectory, "Fixtures", "python-stub.txt")).Where(l => l.StartsWith('@')).ToArray();

    [Test]
    public async Task ARunIsThePythonStubsLineForLine()
    {
        string[] mine = (await RunAsync(repeat: 1)).Skip(1).ToArray();
        string[] python = PythonLines.Skip(1).ToArray();

        Assert.That(mine, Has.Length.EqualTo(python.Length));
        for (int i = 0; i < python.Length; i++)
            Assert.That(Canonical(mine[i]), Is.EqualTo(Canonical(python[i])), $"line {i + 2}");
    }

    [Test]
    public async Task EachRepeatOpensItsOwnSessions()
    {
        // Two modes, two repeats, two conversations: eight sessions, the repeat in each id as the Python's are.
        string[] lines = await RunAsync(repeat: 2);

        var sessions = lines.Select(Parse)
            .Where(e => e.GetProperty("event").GetString() == "question")
            .Select(e => e.GetProperty("session").GetString())
            .Distinct()
            .ToList();
        Assert.That(sessions, Has.Count.EqualTo(8));
        Assert.That(sessions, Does.Contain("sess_stub_puls-only_2_bobotie"));
    }

    [Test]
    public async Task EverySessionIsTaggedWithTheRunsName()
    {
        // The ledger's rows find their run by this tag (F18 step 4.1 review): {run}:{mode}:{repeat}, the run being BENCHMARK_RUN.
        var client = new RecordingClient(new StubSessionClient());
        var runner = new EvalRunner(client, new ProtocolWriter(new StringWriter(), TimeProvider.System), TimeProvider.System,
            NullLogger<EvalRunner>.Instance);

        await runner.RunAsync(new StubEval(), new EvalRun("portal-run-7", 1), CancellationToken.None);

        Assert.That(client.Tags, Has.Count.EqualTo(4));
        Assert.That(client.Tags, Does.Contain("portal-run-7:puls-cook:1"));
        Assert.That(client.Tags.All(tag => tag.StartsWith("portal-run-7:", StringComparison.Ordinal)), Is.True);
    }

    /// <summary>A session client that records each open's tag and passes everything through.</summary>
    private sealed class RecordingClient(ISessionClient inner) : ISessionClient
    {
        public List<string> Tags { get; } = [];

        public Task<IReadOnlyList<string>> ToolsetsAsync(CancellationToken cancellationToken) => inner.ToolsetsAsync(cancellationToken);

        public async Task<string> OpenAsync(SessionRequest request, CancellationToken cancellationToken)
        {
            Tags.Add(request.RunTag);
            return await inner.OpenAsync(request, cancellationToken);
        }

        public IAsyncEnumerable<SessionEvent> SendAsync(string session, string message, CancellationToken cancellationToken) =>
            inner.SendAsync(session, message, cancellationToken);

        public Task CloseAsync(string session, CancellationToken cancellationToken) => inner.CloseAsync(session, cancellationToken);
    }

    private static async Task<string[]> RunAsync(int repeat)
    {
        var sink = new StringWriter();
        var runner = new EvalRunner(new StubSessionClient(), new ProtocolWriter(sink, TimeProvider.System), TimeProvider.System,
            NullLogger<EvalRunner>.Instance);

        await runner.RunAsync(new StubEval(), new EvalRun("run_test", repeat), CancellationToken.None);

        return sink.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries);
    }

    private static JsonElement Parse(string line) => JsonDocument.Parse(line.TrimStart('@')).RootElement;

    /// <summary>One line as its keys, in order, and its values: the clock readings masked, the grades and the pass rates (the harness adds them; the Python has none) left out, everything else compared as written.</summary>
    private static string Canonical(string line) => Canonical(Parse(line), null);

    private static string Canonical(JsonElement element, string? name) => element.ValueKind switch
    {
        JsonValueKind.Object => "{" + string.Join(",", element.EnumerateObject().Where(p => p.Name is not ("grades" or "outcome" or "pass_at_k" or "pass_hat_k" or "k" or "infrastructure" or "retried"))
            .Select(p => JsonSerializer.Serialize(p.Name) + ":" + Canonical(p.Value, p.Name))) + "}",
        JsonValueKind.Array => "[" + string.Join(",", element.EnumerateArray().Select(v => Canonical(v, null))) + "]",
        _ when name is "t" or "seconds" => "<clock>",
        JsonValueKind.String => JsonSerializer.Serialize(element.GetString()),
        _ => element.GetRawText(),
    };
}
