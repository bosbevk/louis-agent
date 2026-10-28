using LouisAgent.Testing;
using System.Runtime.CompilerServices;
using System.Text.Json;
using LouisAgent.Evals.Api;
using LouisAgent.Evals.Datasets;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Protocol;
using LouisAgent.Evals.Protocol.Events.Conversations;
using LouisAgent.Evals.Protocol.Events.Run;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Protocol.Parts.Run;
using LouisAgent.Evals.Running;
using LouisAgent.Evals.Tests.Protocol;
using LouisAgent.Evals.Trials;
using LouisAgent.SessionStream;
using Microsoft.Extensions.Logging.Abstractions;

namespace LouisAgent.Evals.Tests.Running;

/// <summary>
/// Re-scoring a stored run for $0: the stored run's lines go in, today's graders grade its transcripts, and a new
/// run's lines come out that name the one they re-scored. Evals as regression tests over recorded behaviour: the
/// transcripts are the fixed input, the graders the code under test.
/// </summary>
public class EvalRunnerRescoreTests
{
    /// <summary>The time every line carries.</summary>
    private static readonly DateTimeOffset At = DateTimeOffset.FromUnixTimeMilliseconds(1700000000123);

    /// <summary>The one answer every turn gets: "Hallo".</summary>
    private static readonly IReadOnlyList<SessionEvent> Answer =
    [
        SessionEvent.From("content_block_delta",
            Json("""{"type":"content_block_delta","index":0,"delta":{"type":"text_delta","text":"Hallo"}}"""))
    ];

    /// <summary>
    /// Runs <paramref name="eval"/> against a canned client and returns the lines it wrote: the stored run a re-score
    /// reads.
    /// </summary>
    private static async Task<string[]> StoredRunAsync(RescoreEval eval, int repeats = 1, ISessionClient? client = null)
    {
        var sink = new StringWriter();
        var runner = new EvalRunner(client ?? new CannedSessionClient(Enumerable.Repeat(Answer, 8).ToArray()),
            new ProtocolWriter(sink, new FixedClock(At)), new FixedClock(At), NullLogger<EvalRunner>.Instance);

        await runner.RunAsync(eval, new EvalRun("run_old", repeats), CancellationToken.None);

        return Written.Lines(sink);
    }

    /// <summary>
    /// Re-scores <paramref name="stored"/> with <paramref name="eval"/> as it is today; the runner is given a client
    /// that throws if it is ever called.
    /// </summary>
    private static async Task<string[]> RescoreAsync(RescoreEval eval, string[] stored)
    {
        var sink = new StringWriter();
        var runner = new EvalRunner(new NoCallsClient(), new ProtocolWriter(sink, new FixedClock(At)),
            new FixedClock(At), NullLogger<EvalRunner>.Instance);

        await runner.RescoreAsync(eval, "run_new", "run_old", stored, CancellationToken.None);

        return Written.Lines(sink);
    }

    /// <summary>The events of one kind among the lines.</summary>
    private static List<T> Of<T>(IEnumerable<string> lines) where T : EvalEvent =>
        [.. Written.Events(lines).OfType<T>()];

    /// <summary>The same graders give the same grades, rates and outcomes as the run did.</summary>
    [Test]
    public async Task SameGraders_GiveTheSameGradesPerConversation_AndTheSameRates()
    {
        string[] stored = await StoredRunAsync(new RescoreEval(), repeats: 2);

        string[] rescored = await RescoreAsync(new RescoreEval(), stored);

        Assert.That(Of<ConversationEvent>(rescored).SelectMany(c => c.Grades),
            Is.EqualTo(Of<ConversationEvent>(stored).SelectMany(c => c.Grades)));
        Assert.That(Of<ModeDoneEvent>(rescored).Select(d => d.Rates),
            Is.EqualTo(Of<ModeDoneEvent>(stored).Select(d => d.Rates)));
        Assert.That(Of<ConversationEvent>(rescored).Select(c => c.Outcome),
            Is.EqualTo(Of<ConversationEvent>(stored).Select(c => c.Outcome)));
    }

