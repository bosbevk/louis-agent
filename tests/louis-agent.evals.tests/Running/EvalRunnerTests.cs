using LouisAgent.Testing;
using System.Text.Json;
using LouisAgent.Api.Benchmarks.Runs;
using LouisAgent.Evals.Api;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Grading.Common;
using LouisAgent.Evals.Protocol;
using LouisAgent.Evals.Trials;
using LouisAgent.Core.Sessions.Replay;
using LouisAgent.SessionStream;
using Microsoft.Extensions.Logging.Abstractions;
using LouisAgent.Evals.Running;
using LouisAgent.Evals.Datasets;

namespace LouisAgent.Evals.Tests.Running;

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
            SessionEvent.From("message_start",
                Json(SampleRole.Fill(
                    """{"type":"message_start","message":{"session_id":"sess_canned","role":"assistant","toolsets":["web"],"session_role":"<RoleAlpha>"}}"""))),
            SessionEvent.From("content_block_delta",
                Json("""{"type":"content_block_delta","index":0,"delta":{"type":"text_delta","text":"Hallo"}}""")),
        };
        var sink = new StringWriter();
        var runner = new EvalRunner(new CannedSessionClient(turn), new ProtocolWriter(sink, new FixedClock(At)),
            new FixedClock(At),
            NullLogger<EvalRunner>.Instance);

        await runner.RunAsync(new CannedEval(), new EvalRun("run_1", 1), CancellationToken.None);

        var lines = sink.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(BenchmarkProtocol.Parse)
            .ToList();
        Assert.That(lines.Select(l => l.GetType().Name),
            Is.EqualTo(new[]
            {
                "ProtocolStarted", "ModeStarted", "QuestionAsked", "SseEvent", "SseEvent", "TurnEnded",
                "ConversationEnded", "ModeEnded", "SummaryReported"
            }),
            "the mode's summary line (F18 step 5c), read since 5c.2");
        var first = (BenchmarkProtocol.ProtocolStarted)lines[0];
        Assert.That((first.Eval, first.Commit, first.Models!.Single()),
            Is.EqualTo(("stub-cook", EvalBuild.Commit,
                new BenchmarkProtocol.ModelInfo("cook", BenchmarkModelLevel.Agent, SampleRole.Alpha, null))));
        Assert.That(lines.OfType<BenchmarkProtocol.TurnEnded>().Single().Answer, Is.EqualTo("Hallo"));
        Assert.That(
            lines.OfType<BenchmarkProtocol.ConversationEnded>().Single().Scores.GetProperty("routed_right")
                .GetBoolean(), Is.True);
    }

    [Test]
    public async Task ASessionIsClosedWhenItsTurnThrows()
    {
        var client = new ThrowingSessionClient();
        var runner = new EvalRunner(client, new ProtocolWriter(new StringWriter(), new FixedClock(At)),
            new FixedClock(At),
            NullLogger<EvalRunner>.Instance);

        Assert.That(
            async () => await runner.RunAsync(new CannedEval(), new EvalRun("run_1", 1), CancellationToken.None),
            Throws.InstanceOf<InvalidOperationException>().With.Message.EqualTo("the connection timed out"));
        Assert.That(client.Closed, Is.EqualTo(new[] { "sess_throws" }));
    }

    [Test]
    public void TheSendsErrorSurvivesAFailedClose()
    {
        var client = new ThrowingSessionClient(closeFails: true);
        var runner = new EvalRunner(client, new ProtocolWriter(new StringWriter(), new FixedClock(At)),
            new FixedClock(At),
            NullLogger<EvalRunner>.Instance);

        Assert.That(
            async () => await runner.RunAsync(new CannedEval(), new EvalRun("run_1", 1), CancellationToken.None),
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
        var runner = new EvalRunner(new CannedSessionClient(turn), new ProtocolWriter(sink, new FixedClock(At)),
            new FixedClock(At),
            NullLogger<EvalRunner>.Instance);

        await runner.RunAsync(new CannedEval(), new EvalRun("run_1", 1), CancellationToken.None);

        var lines = sink.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(BenchmarkProtocol.Parse)
            .ToList();
        Assert.That(lines.OfType<BenchmarkProtocol.TurnEnded>().Single().Answer, Is.EqualTo(""));
    }

    [Test]
    public async Task ATurnsFactsAreWritten_BesideTheModeAndTheConversation()
    {
        // F18 step 4.1: the runner writes what the eval says about each turn (who answered, the calls, the hosts), and the mode's
        // number of conversations and the conversation's errors.
        var turn = new[]
        {
            SessionEvent.From("content_block_delta",
                Json("""{"type":"content_block_delta","index":0,"delta":{"type":"text_delta","text":"Hallo"}}"""))
        };
        var facts = new TurnFacts(SampleRole.Alpha, null, 1, 3, ["www.checkers.co.za"], null);
        var sink = new StringWriter();
        var runner = new EvalRunner(new CannedSessionClient(turn), new ProtocolWriter(sink, new FixedClock(At)),
            new FixedClock(At),
            NullLogger<EvalRunner>.Instance);

        await runner.RunAsync(new CannedEval(facts), new EvalRun("run_1", 1), CancellationToken.None);

        var lines = sink.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(l => JsonDocument.Parse(l.TrimStart('@')).RootElement).ToList();
        var turnLine = lines.Single(e => e.GetProperty("event").GetString() == "turn");
        Assert.That(turnLine.GetProperty("role").GetString(), Is.EqualTo(SampleRole.Alpha));
        Assert.That(turnLine.GetProperty("puls_calls").GetInt32(), Is.EqualTo(1));
        Assert.That(turnLine.GetProperty("specialist_calls").GetInt32(), Is.EqualTo(3));
        Assert.That(turnLine.GetProperty("hosts").EnumerateArray().Select(h => h.GetString()),
            Is.EqualTo(new[] { "www.checkers.co.za" }));
        Assert.That(
            lines.Single(e => e.GetProperty("event").GetString() == "mode").GetProperty("conversations").GetInt32(),
            Is.EqualTo(1));
        Assert.That(
            lines.Single(e => e.GetProperty("event").GetString() == "conversation").GetProperty("errors").GetInt32(),
            Is.EqualTo(0));
    }

    [Test]
    public async Task TheConversationLineCarriesOneGradePerGraderInOrder_AndTheOutcome()
    {
        // F18 step 4.5a: the runner grades the trial with the eval's graders and writes the grades and the outcome on the conversation.
        var turn = new[]
        {
            SessionEvent.From("content_block_delta",
                Json("""{"type":"content_block_delta","index":0,"delta":{"type":"text_delta","text":"Hallo"}}"""))
        };
        var sink = new StringWriter();
        var runner = new EvalRunner(new CannedSessionClient(turn), new ProtocolWriter(sink, new FixedClock(At)),
            new FixedClock(At),
            NullLogger<EvalRunner>.Instance);

        await runner.RunAsync(
            new CannedEval(graders: [new WithinTimeGrader(TimeSpan.FromMinutes(1)), new NoErrorGrader()]),
            new EvalRun("run_1", 1),
            CancellationToken.None);

        var lines = sink.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(BenchmarkProtocol.Parse)
            .ToList();
        var conversation = lines.OfType<BenchmarkProtocol.ConversationEnded>().Single();
        Assert.That(conversation.Grades!.Select(g => (g.Grader, g.Outcome)),
            Is.EqualTo(new[] { ("within-time", BenchmarkOutcome.Pass), ("no-error", BenchmarkOutcome.Pass) }));
        Assert.That(conversation.Outcome, Is.EqualTo(BenchmarkOutcome.Pass));
    }

    [Test]
    public async Task ScoresOfReceivesTheGrades()
    {
        var turn = new[]
        {
            SessionEvent.From("content_block_delta",
                Json("""{"type":"content_block_delta","index":0,"delta":{"type":"text_delta","text":"Hallo"}}"""))
        };
        var eval = new CannedEval(graders: [new NoErrorGrader()]);
        var runner = new EvalRunner(new CannedSessionClient(turn),
            new ProtocolWriter(new StringWriter(), new FixedClock(At)), new FixedClock(At),
            NullLogger<EvalRunner>.Instance);

        await runner.RunAsync(eval, new EvalRun("run_1", 1), CancellationToken.None);

        Assert.That(eval.GradesScored.Select(g => g.Grader), Is.EqualTo(new[] { "no-error" }));
    }

    [Test]
    public async Task ADeltaWhoseTextIsANumberDoesNotStopTheRun_AndTheAnswerIsWhatAnswerOfGives()
    {
        // The runner's own loop called GetString() on the text, which throws on a number; EngineEvents.AnswerOf skips such a delta.
        var turn = new[]
        {
            SessionEvent.From("content_block_delta",
                Json("""{"type":"content_block_delta","index":0,"delta":{"type":"text_delta","text":42}}""")),
            SessionEvent.From("content_block_delta",
                Json("""{"type":"content_block_delta","index":0,"delta":{"type":"text_delta","text":" Ja "}}""")),
        };
        var sink = new StringWriter();
        var runner = new EvalRunner(new CannedSessionClient(turn), new ProtocolWriter(sink, new FixedClock(At)),
            new FixedClock(At),
            NullLogger<EvalRunner>.Instance);

        await runner.RunAsync(new CannedEval(), new EvalRun("run_1", 1), CancellationToken.None);

        var lines = sink.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(BenchmarkProtocol.Parse)
            .ToList();
        var turnLine = lines.OfType<BenchmarkProtocol.TurnEnded>().Single();
        Assert.That(turnLine.Answer,
            Is.EqualTo(EngineEvents.AnswerOf(new TurnTranscript(1, "sess_canned", "Gee my resep", turn, TimeSpan.Zero,
                null))));
        Assert.That(turnLine.Answer, Is.EqualTo("Ja"));
    }

    // F18 step 4a.3: every session the runner opens sends a replay (the eval's mark); its web and its trial follow the run's flags.
    private static readonly IReadOnlyList<SessionEvent> OneAnswer =
    [
        SessionEvent.From("content_block_delta",
            Json("""{"type":"content_block_delta","index":0,"delta":{"type":"text_delta","text":"Hallo"}}"""))
    ];

    /// <summary>Runs one eval with its sessions answered from <paramref name="turns"/> canned turns; returns the client, with what it was asked to open.</summary>
    private static async Task<CannedSessionClient> RunAndOpened(EvalRun run, int turns = 1)
    {
        var client = new CannedSessionClient(Enumerable.Repeat(OneAnswer, turns).ToArray());
        var runner = new EvalRunner(client, new ProtocolWriter(new StringWriter(), new FixedClock(At)),
            new FixedClock(At),
            NullLogger<EvalRunner>.Instance);

        await runner.RunAsync(new CannedEval(), run, CancellationToken.None);
        return client;
    }

    [Test]
    public async Task WithNoWebFlags_EverySessionStillSendsTheMark_WithNeitherWebNorTrial()
    {
        var client = await RunAndOpened(new EvalRun("run_1", 1));

        Assert.That(client.Opened.Select(s => s.Replay), Is.EqualTo(new[] { new WebReplayRequest(null, null) }));
    }

    [Test]
    public async Task ARecording_SendsWebRecord_AndNoTrial_SoTheApiResolvesIt()
    {
        var client = await RunAndOpened(new EvalRun("run_1", 1, WebReplayMode.Record));

        Assert.That(client.Opened.Select(s => s.Replay),
            Is.EqualTo(new[] { new WebReplayRequest(WebReplayMode.Record, null) }));
    }

    [Test]
    public async Task AReplay_NamesEachTrialTheRecordingWrote_OnePerSessionAndRepeat()
    {
        var client = await RunAndOpened(new EvalRun("run_new", 2, WebReplayMode.Replay, "run_A"), turns: 2);

        Assert.That(client.Opened.Select(s => s.Replay.Of),
            Is.EqualTo(new[] { "run_A:cook:1/pannekoek", "run_A:cook:2/pannekoek" }));
        Assert.That(client.Opened.Select(s => s.Replay.Web).Distinct(), Is.EqualTo(new[] { WebReplayMode.Replay }));
    }

    [Test]
    public async Task AReplayWithTheModel_SendsModelTrue_WithEachTrialsKey_AndEveryOtherRunSendsFalse()
    {
        // F18 step 4e.4: --replay is the web replay plus the model's answers; the API decides whether it accepts it.
        var withModel = await RunAndOpened(new EvalRun("run_new", 2, WebReplayMode.Replay, "run_A", Model: true),
            turns: 2);
        var webOnly = await RunAndOpened(new EvalRun("run_new", 1, WebReplayMode.Replay, "run_A"));
        var recording = await RunAndOpened(new EvalRun("run_R", 1, WebReplayMode.Record));
        var noFlags = await RunAndOpened(new EvalRun("run_1", 1));

        Assert.That(withModel.Opened.Select(s => s.Replay), Is.EqualTo(new[]
        {
            new WebReplayRequest(WebReplayMode.Replay, "run_A:cook:1/pannekoek", true),
            new WebReplayRequest(WebReplayMode.Replay, "run_A:cook:2/pannekoek", true),
        }));
        Assert.That(webOnly.Opened.Single().Replay.Model, Is.False);
        Assert.That(recording.Opened.Single().Replay.Model, Is.False);
        Assert.That(noFlags.Opened.Single().Replay.Model, Is.False);
    }

    [Test]
    public async Task AReplayReadsTheKeyTheRecordingWrote()
    {
        // The cross-check: a recording's sessions carry a run tag and a task, and the API writes {run tag}/{task} as the trial's key.
        // A replay of that run must name exactly that key, so a later change to the run tag can't break replays silently.
        var recorded = (await RunAndOpened(new EvalRun("run_R", 1, WebReplayMode.Record))).Opened.Single();
        var replayed = (await RunAndOpened(new EvalRun("run_new", 1, WebReplayMode.Replay, "run_R"))).Opened.Single();

        Assert.That(replayed.Replay.Of, Is.EqualTo($"{recorded.RunTag}/{recorded.Task}"));
    }

    /// <summary>A session whose send fails, as a timed-out connection does; it records which sessions were closed.</summary>
    private sealed class ThrowingSessionClient(bool closeFails = false) : ISessionClient
    {
        public List<string> Closed { get; } = [];

        public Task<IReadOnlyList<string>> ToolsetsAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());

        public Task<string> OpenAsync(SessionRequest request, CancellationToken cancellationToken) =>
            Task.FromResult("sess_throws");

        public IAsyncEnumerable<SessionEvent> SendAsync(string session, string message,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("the connection timed out");

        public Task CloseAsync(string session, CancellationToken cancellationToken)
        {
            Closed.Add(session);
            if (closeFails) throw new InvalidOperationException("the API is down");
            return Task.CompletedTask;
        }
    }

    private static JsonElement Json(string text) => JsonDocument.Parse(text).RootElement.Clone();

    /// <summary>The stub eval: one mode, one scenario with one message, scores that say the route was right; it keeps the grades it was scored with.</summary>
    private sealed class CannedEval(TurnFacts? facts = null, IReadOnlyList<IGrader>? graders = null) : IEval
    {
        public IReadOnlyList<Grade> GradesScored { get; private set; } = [];

        public string Name => "stub-cook";
        public int Version => 1;
        public string Proves => "A canned trial, to see the runner's lines.";

        public ValueTask<IReadOnlyList<EvalMode>> ModesAsync(ISessionClient sessions,
            CancellationToken cancellationToken) =>
            ValueTask.FromResult<IReadOnlyList<EvalMode>>([
                new EvalMode("cook", SampleRole.Alpha, [SampleToolset.Alpha])
            ]);

        public IReadOnlyList<EvalScenario> Scenarios { get; } =
        [
            new EvalScenario("pannekoek", new EvalInput(null, "Africa/Johannesburg", ["Gee my resep"]), default, [],
                DatasetSplit.Dev)
        ];

        public IReadOnlyList<IGrader> Graders { get; } = graders ?? [];

        public EvalScores ScoresOf(Trial trial, IReadOnlyList<Grade> grades)
        {
            GradesScored = grades;
            return new CannedScores(RoutedRight: true);
        }

        public TurnFacts TurnFactsOf(TurnTranscript turn, EvalMode mode) =>
            facts ?? new TurnFacts(null, null, 0, 0, [], null);
    }

    private sealed record CannedScores(bool RoutedRight) : EvalScores;
}
