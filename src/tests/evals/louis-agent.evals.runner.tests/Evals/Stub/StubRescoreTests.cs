using LouisAgent.Evals.Api;
using LouisAgent.Evals.Protocol;
using LouisAgent.Evals.Protocol.Events.Conversations;
using LouisAgent.Evals.Protocol.Events.Run;
using LouisAgent.Evals.Running;
using LouisAgent.SessionStream;
using Microsoft.Extensions.Logging.Abstractions;
using LouisAgent.Evals.Runner.Evals.Stub;

namespace LouisAgent.Evals.Runner.Tests.Evals.Stub;

/// <summary>
/// The stub's run, re-scored (F18 step 5b). The stub's graders are the same today, so each conversation keeps its
/// grades and each mode its rates, and the session client the runner is given is never called.
/// </summary>
public class StubRescoreTests
{
    /// <summary>The re-score writes the same conversations and mode ends as the run, and opens no session.</summary>
    [Test]
    public async Task ARescoredStubRunHasTheSameGradesAndRatesAsTheRun()
    {
        // The stub's run, as it's stored.
        var recorded = new StringWriter();
        await new EvalRunner(new StubSessionClient(), new ProtocolWriter(recorded, TimeProvider.System),
                TimeProvider.System, NullLogger<EvalRunner>.Instance)
            .RunAsync(new StubEval(), new EvalRun("run_old", 2), CancellationToken.None);
        string[] stored = Lines(recorded);

        // Re-scored, with a client that only counts the calls it gets.
        var rescored = new StringWriter();
        var silent = new SilentClient();
        await new EvalRunner(silent, new ProtocolWriter(rescored, TimeProvider.System), TimeProvider.System,
                NullLogger<EvalRunner>.Instance)
            .RescoreAsync(new StubEval(), "run_new", "run_old", stored, CancellationToken.None);
        string[] again = Lines(rescored);

        Assert.That(Of<ConversationEvent>(again), Is.Not.Empty);
        Assert.That(Of<ConversationEvent>(again), Is.EqualTo(Of<ConversationEvent>(stored)));
        Assert.That(Of<ModeDoneEvent>(again), Is.Not.Empty);
        Assert.That(Of<ModeDoneEvent>(again), Is.EqualTo(Of<ModeDoneEvent>(stored)));
        Assert.That(silent.Calls, Is.Zero);
    }

    /// <summary>The lines a writer printed.</summary>
    private static string[] Lines(StringWriter sink) =>
        sink.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries);

    /// <summary>
    /// The events of one kind, written again with the clock left out: the grades, outcomes, metrics and rates are
    /// what a re-score must keep.
    /// </summary>
    private static List<string> Of<T>(IEnumerable<string> lines) where T : EvalEvent =>
        [.. lines.Select(EvalProtocolJson.Read).OfType<T>().Select(e => EvalProtocolJson.Line(e with { T = default }))];

    /// <summary>A client that says it was called, and answers nothing.</summary>
    private sealed class SilentClient : ISessionClient
    {
        /// <summary>How many times anything was asked of it.</summary>
        public int Calls { get; private set; }

        /// <summary>Counts the call and refuses it.</summary>
        public Task<IReadOnlyList<string>> ToolsetsAsync(CancellationToken cancellationToken) => throw Called();

        /// <summary>Counts the call and refuses it.</summary>
        public Task<string> OpenAsync(SessionRequest request, CancellationToken cancellationToken) => throw Called();

        /// <summary>Counts the call and refuses it.</summary>
        public IAsyncEnumerable<SessionEvent> SendAsync(string session, string message,
            CancellationToken cancellationToken) => throw Called();

        /// <summary>Counts the call and refuses it.</summary>
        public Task CloseAsync(string session, CancellationToken cancellationToken) => throw Called();

        /// <summary>Counts a call, and the error that says a re-score opened a session.</summary>
        private InvalidOperationException Called()
        {
            Calls++;
            return new InvalidOperationException("a re-score opened a session");
        }
    }
}