    /// <summary>A grader's new version is on the new grades, and the stored lines are not touched.</summary>
    [Test]
    public async Task AGradersBumpedVersion_IsOnTheNewGrades_AndTheStoredLinesAreNotTouched()
    {
        string[] stored = await StoredRunAsync(new RescoreEval());
        string[] before = [.. stored];

        string[] rescored = await RescoreAsync(new RescoreEval(graderVersion: 2, wanted: "Totsiens"), stored);

        var grade = Of<ConversationEvent>(rescored).First().Grades[0];
        Assert.That((grade.Version, grade.Outcome), Is.EqualTo((2, GradeOutcome.Fail)),
            "today's rule, the old transcript");
        Assert.That(Of<ConversationEvent>(stored).First().Grades[0].Outcome, Is.EqualTo(GradeOutcome.Pass));
        Assert.That(stored, Is.EqualTo(before));
    }

    /// <summary>A re-score opens no session and calls no model.</summary>
    [Test]
    public async Task NoSessionIsOpenedAndNoModelIsCalled()
    {
        string[] stored = await StoredRunAsync(new RescoreEval());
        var client = new NoCallsClient();
        var runner = new EvalRunner(client, new ProtocolWriter(new StringWriter(), new FixedClock(At)),
            new FixedClock(At), NullLogger<EvalRunner>.Instance);

        await runner.RescoreAsync(new RescoreEval(), "run_new", "run_old", stored, CancellationToken.None);

        Assert.That(client.Calls, Is.Zero);
    }

    /// <summary>The first line names the run it re-scored, and the eval and its dataset as they are now.</summary>
    [Test]
    public async Task TheBeginLineNamesTheRunItRescored_AndTheEvalAndDatasetAsTheyAreNow()
    {
        string[] stored = await StoredRunAsync(new RescoreEval());
        var today = new RescoreEval(version: 4, graderVersion: 2,
            dataset: new DatasetManifest("cook", 7, "cooking", "abc123", []));

        string[] rescored = await RescoreAsync(today, stored);

        var begin = Of<ProtocolEvent>(rescored).Single();
        Assert.That((begin.RescoreOf, begin.Eval, begin.EvalVersion), Is.EqualTo(("run_old", "rescore-cook", 4)));
        Assert.That(begin.Graders[0].Version, Is.EqualTo(2));
        Assert.That(begin.Dataset, Is.EqualTo(new DatasetInfo("cook", 7, "abc123")));
        Assert.That(Of<ProtocolEvent>(stored).Single().RescoreOf, Is.Null, "a run that is no re-score names nothing");
    }

    /// <summary>
    /// The mode, question, stream and turn lines are copied as they were, so the new run can be explored.
    /// </summary>
    [Test]
    public async Task TheTurnsAreCopiedAsTheyWere_SoTheNewRunCanBeExploredLikeTheOld()
    {
        string[] stored = await StoredRunAsync(new RescoreEval(), repeats: 2);

        string[] rescored = await RescoreAsync(new RescoreEval(), stored);

        // A copied line is the stored line, byte for byte.
        string[] Copied(string[] lines) =>
        [
            .. lines.Where(line => EvalProtocolJson.Read(line)
                is ModeEvent or QuestionEvent or SseEvent or TurnEvent)
        ];

        Assert.That(Copied(rescored), Is.EqualTo(Copied(stored)));
        Assert.That(Of<ModeDoneEvent>(rescored), Has.Count.EqualTo(2), "the repeats come from the stored run");
    }

    /// <summary>A scenario the dataset no longer has is Unknown with a reason, never a crash or a fail.</summary>
    [Test]
    public async Task AScenarioTheDatasetNoLongerHas_IsUnknownWithAReason_NeverACrashOrAFail()
    {
        string[] stored = await StoredRunAsync(new RescoreEval());

        string[] rescored = await RescoreAsync(new RescoreEval(scenarios: ["pannekoek"]), stored);

        var conversations = Of<ConversationEvent>(rescored);
        Assert.That(conversations, Has.Count.EqualTo(2));
        var gone = conversations.Single(c => c.Task == "koeksister");
        Assert.That(gone.Grades[0].Outcome, Is.EqualTo(GradeOutcome.Unknown));
        Assert.That(gone.Grades[0].Reason, Does.Contain("koeksister"));
        Assert.That(gone.Outcome, Is.EqualTo(TrialOutcome.Unknown));
        Assert.That(conversations.Single(c => c.Task == "pannekoek").Outcome, Is.EqualTo(TrialOutcome.Pass));
    }

