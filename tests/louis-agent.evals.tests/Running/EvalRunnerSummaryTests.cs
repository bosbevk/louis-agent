using LouisAgent.Testing;
using System.Text.Json;
using LouisAgent.Evals.Api;
using LouisAgent.Evals.Datasets;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Protocol;
using LouisAgent.Evals.Protocol.Events.Run;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Protocol.Parts.Run;
using LouisAgent.Evals.Reports;
using LouisAgent.Evals.Running;
using LouisAgent.Evals.Tests.Protocol;
using LouisAgent.Evals.Trials;
using LouisAgent.SessionStream;
using Microsoft.Extensions.Logging.Abstractions;

namespace LouisAgent.Evals.Tests.Running;

/// <summary>
/// After a mode's last repeat the runner writes one summary per slice (overall, then each category); a re-score
/// writes them from its new grades and never copies a stored run's old ones. The cost is not on them: the API adds
/// it from the ledger (ADR 0007).
/// </summary>
public class EvalRunnerSummaryTests
{
    /// <summary>The time every line carries.</summary>
    private static readonly DateTimeOffset At = DateTimeOffset.FromUnixTimeMilliseconds(1700000000123);

    /// <summary>The one answer every turn gets: "Hallo".</summary>
    private static readonly IReadOnlyList<SessionEvent> Answer =
    [
        SessionEvent.From("content_block_delta",
            Json("""{"type":"content_block_delta","index":0,"delta":{"type":"text_delta","text":"Hallo"}}"""))
    ];

    /// <summary>A bit of JSON text as an element that outlives its document.</summary>
    private static JsonElement Json(string text) => JsonDocument.Parse(text).RootElement.Clone();

    /// <summary>Runs the eval over canned answers and hands back the lines it wrote.</summary>
    private static async Task<string[]> RunAsync(SummaryEval eval, int repeats = 1)
    {
        var sink = new StringWriter();
        var runner = new EvalRunner(new CannedSessionClient(Enumerable.Repeat(Answer, 24).ToArray()),
            new ProtocolWriter(sink, new FixedClock(At)), new FixedClock(At),
            NullLogger<EvalRunner>.Instance);

        await runner.RunAsync(eval, new EvalRun("run_1", repeats), CancellationToken.None);

        return Written.Lines(sink);
    }

    /// <summary>
    /// Re-scores the stored lines with the eval as it is today, and hands back the summaries it wrote.
    /// </summary>
    private static async Task<List<SummaryEvent>> RescoreAsync(SummaryEval eval, string[] stored)
    {
        var sink = new StringWriter();
        var runner = new EvalRunner(new NoSessionClient(), new ProtocolWriter(sink, new FixedClock(At)),
            new FixedClock(At), NullLogger<EvalRunner>.Instance);

        await runner.RescoreAsync(eval, "run_new", "run_old", stored, CancellationToken.None);

        return [.. Written.Events(sink).OfType<SummaryEvent>()];
    }

    /// <summary>The summaries of a run's lines.</summary>
    private static List<SummaryEvent> Summaries(IEnumerable<string> lines) =>
        [.. Written.Events(lines).OfType<SummaryEvent>()];

    /// <summary>One summary per slice comes after the mode's last end, the overall one first.</summary>
    [Test]
    public async Task AfterAModesLastModeDone_TheRunnerWritesOneSummaryPerSlice_OverallFirst()
    {
        var events = Written.Events(await RunAsync(new SummaryEval(), repeats: 2));

        int lastModeDone = events.ToList().FindLastIndex(e => e is ModeDoneEvent);
        var after = events.Skip(lastModeDone + 1).ToList();
        Assert.That(after, Has.All.InstanceOf<SummaryEvent>());
        Assert.That(after, Has.Count.EqualTo(3), "overall, then the two categories, after the mode's last end");
        Assert.That(events.Take(lastModeDone + 1).OfType<SummaryEvent>(), Is.Empty);
        var summaries = events.OfType<SummaryEvent>().ToList();
        Assert.That(summaries.Select(s => s.Slice), Is.EqualTo(new[] { "overall", "language:af", "shop:pnp" }));
        Assert.That(summaries.Select(s => s.Mode).Distinct(), Is.EqualTo(new[] { "cook" }));
    }

    /// <summary>The overall slice counts every trial of every repeat, and has rates from five trials.</summary>
    [Test]
    public async Task TheOverallLine_CountsEveryTrialOfEveryRepeat_AndSaysTooFewBelowFive()
    {
        var overall = Summaries(await RunAsync(new SummaryEval(), repeats: 2)).First();

        Assert.That((overall.N, overall.Passed, overall.Failed, overall.TooFew, overall.PassRate),
            Is.EqualTo((6, 6, 0, false, (double?)1.0)));
        Assert.That(overall.Tasks, Is.EqualTo(new[] { "a", "b", "c" }));
    }

    /// <summary>A category of fewer than five trials says so and has no rates, never 0.</summary>
    [Test]
    public async Task ACategoryOfFewerThanFiveTrials_IsTooFew_AndHasNoRates()
    {
        var shop = Summaries(await RunAsync(new SummaryEval(), repeats: 1)).Single(s => s.Slice == "shop:pnp");

        Assert.That((shop.TooFew, shop.N), Is.EqualTo((true, 2)));
        Assert.That((shop.PassRate, shop.PassAtK, shop.PassHatK), Is.EqualTo(((double?)null, (double?)null,
            (double?)null)), "null, never 0");
    }

