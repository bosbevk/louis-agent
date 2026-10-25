using System.Runtime.CompilerServices;
using System.Text.Json;
using LouisAgent.Evals.Api;
using LouisAgent.Evals.Datasets;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Protocol;
using LouisAgent.Evals.Running;
using LouisAgent.Evals.Trials;
using LouisAgent.SessionStream;
using Microsoft.Extensions.Logging.Abstractions;

namespace LouisAgent.Evals.Tests.Running;

// F18 step 5b: re-scoring a stored run for $0. The stored run's lines go in, today's graders grade its transcripts, and a new run's lines come out
// that name the one they re-scored. Evals as regression tests over recorded behaviour: the transcripts are the fixed input, the graders the code
// under test. Written first: EvalRunner.RescoreAsync did not exist on the red run.
public class EvalRunnerRescoreTests
{
    private static readonly DateTimeOffset At = DateTimeOffset.FromUnixTimeMilliseconds(1700000000123);

    private static readonly IReadOnlyList<SessionEvent> Answer =
        [SessionEvent.From("content_block_delta", Json("""{"type":"content_block_delta","index":0,"delta":{"type":"text_delta","text":"Hallo"}}"""))];

    /// <summary>Runs <paramref name="eval"/> against a canned client and returns the lines it wrote: the stored run a re-score reads.</summary>
    private static async Task<string[]> StoredRunAsync(RescoreEval eval, int repeats = 1, ISessionClient? client = null)
    {
        var sink = new StringWriter();
        var runner = new EvalRunner(client ?? new CannedSessionClient(Enumerable.Repeat(Answer, 8).ToArray()),
            new ProtocolWriter(sink, new FixedClock(At)), new FixedClock(At), NullLogger<EvalRunner>.Instance);

        await runner.RunAsync(eval, new EvalRun("run_old", repeats), CancellationToken.None);
        return Lines(sink);
    }

    /// <summary>Re-scores <paramref name="stored"/> with <paramref name="eval"/> as it is today; the runner is given a client that throws if it is ever called.</summary>
    private static async Task<string[]> RescoreAsync(RescoreEval eval, string[] stored)
    {
        var sink = new StringWriter();
        var runner = new EvalRunner(new NoCallsClient(), new ProtocolWriter(sink, new FixedClock(At)), new FixedClock(At), NullLogger<EvalRunner>.Instance);

        await runner.RescoreAsync(eval, "run_new", "run_old", stored, CancellationToken.None);
        return Lines(sink);
    }

    [Test]
    public async Task SameGraders_GiveTheSameGradesPerConversation_AndTheSameRates()
    {
        string[] stored = await StoredRunAsync(new RescoreEval(), repeats: 2);

        string[] rescored = await RescoreAsync(new RescoreEval(), stored);

        Assert.That(Events(rescored, "conversation").Select(Grades), Is.EqualTo(Events(stored, "conversation").Select(Grades)));
        Assert.That(Events(rescored, "mode_done").Select(Rates), Is.EqualTo(Events(stored, "mode_done").Select(Rates)));
        Assert.That(Events(rescored, "conversation").Select(Outcome), Is.EqualTo(Events(stored, "conversation").Select(Outcome)));
    }

    [Test]
    public async Task AGradersBumpedVersion_IsOnTheNewGrades_AndTheStoredLinesAreNotTouched()
    {
        string[] stored = await StoredRunAsync(new RescoreEval());
        string[] before = [.. stored];

        string[] rescored = await RescoreAsync(new RescoreEval(graderVersion: 2, wanted: "Totsiens"), stored);

        var grade = Events(rescored, "conversation").First().GetProperty("grades")[0];
        Assert.That(grade.GetProperty("version").GetInt32(), Is.EqualTo(2));
        Assert.That(grade.GetProperty("outcome").GetString(), Is.EqualTo("Fail"), "today's rule, the old transcript");
        Assert.That(Events(stored, "conversation").First().GetProperty("grades")[0].GetProperty("outcome").GetString(), Is.EqualTo("Pass"));
        Assert.That(stored, Is.EqualTo(before));
    }

    [Test]
    public async Task NoSessionIsOpenedAndNoModelIsCalled()
    {
        string[] stored = await StoredRunAsync(new RescoreEval());
        var client = new NoCallsClient();
        var runner = new EvalRunner(client, new ProtocolWriter(new StringWriter(), new FixedClock(At)), new FixedClock(At), NullLogger<EvalRunner>.Instance);

        await runner.RescoreAsync(new RescoreEval(), "run_new", "run_old", stored, CancellationToken.None);

        Assert.That(client.Calls, Is.Zero);
    }

