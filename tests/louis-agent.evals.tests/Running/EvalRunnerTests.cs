using LouisAgent.Testing;
using System.Text.Json;
using LouisAgent.Evals.Api;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Grading.Common;
using LouisAgent.Evals.Protocol;
using LouisAgent.Evals.Protocol.Events.Conversations;
using LouisAgent.Evals.Protocol.Events.Run;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Protocol.Parts.Run;
using LouisAgent.Evals.Tests.Protocol;
using LouisAgent.Evals.Trials;
using LouisAgent.Core.Sessions.Replay;
using LouisAgent.SessionStream;
using Microsoft.Extensions.Logging.Abstractions;
using LouisAgent.Evals.Running;
using LouisAgent.Evals.Datasets;
using LouisAgent.Evals.Datasets.Scenarios;

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

    /// <summary>Runs the canned eval over the canned turn, and hands back the events it wrote.</summary>
    private static async Task<IReadOnlyList<EvalEvent>> RunAsync(SessionEvent[] turn, CannedEval? eval = null)
    {
        var sink = new StringWriter();
        var runner = new EvalRunner(new CannedSessionClient(turn), new ProtocolWriter(sink, new FixedClock(At)),
            new FixedClock(At),
            NullLogger<EvalRunner>.Instance);

        await runner.RunAsync(eval ?? new CannedEval(), new EvalRun("run_1", 1), CancellationToken.None);

        return Written.Events(sink);
    }

    /// <summary>
    /// One trial writes its lines in order: the eval and its model, the turn, the conversation, the end.
    /// </summary>
    [Test]
    public async Task ARunWritesOneCannedTrialInOrder()
    {
        // A turn that opens a session and says "Hallo".
        SessionEvent[] turn =
        [
            SessionEvent.From("message_start",
                Json(SampleRole.Fill("""{"type":"message_start","message":{"session_id":"sess_"""
                                     + """canned","role":"assistant","toolsets":["web"],"session_"""
                                     + """role":"<RoleAlpha>"}}"""))),
            SessionEvent.From("content_block_delta",
                Json("""{"type":"content_block_delta","index":0,"delta":{"type":"text_delta","text":"Hallo"}}""")),
        ];

        var events = await RunAsync(turn);

        // The lines come in order, ending with the mode's summary.
        Assert.That(events.Select(e => e.GetType()),
            Is.EqualTo(new[]
            {
                typeof(ProtocolEvent), typeof(ModeEvent), typeof(QuestionEvent), typeof(SseEvent), typeof(SseEvent),
                typeof(TurnEvent), typeof(ConversationEvent), typeof(ModeDoneEvent), typeof(SummaryEvent),
            }));

        // The first line names the eval and its model; the turn carries the answer.
        var first = (ProtocolEvent)events[0];
        Assert.That((first.Eval, first.Commit, first.Models.Single()),
            Is.EqualTo(("stub-cook", EvalBuild.Commit,
                new ModelInfo("cook", ModelLevel.Agent, SampleRole.Alpha, null))));
        Assert.That(events.OfType<TurnEvent>().Single().Answer, Is.EqualTo("Hallo"));
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
        SessionEvent[] turn =
        [
            SessionEvent.From("content_block_delta", Json("""{"type":"content_block_delta","index":0}""")),
            SessionEvent.From("content_block_delta",
                Json("""{"type":"content_block_delta","index":0,"delta":7}""")),
        ];

        var events = await RunAsync(turn);

        Assert.That(events.OfType<TurnEvent>().Single().Answer, Is.EqualTo(""));
    }

    /// <summary>
    /// What the eval says about each turn (who answered, the calls, the hosts) is written on the turn, with the
    /// mode's number of conversations and the conversation's errors.
    /// </summary>
    [Test]
    public async Task ATurnsFactsAreWritten_BesideTheModeAndTheConversation()
    {
        var facts = new TurnFacts(SampleRole.Alpha, null, 1, 3, ["www.checkers.co.za"], null);

        var events = await RunAsync(OneAnswer.ToArray(), new CannedEval(facts));

        var turn = events.OfType<TurnEvent>().Single();
        Assert.That((turn.Role, turn.AgentCalls, turn.SpecialistCalls), Is.EqualTo((SampleRole.Alpha, 1, 3)));
        Assert.That(turn.Hosts, Is.EqualTo(new[] { "www.checkers.co.za" }));
        Assert.That(events.OfType<ModeEvent>().Single().Conversations, Is.EqualTo(1));
        Assert.That(events.OfType<ConversationEvent>().Single().Errors, Is.EqualTo(0));
    }

    /// <summary>
    /// A conversation's metrics are its turns' facts added up: the calls summed, each host once, the turns counted;
    /// the tokens and the cost are the ledger's, so the harness leaves them unknown.
    /// </summary>
    [Test]
    public async Task AConversationsMetrics_AreItsTurnsFacts_AddedUp()
    {
        var facts = new TurnFacts(SampleRole.Alpha, null, 1, 3, ["www.checkers.co.za"], null);
        SessionEvent[] answer = [.. OneAnswer];
        var sink = new StringWriter();
        var runner = new EvalRunner(new CannedSessionClient(answer, answer),
            new ProtocolWriter(sink, new FixedClock(At)),
            new FixedClock(At),
            NullLogger<EvalRunner>.Instance);

        await runner.RunAsync(new CannedEval(facts, messages: 2), new EvalRun("run_1", 1), CancellationToken.None);

        var metrics = Written.Events(sink).OfType<ConversationEvent>().Single().Metrics;
        Assert.That((metrics.AgentCalls, metrics.SpecialistCalls, metrics.Turns, metrics.Seconds),
            Is.EqualTo(((int?)2, (int?)6, (int?)2, (double?)0)));
        Assert.That(metrics.Hosts, Is.EqualTo(new[] { "www.checkers.co.za" }));
        Assert.That((metrics.TokensIn, metrics.TokensOut, metrics.Cost),
            Is.EqualTo(((long?)null, (long?)null, (decimal?)null)));
    }

    /// <summary>Each of the eval's graders grades the trial, in order, and the outcome is written.</summary>
    [Test]
    public async Task TheConversationLineCarriesOneGradePerGraderInOrder_AndTheOutcome()
    {
        var eval = new CannedEval(graders: [new WithinTimeGrader(TimeSpan.FromMinutes(1)), new NoErrorGrader()]);

        var events = await RunAsync(OneAnswer.ToArray(), eval);

        var conversation = events.OfType<ConversationEvent>().Single();
        Assert.That(conversation.Grades.Select(g => (g.Grader, g.Outcome)),
            Is.EqualTo(new[] { ("within-time", GradeOutcome.Pass), ("no-error", GradeOutcome.Pass) }));
        Assert.That(conversation.Outcome, Is.EqualTo(TrialOutcome.Pass));
    }

    /// <summary>
    /// A text event whose text is a number is skipped instead of stopping the run, and the answer is exactly what the
    /// shared answer reader makes of the turn. The runner's own loop used to read the text as a string and throw.
    /// </summary>
    [Test]
    public async Task ADeltaWhoseTextIsANumberDoesNotStopTheRun_AndTheAnswerIsWhatAnswerOfGives()
    {
        SessionEvent[] turn =
        [
            SessionEvent.From("content_block_delta",
                Json("""{"type":"content_block_delta","index":0,"delta":{"type":"text_delta","text":42}}""")),
            SessionEvent.From("content_block_delta",
                Json("""{"type":"content_block_delta","index":0,"delta":{"type":"text_delta","text":" Ja "}}""")),
        ];

        var events = await RunAsync(turn);

        var answer = events.OfType<TurnEvent>().Single().Answer;
        Assert.That(answer,
            Is.EqualTo(EngineEvents.AnswerOf(new TurnTranscript(1, "sess_canned", "Gee my resep", turn, TimeSpan.Zero,
                null))));
        Assert.That(answer, Is.EqualTo("Ja"));
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
        /// <summary>The sessions closed, in order.</summary>
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

    /// <summary>The stub eval: one mode, and one scenario of as many messages as asked.</summary>
    private sealed class CannedEval(TurnFacts? facts = null, IReadOnlyList<IGrader>? graders = null, int messages = 1)
        : IEval
    {
        /// <inheritdoc />
        public string Name => "stub-cook";

        /// <inheritdoc />
        public int Version => 1;

        /// <inheritdoc />
        public string Proves => "A canned trial, to see the runner's lines.";

        /// <summary>The one mode, "cook".</summary>
        public ValueTask<IReadOnlyList<EvalMode>> ModesAsync(ISessionClient sessions,
            CancellationToken cancellationToken) =>
            ValueTask.FromResult<IReadOnlyList<EvalMode>>([
                new EvalMode("cook", SampleRole.Alpha, [SampleToolset.Alpha])
            ]);

        /// <inheritdoc />
        public IReadOnlyList<EvalScenario> Scenarios { get; } =
        [
            new EvalScenario("pannekoek",
                new EvalInput(null, "Africa/Johannesburg", [.. Enumerable.Repeat("Gee my resep", messages)]), default,
                [], DatasetSplit.Dev)
        ];

        /// <inheritdoc />
        public IReadOnlyList<IGrader> Graders { get; } = graders ?? [];

        /// <summary>The facts the test handed in, or none at all.</summary>
        public TurnFacts TurnFactsOf(TurnTranscript turn, EvalMode mode) =>
            facts ?? new TurnFacts(null, null, 0, 0, [], null);
    }
}