    /// <summary>A slice carries each grader's counts and mean score, and the counts per kind of grader.</summary>
    [Test]
    public async Task TheLineCarriesTheGraderAndKindCounts_AndEachGradersMeanScore()
    {
        var overall = Summaries(await RunAsync(new SummaryEval())).Single(s => s.Slice == "overall");

        Assert.That(overall.Graders.Single(),
            Is.EqualTo(new GraderCount("wants-text", GraderKind.FixedRule, 3, 0, 0, 1)));
        Assert.That(overall.Kinds.Single(), Is.EqualTo(new KindCount(GraderKind.FixedRule, 3, 0, 0)));
    }

    /// <summary>A re-score writes its summaries from its new grades, and never the stored run's own.</summary>
    [Test]
    public async Task AReScore_WritesItsSummaryFromTheNewGrades_AndNeverCopiesTheStoredRunsOwn()
    {
        string[] stored = await RunAsync(new SummaryEval());

        var summaries = await RescoreAsync(new SummaryEval(wanted: "Totsiens"), stored);

        Assert.That(summaries, Has.Count.EqualTo(3), "one per slice, not the stored run's three as well");
        Assert.That((summaries[0].Passed, summaries[0].Failed), Is.EqualTo((0, 3)),
            "today's grader, the old transcripts");
        Assert.That(Summaries(stored), Has.Count.EqualTo(3), "the stored run's own lines are untouched");
    }

    /// <summary>
    /// A re-scored scenario the dataset no longer has is in no category, so only in the overall slice.
    /// </summary>
    [Test]
    public async Task AReScoredScenarioTheDatasetNoLongerHas_HasNoCategory_SoItIsOnlyInTheOverallLine()
    {
        string[] stored = await RunAsync(new SummaryEval());

        var summaries = (await RescoreAsync(new SummaryEval(scenarios: ["a", "b"]), stored))
            .ToDictionary(s => s.Slice);

        Assert.That(summaries["overall"].N, Is.EqualTo(3));
        Assert.That(summaries["overall"].Unknown, Is.EqualTo(1), "c is Unknown: its scenario is gone");
        Assert.That(summaries["language:af"].N, Is.EqualTo(2), "c is in no category now");
    }

    /// <summary>A slice of too few trials goes out with its rates null and its name as it is.</summary>
    [Test]
    public void TheProtocolWriter_WritesATooFewSlice_WithNoRates_AndItsNameAsItIs()
    {
        var sink = new StringWriter();
        var writer = new ProtocolWriter(sink, new FixedClock(At));

        writer.Summary(new SummaryLine("cook", "café", 3, 3, 0, 0, 0, ["c"], TooFew: true, null, null, null, [], []));

        var few = Written.Events(sink).OfType<SummaryEvent>().Single();
        Assert.That((few.Slice, few.TooFew, few.PassRate), Is.EqualTo(("café", true, (double?)null)));
    }

    /// <summary>
    /// An eval of scenarios a, b and c (b and c named in categories), one mode, one grader (the answer holds the
    /// wanted text) and one numeric score.
    /// </summary>
    private sealed class SummaryEval(string wanted = "Hallo", string[]? scenarios = null) : IEval
    {
        /// <summary>Each scenario's categories.</summary>
        private static readonly Dictionary<string, string[]> Categories = new()
        {
            ["a"] = ["language:af"],
            ["b"] = ["language:af", "shop:pnp"],
            ["c"] = ["shop:pnp"]
        };

        /// <inheritdoc />
        public string Name => "summary-cook";

        /// <inheritdoc />
        public int Version => 1;

        /// <inheritdoc />
        public string Proves => "A canned run, to roll up.";

        /// <summary>One mode.</summary>
        public ValueTask<IReadOnlyList<EvalMode>> ModesAsync(ISessionClient sessions,
            CancellationToken cancellationToken) =>
            ValueTask.FromResult<IReadOnlyList<EvalMode>>([
                new EvalMode("cook", SampleRole.Alpha, [SampleToolset.Alpha])
            ]);

        /// <inheritdoc />
        public IReadOnlyList<EvalScenario> Scenarios { get; } = (scenarios ?? ["a", "b", "c"])
            .Select(id => new EvalScenario(id, new EvalInput(null, "Africa/Johannesburg", ["Gee my resep"]), default,
                Categories[id], DatasetSplit.Dev)).ToList();

        /// <inheritdoc />
        public IReadOnlyList<IGrader> Graders { get; } = [new WantsText(wanted)];

        /// <summary>How many turns the trial had.</summary>
        public EvalScores ScoresOf(Trial trial, IReadOnlyList<Grade> grades) =>
            new Scores(trial.Transcript.Turns.Count);

        /// <summary>No facts about a turn.</summary>
        public TurnFacts TurnFactsOf(TurnTranscript turn, EvalMode mode) => new(null, null, 0, 0, [], null);
    }

    /// <summary>A grader that passes when every answer holds the wanted text.</summary>
    private sealed class WantsText(string wanted) : IGrader
    {
        /// <inheritdoc />
        public string Name => "wants-text";

        /// <inheritdoc />
        public int Version => 1;

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

    /// <summary>The eval's one score: how many turns.</summary>
    private sealed record Scores(int Turns) : EvalScores;
}
