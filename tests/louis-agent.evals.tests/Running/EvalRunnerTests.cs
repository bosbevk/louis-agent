using LouisAgent.Testing;
using System.Text.Json;
using LouisAgent.Api.Evals.Runs.Kinds;
using LouisAgent.Api.Evals.Runs.Protocol;
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

/// <summary>
/// The runner on a canned trial. The client answers from canned events and never calls the API, so every line written
/// is the runner's own: the mode, the question, each event the session sent, the turn, the conversation and the
/// mode's end.
/// </summary>
public class EvalRunnerTests
{
    /// <summary>The fixed moment every line is stamped with.</summary>
    private static readonly DateTimeOffset At = DateTimeOffset.FromUnixTimeMilliseconds(1700000000123);

    /// <summary>One trial writes its lines in order: the eval and its model, the answer and the scores.</summary>
    [Test]
    public async Task ARunWritesOneCannedTrialInOrder()
    {
        // A turn that opens a session and says "Hallo".
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

        // The lines come in order, ending with the mode's summary.
        var lines = sink.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(EvalProtocol.Parse)
            .ToList();
        Assert.That(lines.Select(l => l.GetType().Name),
            Is.EqualTo(new[]
            {
                "ProtocolStarted", "ModeStarted", "QuestionAsked", "SseEvent", "SseEvent", "TurnEnded",
                "ConversationEnded", "ModeEnded", "SummaryReported"
            }),
            "the mode's summary line (F18 step 5c), read since 5c.2");

        // The first line names the eval and its model; the turn and the conversation carry what happened.
        var first = (EvalProtocol.ProtocolStarted)lines[0];
        Assert.That((first.Eval, first.Commit, first.Models!.Single()),
            Is.EqualTo(("stub-cook", EvalBuild.Commit,
                new EvalProtocol.ModelInfo("cook", EvalModelLevel.Agent, SampleRole.Alpha, null))));
        Assert.That(lines.OfType<EvalProtocol.TurnEnded>().Single().Answer, Is.EqualTo("Hallo"));
        Assert.That(
            lines.OfType<EvalProtocol.ConversationEnded>().Single().Scores.GetProperty("routed_right")
                .GetBoolean(), Is.True);
    }

    /// <summary>When a turn blows up, the session is still closed, and the turn's error is what you see.</summary>
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

    /// <summary>If closing fails too, you still see why the turn failed, not why the close did.</summary>
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

    /// <summary>A text event with no text, or a broken one, is skipped: the run goes on with an empty answer.</summary>
    [Test]
    public async Task ADeltaWithoutItsDeltaDoesNotStopTheRun()
    {
        var turn = new[]
        {
            SessionEvent.From("content_block_delta", Json("""{"type":"content_block_delta","index":0}""")),
            SessionEvent.From("content_block_delta",
                Json("""{"type":"content_block_delta","index":0,"delta":7}""")),
        };
        var sink = new StringWriter();
        var runner = new EvalRunner(new CannedSessionClient(turn), new ProtocolWriter(sink, new FixedClock(At)),
            new FixedClock(At),
            NullLogger<EvalRunner>.Instance);

        await runner.RunAsync(new CannedEval(), new EvalRun("run_1", 1), CancellationToken.None);

        var lines = sink.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(EvalProtocol.Parse)
            .ToList();
        Assert.That(lines.OfType<EvalProtocol.TurnEnded>().Single().Answer, Is.EqualTo(""));
    }