    /// <summary>A trial that failed for the infrastructure stays one: left out of k, and counted.</summary>
    [Test]
    public async Task AnInfrastructureFailedTrial_StaysOne_LeftOutOfKAndCounted()
    {
        string[] stored =
            await StoredRunAsync(new RescoreEval(scenarios: ["pannekoek"]), client: new TimingOutClient());

        string[] rescored = await RescoreAsync(new RescoreEval(scenarios: ["pannekoek"]), stored);

        Assert.That(Of<ConversationEvent>(rescored).Single().Outcome, Is.EqualTo(TrialOutcome.Infrastructure));
        var rates = Of<ModeDoneEvent>(rescored).Single().Rates!;
        Assert.That(rates.Infrastructure, Is.EqualTo(1));
        Assert.That(rates.PassAtK, Is.Null, "no counted trial, so no rate: never 0");
    }

    /// <summary>
    /// The retries the stored run recorded carry over: the re-score runs no trial, so it retries none.
    /// </summary>
    [Test]
    public async Task ATrialsRetryCount_CarriesOverFromTheStoredRun()
    {
        // The stored run says it retried one trial.
        string[] stored =
        [
            .. (await StoredRunAsync(new RescoreEval())).Select(line =>
                EvalProtocolJson.Read(line) is ModeDoneEvent { Rates: { } rates } done
                    ? EvalProtocolJson.Line(done with { Rates = rates with { Retried = 1 } })
                    : line)
        ];

        string[] rescored = await RescoreAsync(new RescoreEval(), stored);

        Assert.That(Of<ModeDoneEvent>(rescored).Single().Rates!.Retried, Is.EqualTo(1));
    }

    /// <summary>
    /// A re-score measures the stored turns again the way the run did, so its metrics are the run's.
    /// </summary>
    [Test]
    public async Task TheMetrics_AreMeasuredAgain_FromTheStoredTurns()
    {
        string[] stored = await StoredRunAsync(new RescoreEval(scenarios: ["pannekoek"]));

        string[] rescored = await RescoreAsync(new RescoreEval(scenarios: ["pannekoek"]), stored);

        var before = Of<ConversationEvent>(stored).Single().Metrics;
        var after = Of<ConversationEvent>(rescored).Single().Metrics;
        Assert.That((after.AgentCalls, after.SpecialistCalls, after.Turns, after.Seconds),
            Is.EqualTo((before.AgentCalls, before.SpecialistCalls, before.Turns, before.Seconds)));
        Assert.That(after.Hosts, Is.EqualTo(before.Hosts));
        Assert.That((after.AgentCalls, after.SpecialistCalls, after.Turns), Is.EqualTo(((int?)1, (int?)2, (int?)1)));
    }

    /// <summary>A bit of JSON text as an element that outlives its document.</summary>
    private static JsonElement Json(string text) => JsonDocument.Parse(text).RootElement.Clone();

    /// <summary>A client that counts every call and throws: a re-score must never reach it.</summary>
    private sealed class NoCallsClient : ISessionClient
    {
        /// <summary>How many times it was called.</summary>
        public int Calls { get; private set; }

        /// <summary>Throws: a re-score never asks.</summary>
        public Task<IReadOnlyList<string>> ToolsetsAsync(CancellationToken cancellationToken) => throw Called();

        /// <summary>Throws: a re-score never opens a session.</summary>
        public Task<string> OpenAsync(SessionRequest request, CancellationToken cancellationToken) => throw Called();

        /// <summary>Throws: a re-score never sends.</summary>
        public IAsyncEnumerable<SessionEvent> SendAsync(string session, string message,
            CancellationToken cancellationToken) => throw Called();