    [Test]
    public async Task TheBeginLineNamesTheRunItRescored_AndTheEvalAndDatasetAsTheyAreNow()
    {
        string[] stored = await StoredRunAsync(new RescoreEval());
        var today = new RescoreEval(version: 4, graderVersion: 2, dataset: new DatasetManifest("cook", 7, "cooking", "abc123", []));

        string[] rescored = await RescoreAsync(today, stored);

        var begin = Events(rescored, "protocol").First();
        Assert.That(begin.GetProperty("rescore_of").GetString(), Is.EqualTo("run_old"));
        Assert.That(begin.GetProperty("eval").GetString(), Is.EqualTo("rescore-cook"));
        Assert.That(begin.GetProperty("eval_version").GetInt32(), Is.EqualTo(4));
        Assert.That(begin.GetProperty("graders")[0].GetProperty("version").GetInt32(), Is.EqualTo(2));
        var dataset = begin.GetProperty("dataset");
        Assert.That((dataset.GetProperty("version").GetInt32(), dataset.GetProperty("hash").GetString()), Is.EqualTo((7, "abc123")));
        Assert.That(Events(stored, "protocol").First().TryGetProperty("rescore_of", out _), Is.False, "a run that is no re-score names nothing");
    }

    [Test]
    public async Task TheTurnsAreCopiedAsTheyWere_SoTheNewRunCanBeExploredLikeTheOld()
    {
        string[] stored = await StoredRunAsync(new RescoreEval(), repeats: 2);

        string[] rescored = await RescoreAsync(new RescoreEval(), stored);

        foreach (string kind in new[] { "mode", "question", "sse", "turn" })
            Assert.That(Events(rescored, kind).Select(e => e.GetRawText()), Is.EqualTo(Events(stored, kind).Select(e => e.GetRawText())), kind);
        Assert.That(Events(rescored, "mode_done"), Has.Count.EqualTo(2), "the repeats come from the stored run");
    }

    [Test]
    public async Task AScenarioTheDatasetNoLongerHas_IsUnknownWithAReason_NeverACrashOrAFail()
    {
        string[] stored = await StoredRunAsync(new RescoreEval());

        string[] rescored = await RescoreAsync(new RescoreEval(scenarios: ["pannekoek"]), stored);

        var conversations = Events(rescored, "conversation").ToList();
        Assert.That(conversations, Has.Count.EqualTo(2));
        var gone = conversations.Single(c => c.GetProperty("task").GetString() == "koeksister");
        var grade = gone.GetProperty("grades")[0];
        Assert.That(grade.GetProperty("outcome").GetString(), Is.EqualTo("Unknown"));
        Assert.That(grade.GetProperty("reason").GetString(), Does.Contain("koeksister"));
        Assert.That(gone.GetProperty("outcome").GetString(), Is.EqualTo("Unknown"));
        Assert.That(conversations.Single(c => c.GetProperty("task").GetString() == "pannekoek").GetProperty("outcome").GetString(), Is.EqualTo("Pass"));
    }

    [Test]
    public async Task AnInfrastructureFailedTrial_StaysOne_LeftOutOfKAndCounted()
    {
        string[] stored = await StoredRunAsync(new RescoreEval(scenarios: ["pannekoek"]), client: new TimingOutClient());

        string[] rescored = await RescoreAsync(new RescoreEval(scenarios: ["pannekoek"]), stored);

        Assert.That(Events(rescored, "conversation").Single().GetProperty("outcome").GetString(), Is.EqualTo("Infrastructure"));
        var done = Events(rescored, "mode_done").Single();
        Assert.That(done.GetProperty("infrastructure").GetInt32(), Is.EqualTo(1));
        Assert.That(done.TryGetProperty("pass_at_k", out _), Is.False, "no counted trial, so no rate: never 0");
    }