    /// <summary>
    /// What the eval says about each turn (who answered, the calls, the hosts) is written on the turn, with the
    /// mode's number of conversations and the conversation's errors.
    /// </summary>
    [Test]
    public async Task ATurnsFactsAreWritten_BesideTheModeAndTheConversation()
    {
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

        // Read the raw lines, so we see the fields as written.
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

    /// <summary>Each of the eval's graders grades the trial, in order, and the outcome is written.</summary>
    [Test]
    public async Task TheConversationLineCarriesOneGradePerGraderInOrder_AndTheOutcome()
    {
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

        var lines = sink.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(EvalProtocol.Parse)
            .ToList();
        var conversation = lines.OfType<EvalProtocol.ConversationEnded>().Single();
        Assert.That(conversation.Grades!.Select(g => (g.Grader, g.Outcome)),
            Is.EqualTo(new[] { ("within-time", EvalOutcome.Pass), ("no-error", EvalOutcome.Pass) }));
        Assert.That(conversation.Outcome, Is.EqualTo(EvalOutcome.Pass));
    }

    /// <summary>The eval's scores get to see the grades the trial was given.</summary>
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

    /// <summary>
    /// A text event whose text is a number is skipped instead of stopping the run, and the answer is exactly what the
    /// shared answer reader makes of the turn. The runner's own loop used to read the text as a string and throw.
    /// </summary>
    [Test]
    public async Task ADeltaWhoseTextIsANumberDoesNotStopTheRun_AndTheAnswerIsWhatAnswerOfGives()
    {
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

        var lines = sink.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(EvalProtocol.Parse)
            .ToList();
        var turnLine = lines.OfType<EvalProtocol.TurnEnded>().Single();
        Assert.That(turnLine.Answer,
            Is.EqualTo(EngineEvents.AnswerOf(new TurnTranscript(1, "sess_canned", "Gee my resep", turn, TimeSpan.Zero,
                null))));
        Assert.That(turnLine.Answer, Is.EqualTo("Ja"));
    }

    /// <summary>
    /// One turn that says "Hallo", for the replay tests below. Every session the runner opens sends a replay request
    /// (the eval's mark); its web and its trial follow the run's flags.
    /// </summary>
    private static readonly IReadOnlyList<SessionEvent> OneAnswer =
    [
        SessionEvent.From("content_block_delta",
            Json("""{"type":"content_block_delta","index":0,"delta":{"type":"text_delta","text":"Hallo"}}"""))
    ];

    /// <summary>
    /// Runs the stub eval with its sessions answered from that many canned turns, and hands back the client, so you
    /// can see what it was asked to open.
    /// </summary>
    private static async Task<CannedSessionClient> RunAndOpened(EvalRun run, int turns = 1)
    {
        var client = new CannedSessionClient(Enumerable.Repeat(OneAnswer, turns).ToArray());
        var runner = new EvalRunner(client, new ProtocolWriter(new StringWriter(), new FixedClock(At)),
            new FixedClock(At),
            NullLogger<EvalRunner>.Instance);

        await runner.RunAsync(new CannedEval(), run, CancellationToken.None);

        return client;
    }

    /// <summary>A plain run still marks every session as the eval's, with no web replay and no trial named.</summary>
    [Test]
    public async Task WithNoWebFlags_EverySessionStillSendsTheMark_WithNeitherWebNorTrial()
    {
        var client = await RunAndOpened(new EvalRun("run_1", 1));

        Assert.That(client.Opened.Select(s => s.Replay), Is.EqualTo(new[] { new WebReplayRequest(null, null) }));
    }

    /// <summary>A recording asks to record the web and names no trial: the API works out the key itself.</summary>
    [Test]
    public async Task ARecording_SendsWebRecord_AndNoTrial_SoTheApiResolvesIt()
    {
        var client = await RunAndOpened(new EvalRun("run_1", 1, WebReplayMode.Record));

        Assert.That(client.Opened.Select(s => s.Replay),
            Is.EqualTo(new[] { new WebReplayRequest(WebReplayMode.Record, null) }));
    }

    /// <summary>A replay names the recorded trial for each session and each repeat.</summary>
    [Test]
    public async Task AReplay_NamesEachTrialTheRecordingWrote_OnePerSessionAndRepeat()
    {
        var client = await RunAndOpened(new EvalRun("run_new", 2, WebReplayMode.Replay, "run_A"), turns: 2);

        Assert.That(client.Opened.Select(s => s.Replay.Of),
            Is.EqualTo(new[] { "run_A:cook:1/pannekoek", "run_A:cook:2/pannekoek" }));
        Assert.That(client.Opened.Select(s => s.Replay.Web).Distinct(), Is.EqualTo(new[] { WebReplayMode.Replay }));
    }

    /// <summary>
    /// A full replay is the web replay plus the model's recorded answers, so only it asks for the model; the API
    /// decides whether to accept it. Every other kind of run asks for no model.
    /// </summary>
    [Test]
    public async Task AReplayWithTheModel_SendsModelTrue_WithEachTrialsKey_AndEveryOtherRunSendsFalse()
    {
        var withModel = await RunAndOpened(new EvalRun("run_new", 2, WebReplayMode.Replay, "run_A", Model: true),
            turns: 2);
        var webOnly = await RunAndOpened(new EvalRun("run_new", 1, WebReplayMode.Replay, "run_A"));
        var recording = await RunAndOpened(new EvalRun("run_R", 1, WebReplayMode.Record));
        var noFlags = await RunAndOpened(new EvalRun("run_1", 1));

        Assert.That(withModel.Opened.Select(s => s.Replay),
            Is.EqualTo(new[]
            {
                new WebReplayRequest(WebReplayMode.Replay, "run_A:cook:1/pannekoek", true),
                new WebReplayRequest(WebReplayMode.Replay, "run_A:cook:2/pannekoek", true),
            }));
        Assert.That(webOnly.Opened.Single().Replay.Model, Is.False);
        Assert.That(recording.Opened.Single().Replay.Model, Is.False);
        Assert.That(noFlags.Opened.Single().Replay.Model, Is.False);
    }

    /// <summary>
    /// The cross-check: a recording's sessions carry a run tag and a task, and the API keys the trial as
    /// {run tag}/{task}. A replay must name exactly that key, so a later change to the run tag can't silently break
    /// replays.
    /// </summary>
    [Test]
    public async Task AReplayReadsTheKeyTheRecordingWrote()
    {
        var recorded = (await RunAndOpened(new EvalRun("run_R", 1, WebReplayMode.Record))).Opened.Single();
        var replayed = (await RunAndOpened(new EvalRun("run_new", 1, WebReplayMode.Replay, "run_R"))).Opened.Single();

        Assert.That(replayed.Replay.Of, Is.EqualTo($"{recorded.RunTag}/{recorded.Task}"));
    }

    /// <summary>A session that fails to send, like a timed-out connection, and notes which ones were closed.</summary>
    private sealed class ThrowingSessionClient(bool closeFails = false) : ISessionClient
    {
        public List<string> Closed { get; } = [];

        /// <summary>No toolsets: these tests never ask.</summary>
        public Task<IReadOnlyList<string>> ToolsetsAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());

        /// <summary>Every session opens as the same one.</summary>
        public Task<string> OpenAsync(SessionRequest request, CancellationToken cancellationToken) =>
            Task.FromResult("sess_throws");

        /// <summary>Sending always fails, as a timed-out connection would.</summary>
        public IAsyncEnumerable<SessionEvent> SendAsync(string session, string message,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("the connection timed out");

        /// <summary>Notes the close, and fails it too when asked to.</summary>
        public Task CloseAsync(string session, CancellationToken cancellationToken)
        {
            Closed.Add(session);
            if (closeFails) throw new InvalidOperationException("the API is down");
            return Task.CompletedTask;
        }
    }

    /// <summary>A bit of JSON text as an element that outlives its document.</summary>
    private static JsonElement Json(string text) => JsonDocument.Parse(text).RootElement.Clone();

    /// <summary>
    /// The stub eval: one mode, one scenario with one message, and scores that say the route was right. It keeps the
    /// grades it was scored with, so a test can look at them.
    /// </summary>
    private sealed class CannedEval(TurnFacts? facts = null, IReadOnlyList<IGrader>? graders = null) : IEval
    {
        public IReadOnlyList<Grade> GradesScored { get; private set; } = [];

        public string Name => "stub-cook";
        public int Version => 1;
        public string Proves => "A canned trial, to see the runner's lines.";

        /// <summary>The one mode, "cook".</summary>
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

        /// <summary>Keeps the grades it was given, and says the route was right.</summary>
        public EvalScores ScoresOf(Trial trial, IReadOnlyList<Grade> grades)
        {
            GradesScored = grades;
            return new CannedScores(RoutedRight: true);
        }

        /// <summary>The facts the test handed in, or none at all.</summary>
        public TurnFacts TurnFactsOf(TurnTranscript turn, EvalMode mode) =>
            facts ?? new TurnFacts(null, null, 0, 0, [], null);
    }

    /// <summary>The stub eval's only score: whether the route was right.</summary>
    private sealed record CannedScores(bool RoutedRight) : EvalScores;
}