        /// <summary>Throws: a re-score never closes a session.</summary>
        public Task CloseAsync(string session, CancellationToken cancellationToken) => throw Called();

        /// <summary>Counts the call, and the error to throw.</summary>
        private InvalidOperationException Called()
        {
            Calls++;
            return new InvalidOperationException("a re-score opened a session");
        }
    }

    /// <summary>
    /// A client whose every turn times out, as a dead connection does: the stored run has an infrastructure failure
    /// to keep.
    /// </summary>
    private sealed class TimingOutClient : ISessionClient
    {
        /// <summary>No toolsets.</summary>
        public Task<IReadOnlyList<string>> ToolsetsAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<string>>([]);

        /// <summary>Opens the one session.</summary>
        public Task<string> OpenAsync(SessionRequest request, CancellationToken cancellationToken) =>
            Task.FromResult("sess_timeout");

        /// <summary>Times out before any event.</summary>
        public async IAsyncEnumerable<SessionEvent> SendAsync(string session, string message,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            throw new TimeoutException("the API did not answer");
#pragma warning disable CS0162
            yield break;
#pragma warning restore CS0162
        }

        /// <summary>Nothing to close.</summary>
        public Task CloseAsync(string session, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    /// <summary>A grader whose rule and version a test sets: the answer holds the wanted text.</summary>
    private sealed class WantsText(int version, string wanted) : IGrader
    {
        /// <inheritdoc />
        public string Name => "wants-text";

        /// <inheritdoc />
        public int Version => version;

        /// <inheritdoc />
        public GraderKind Kind => GraderKind.FixedRule;

        /// <inheritdoc />
        public IReadOnlySet<string> ExpectedKeys { get; } = new HashSet<string>();

        /// <inheritdoc />
        public bool Required => true;

        /// <summary>Passes when every turn's answer holds the wanted text.</summary>
        public ValueTask<Grade> GradeAsync(Trial trial, CancellationToken cancellationToken)
        {
            bool held = trial.Transcript.Turns.All(turn =>
                EngineEvents.AnswerOf(turn)?.Contains(wanted, StringComparison.Ordinal) == true);
            return ValueTask.FromResult(new Grade(Name, Version, held ? 1 : 0,
                held ? GradeOutcome.Pass : GradeOutcome.Fail, held ? "held" : "missing"));
        }
    }

    /// <summary>
    /// An eval with two scenarios (or the ones given), one mode and one grader, the shape of the eval as it was or as
    /// it is today.
    /// </summary>
    private sealed class RescoreEval(
        int version = 1,
        int graderVersion = 1,
        string wanted = "Hallo",
        string[]? scenarios = null,
        DatasetManifest? dataset = null) : IEval
    {
        /// <inheritdoc />
        public string Name => "rescore-cook";

        /// <inheritdoc />
        public int Version => version;

        /// <inheritdoc />
        public string Proves => "A canned trial, to re-score.";

        /// <inheritdoc />
        public DatasetManifest? Dataset => dataset;

        /// <summary>One mode.</summary>
        public ValueTask<IReadOnlyList<EvalMode>> ModesAsync(ISessionClient sessions,
            CancellationToken cancellationToken) =>
            ValueTask.FromResult<IReadOnlyList<EvalMode>>([
                new EvalMode("cook", SampleRole.Alpha, [SampleToolset.Alpha])
            ]);

        /// <inheritdoc />
        public IReadOnlyList<EvalScenario> Scenarios { get; } = (scenarios ?? ["pannekoek", "koeksister"])
            .Select(id => new EvalScenario(id, new EvalInput(null, "Africa/Johannesburg", ["Gee my resep"]), default,
                [], DatasetSplit.Dev)).ToList();

        /// <inheritdoc />
        public IReadOnlyList<IGrader> Graders { get; } = [new WantsText(graderVersion, wanted)];

        /// <summary>One call of the agent's and two of a specialist's, reading one site, on every turn.</summary>
        public TurnFacts TurnFactsOf(TurnTranscript turn, EvalMode mode) =>
            new(null, null, 1, 2, ["example.org"], null);
    }
}