    private static string[] Lines(StringWriter sink) => sink.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries);

    private static List<JsonElement> Events(IEnumerable<string> lines, string kind) =>
        lines.Select(line => JsonDocument.Parse(line.TrimStart('@')).RootElement.Clone())
            .Where(e => e.GetProperty("event").GetString() == kind).ToList();

    private static string Grades(JsonElement conversation) => conversation.GetProperty("grades").GetRawText();

    private static string Outcome(JsonElement conversation) => conversation.GetProperty("outcome").GetString()!;

    private static (double?, double?) Rates(JsonElement done) =>
        (done.TryGetProperty("pass_at_k", out var at) ? at.GetDouble() : null, done.TryGetProperty("pass_hat_k", out var hat) ? hat.GetDouble() : null);

    private static JsonElement Json(string text) => JsonDocument.Parse(text).RootElement.Clone();

    /// <summary>A client that counts every call and throws: a re-score must never reach it.</summary>
    private sealed class NoCallsClient : ISessionClient
    {
        public int Calls { get; private set; }

        public Task<IReadOnlyList<string>> ToolsetsAsync(CancellationToken cancellationToken) => throw Called();

        public Task<string> OpenAsync(SessionRequest request, CancellationToken cancellationToken) => throw Called();

        public IAsyncEnumerable<SessionEvent> SendAsync(string session, string message, CancellationToken cancellationToken) => throw Called();

        public Task CloseAsync(string session, CancellationToken cancellationToken) => throw Called();

        private InvalidOperationException Called()
        {
            Calls++;
            return new InvalidOperationException("a re-score opened a session");
        }
    }

    /// <summary>A client whose every turn times out, as a dead connection does: the stored run has an infrastructure failure to keep.</summary>
    private sealed class TimingOutClient : ISessionClient
    {
        public Task<IReadOnlyList<string>> ToolsetsAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<string>>([]);

        public Task<string> OpenAsync(SessionRequest request, CancellationToken cancellationToken) => Task.FromResult("sess_timeout");

        public async IAsyncEnumerable<SessionEvent> SendAsync(string session, string message, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            throw new TimeoutException("the API did not answer");
#pragma warning disable CS0162
            yield break;
#pragma warning restore CS0162
        }

        public Task CloseAsync(string session, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    /// <summary>A grader whose rule and version a test sets: the answer holds the wanted text.</summary>
    private sealed class WantsText(int version, string wanted) : IGrader
    {
        public string Name => "wants-text";
        public int Version => version;
        public GraderKind Kind => GraderKind.FixedRule;
        public IReadOnlySet<string> ExpectedKeys { get; } = new HashSet<string>();
        public bool Required => true;

        public ValueTask<Grade> GradeAsync(Trial trial, CancellationToken cancellationToken)
        {
            bool held = trial.Transcript.Turns.All(turn => EngineEvents.AnswerOf(turn)?.Contains(wanted, StringComparison.Ordinal) == true);
            return ValueTask.FromResult(new Grade(Name, Version, held ? 1 : 0, held ? GradeOutcome.Pass : GradeOutcome.Fail, held ? "held" : "missing"));
        }
    }

    /// <summary>An eval with two scenarios (or the ones given), one mode and one grader, the shape of the eval as it was or as it is today.</summary>
    private sealed class RescoreEval(int version = 1, int graderVersion = 1, string wanted = "Hallo", string[]? scenarios = null,
        DatasetManifest? dataset = null) : IEval
    {
        public string Name => "rescore-cook";
        public string? Identity => null;
        public int Version => version;
        public string Proves => "A canned trial, to re-score.";
        public DatasetManifest? Dataset => dataset;

        public ValueTask<IReadOnlyList<EvalMode>> ModesAsync(ISessionClient sessions, CancellationToken cancellationToken) =>
            ValueTask.FromResult<IReadOnlyList<EvalMode>>([new EvalMode("cook", "puls-cook", ["web"])]);

        public IReadOnlyList<EvalScenario> Scenarios { get; } = (scenarios ?? ["pannekoek", "koeksister"])
            .Select(id => new EvalScenario(id, new EvalInput(null, "Africa/Johannesburg", ["Gee my resep"]), default, [], DatasetSplit.Dev)).ToList();

        public IReadOnlyList<IGrader> Graders { get; } = [new WantsText(graderVersion, wanted)];

        public EvalScores ScoresOf(Trial trial, IReadOnlyList<Grade> grades) => new Scores(trial.Transcript.Turns.Count);

        public TurnFacts TurnFactsOf(TurnTranscript turn, EvalMode mode) => new(null, null, 0, 0, [], null);
    }

    private sealed record Scores(int Turns) : EvalScores;
}
