using System.Text.Json;
using LouisAgent.Api.Benchmarks.Runs;
using LouisAgent.Evals.Api;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Protocol;
using LouisAgent.Evals.Trials;
using LouisAgent.SessionStream;
using Microsoft.Extensions.Logging.Abstractions;

namespace LouisAgent.Evals.Tests;

// F18 step 2: one canned trial, written in order. The client answers from canned events and never calls the API, so the
// lines are the runner's own: the mode, the question, each event the session sent, the turn, the conversation and the mode's end.
public class EvalRunnerTests
{
    private static readonly DateTimeOffset At = DateTimeOffset.FromUnixTimeMilliseconds(1700000000123);

    [Test]
    public async Task ARunWritesOneCannedTrialInOrder()
    {
        var turn = new[]
        {
            SessionEvent.From("message_start", Json("""{"type":"message_start","message":{"session_id":"sess_canned","role":"assistant","toolsets":["web"],"session_role":"puls-cook"}}""")),
            SessionEvent.From("content_block_delta", Json("""{"type":"content_block_delta","index":0,"delta":{"type":"text_delta","text":"Hallo"}}""")),
        };
        var sink = new StringWriter();
        var runner = new EvalRunner(new CannedSessionClient(turn), new ProtocolWriter(sink, new FixedClock(At)), new FixedClock(At),
            NullLogger<EvalRunner>.Instance);

        await runner.RunAsync(new CannedEval(), new EvalRun("run_1", 1), CancellationToken.None);

        var lines = sink.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(BenchmarkProtocol.Parse).ToList();
        Assert.That(lines.Select(l => l.GetType().Name),
            Is.EqualTo(new[] { "ProtocolStarted", "ModeStarted", "QuestionAsked", "SseEvent", "SseEvent", "TurnEnded", "ConversationEnded", "ModeEnded" }));
        var first = (BenchmarkProtocol.ProtocolStarted)lines[0];
        Assert.That((first.Eval, first.Commit, first.Models!["cook"]), Is.EqualTo(("stub-cook", EvalBuild.Commit, "unknown")));
        Assert.That(lines.OfType<BenchmarkProtocol.TurnEnded>().Single().Answer, Is.EqualTo("Hallo"));
        Assert.That(lines.OfType<BenchmarkProtocol.ConversationEnded>().Single().Scores.GetProperty("routed_right").GetBoolean(), Is.True);
    }

    [Test]
    public async Task ASessionIsClosedWhenItsTurnThrows()
    {
        var client = new ThrowingSessionClient();
        var runner = new EvalRunner(client, new ProtocolWriter(new StringWriter(), new FixedClock(At)), new FixedClock(At),
            NullLogger<EvalRunner>.Instance);

        Assert.That(async () => await runner.RunAsync(new CannedEval(), new EvalRun("run_1", 1), CancellationToken.None),
            Throws.InstanceOf<InvalidOperationException>().With.Message.EqualTo("the connection timed out"));
        Assert.That(client.Closed, Is.EqualTo(new[] { "sess_throws" }));
    }

    [Test]
    public void TheSendsErrorSurvivesAFailedClose()
    {
        var client = new ThrowingSessionClient(closeFails: true);
        var runner = new EvalRunner(client, new ProtocolWriter(new StringWriter(), new FixedClock(At)), new FixedClock(At),
            NullLogger<EvalRunner>.Instance);

        Assert.That(async () => await runner.RunAsync(new CannedEval(), new EvalRun("run_1", 1), CancellationToken.None),
            Throws.InstanceOf<InvalidOperationException>().With.Message.EqualTo("the connection timed out"));
        Assert.That(client.Closed, Is.EqualTo(new[] { "sess_throws" }));
    }

    [Test]
    public async Task ADeltaWithoutItsDeltaDoesNotStopTheRun()
    {
        var turn = new[]
        {
            SessionEvent.From("content_block_delta", Json("""{"type":"content_block_delta","index":0}""")),
            SessionEvent.From("content_block_delta", Json("""{"type":"content_block_delta","index":0,"delta":7}""")),
        };
        var sink = new StringWriter();
        var runner = new EvalRunner(new CannedSessionClient(turn), new ProtocolWriter(sink, new FixedClock(At)), new FixedClock(At),
            NullLogger<EvalRunner>.Instance);

        await runner.RunAsync(new CannedEval(), new EvalRun("run_1", 1), CancellationToken.None);

        var lines = sink.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(BenchmarkProtocol.Parse).ToList();
        Assert.That(lines.OfType<BenchmarkProtocol.TurnEnded>().Single().Answer, Is.EqualTo(""));
    }

    /// <summary>A session whose send fails, as a timed-out connection does; it records which sessions were closed.</summary>
    private sealed class ThrowingSessionClient(bool closeFails = false) : ISessionClient
    {
        public List<string> Closed { get; } = [];

        public Task<IReadOnlyList<string>> ToolsetsAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());

        public Task<string> OpenAsync(SessionRequest request, CancellationToken cancellationToken) => Task.FromResult("sess_throws");

        public IAsyncEnumerable<SessionEvent> SendAsync(string session, string message, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("the connection timed out");

        public Task CloseAsync(string session, CancellationToken cancellationToken)
        {
            Closed.Add(session);
            if (closeFails) throw new InvalidOperationException("the API is down");
            return Task.CompletedTask;
        }
    }

    private static JsonElement Json(string text) => JsonDocument.Parse(text).RootElement.Clone();

    /// <summary>The stub eval: one mode, one scenario with one message, scores that say the route was right.</summary>
    private sealed class CannedEval : IEval
    {
        public string Name => "stub-cook";
        public string? Identity => null;
        public int Version => 1;
        public string Proves => "A canned trial, to see the runner's lines.";

        public ValueTask<IReadOnlyList<EvalMode>> ModesAsync(ISessionClient sessions, CancellationToken cancellationToken) =>
            ValueTask.FromResult<IReadOnlyList<EvalMode>>([new EvalMode("cook", "puls-cook", ["web"])]);

        public IReadOnlyList<EvalScenario> Scenarios { get; } =
            [new EvalScenario("pannekoek", new EvalInput(null, "Africa/Johannesburg", ["Gee my resep"]), default)];

        public IReadOnlyList<IGrader> Graders { get; } = [];

        public EvalScores ScoresOf(Trial trial, IReadOnlyList<Grade> grades) => new CannedScores(RoutedRight: true);
    }

    private sealed record CannedScores(bool RoutedRight) : EvalScores;
}
