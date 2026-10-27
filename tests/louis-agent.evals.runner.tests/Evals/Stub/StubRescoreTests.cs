using System.Text.Json;
using LouisAgent.Evals.Api;
using LouisAgent.Evals.Protocol;
using LouisAgent.Evals.Running;
using LouisAgent.SessionStream;
using Microsoft.Extensions.Logging.Abstractions;
using LouisAgent.Evals.Runner.Evals.Stub;

namespace LouisAgent.Evals.Runner.Tests.Evals.Stub;

// F18 step 5b: the stub's run, re-scored. The stub's graders are the same today, so each conversation keeps its grades and each mode its rates, and the
// session client the runner is given is never called (the stub's own, which would answer, is not used).
public class StubRescoreTests
{
    [Test]
    public async Task ARescoredStubRunHasTheSameGradesAndRatesAsTheRun()
    {
        var recorded = new StringWriter();
        await new EvalRunner(new StubSessionClient(), new ProtocolWriter(recorded, TimeProvider.System),
                TimeProvider.System, NullLogger<EvalRunner>.Instance)
            .RunAsync(new StubEval(), new EvalRun("run_old", 2), CancellationToken.None);
        string[] stored = Lines(recorded);
        var rescored = new StringWriter();
        var silent = new SilentClient();

        await new EvalRunner(silent, new ProtocolWriter(rescored, TimeProvider.System), TimeProvider.System,
                NullLogger<EvalRunner>.Instance)
            .RescoreAsync(new StubEval(), "run_new", "run_old", stored, CancellationToken.None);

        string[] again = Lines(rescored);
        Assert.That(Of(again, "conversation"), Is.Not.Empty);
        Assert.That(Of(again, "conversation"), Is.EqualTo(Of(stored, "conversation")));
        Assert.That(Of(again, "mode_done"), Is.EqualTo(Of(stored, "mode_done")));
        Assert.That(silent.Calls, Is.Zero);
    }

    private static string[] Lines(StringWriter sink) =>
        sink.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries);

    /// <summary>The lines of one kind with the clock readings left out: the grades, outcomes, scores and rates are what a re-score must keep.</summary>
    private static List<string> Of(IEnumerable<string> lines, string kind) =>
        lines.Select(line => JsonDocument.Parse(line.TrimStart('@')).RootElement)
            .Where(e => e.GetProperty("event").GetString() == kind)
            .Select(e => string.Join(",",
                e.EnumerateObject().Where(p => p.Name is not ("t" or "seconds"))
                    .Select(p => p.Name + "=" + p.Value.GetRawText())))
            .ToList();

    /// <summary>A client that says it was called, and answers nothing.</summary>
    private sealed class SilentClient : ISessionClient
    {
        public int Calls { get; private set; }

        public Task<IReadOnlyList<string>> ToolsetsAsync(CancellationToken cancellationToken) => throw Called();

        public Task<string> OpenAsync(SessionRequest request, CancellationToken cancellationToken) => throw Called();

        public IAsyncEnumerable<SessionEvent> SendAsync(string session, string message,
            CancellationToken cancellationToken) => throw Called();

        public Task CloseAsync(string session, CancellationToken cancellationToken) => throw Called();

        private InvalidOperationException Called()
        {
            Calls++;
            return new InvalidOperationException("a re-score opened a session");
        }
    }
}
