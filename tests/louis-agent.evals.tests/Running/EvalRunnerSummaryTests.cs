using LouisAgent.Testing;
using System.Text.Json;
using LouisAgent.Evals.Api;
using LouisAgent.Evals.Datasets;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Protocol;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Reports;
using LouisAgent.Evals.Running;
using LouisAgent.Evals.Trials;
using LouisAgent.SessionStream;
using Microsoft.Extensions.Logging.Abstractions;

namespace LouisAgent.Evals.Tests.Running;

// F18 step 5c: after a mode's last mode_done the runner writes one `summary` line per slice (overall, then each category); a re-score writes them from its new grades and never
// copies a stored run's old ones. The line's fields are the runner's; the API adds the cost (ADR 0007). Written first: the runner wrote no summary line.
public class EvalRunnerSummaryTests
{
    private static readonly DateTimeOffset At = DateTimeOffset.FromUnixTimeMilliseconds(1700000000123);

    private static readonly IReadOnlyList<SessionEvent> Answer =
    [
        SessionEvent.From("content_block_delta",
            Json("""{"type":"content_block_delta","index":0,"delta":{"type":"text_delta","text":"Hallo"}}"""))
    ];

    private static JsonElement Json(string text) => JsonDocument.Parse(text).RootElement.Clone();

    private static async Task<string[]> RunAsync(SummaryEval eval, int repeats = 1)
    {
        var sink = new StringWriter();
        var runner = new EvalRunner(new CannedSessionClient(Enumerable.Repeat(Answer, 24).ToArray()),
            new ProtocolWriter(sink, new FixedClock(At)), new FixedClock(At),
            NullLogger<EvalRunner>.Instance);
        await runner.RunAsync(eval, new EvalRun("run_1", repeats), CancellationToken.None);
        return Lines(sink);
    }

    private static string[] Lines(StringWriter sink) =>
        sink.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries);

    private static List<JsonElement> Events(IEnumerable<string> lines) =>
        lines.Select(line => JsonDocument.Parse(line.TrimStart('@')).RootElement.Clone()).ToList();

    private static string Kind(JsonElement line) => line.GetProperty("event").GetString()!;

    [Test]
    public async Task AfterAModesLastModeDone_TheRunnerWritesOneSummaryLinePerSlice_OverallFirst()
    {
        var lines = Events(await RunAsync(new SummaryEval(), repeats: 2));

        var kinds = lines.Select(Kind).ToList();
        int lastModeDone = kinds.LastIndexOf("mode_done");
        Assert.That(kinds.Skip(lastModeDone + 1), Is.EqualTo(new[] { "summary", "summary", "summary" }),
            "overall, then the two categories, after the mode's last mode_done");
        Assert.That(kinds.Take(lastModeDone + 1), Does.Not.Contain("summary"));
        var summaries = lines.Where(l => Kind(l) == "summary").ToList();
        Assert.That(summaries.Select(l => l.GetProperty("slice").GetString()),
            Is.EqualTo(new[] { "overall", "language:af", "shop:pnp" }));
        Assert.That(summaries.Select(l => l.GetProperty("mode").GetString()).Distinct(), Is.EqualTo(new[] { "cook" }));
    }

    [Test]
    public async Task TheOverallLine_CountsEveryTrialOfEveryRepeat_AndSaysTooFewBelowFive()
    {
        var overall = Events(await RunAsync(new SummaryEval(), repeats: 2)).First(l => Kind(l) == "summary");

        Assert.That(
            (overall.GetProperty("n").GetInt32(), overall.GetProperty("passed").GetInt32(),
                overall.GetProperty("failed").GetInt32()), Is.EqualTo((6, 6, 0)));
        Assert.That(overall.GetProperty("too_few").GetBoolean(), Is.False);
        Assert.That(overall.GetProperty("pass_rate").GetDouble(), Is.EqualTo(1.0));
        Assert.That(overall.GetProperty("tasks").EnumerateArray().Select(t => t.GetString()),
            Is.EqualTo(new[] { "a", "b", "c" }));
    }

    [Test]
    public async Task ACategoryOfFewerThanFiveTrials_IsTooFew_AndWritesNoRates()
    {
        var shop = Events(await RunAsync(new SummaryEval(), repeats: 1)).Single(l =>
            Kind(l) == "summary" && l.GetProperty("slice").GetString() == "shop:pnp");

        Assert.That(shop.GetProperty("too_few").GetBoolean(), Is.True);
        Assert.That(shop.GetProperty("n").GetInt32(), Is.EqualTo(2));
        Assert.That(
            shop.TryGetProperty("pass_rate", out _) || shop.TryGetProperty("pass_at_k", out _) ||
            shop.TryGetProperty("pass_hat_k", out _), Is.False, "absent, never 0");
    }

    [Test]
    public async Task TheLineCarriesTheGraderAndKindCounts_AndTheScoreMeans()
    {
        var overall = Events(await RunAsync(new SummaryEval()))
            .Single(l => Kind(l) == "summary" && l.GetProperty("slice").GetString() == "overall");

        var grader = overall.GetProperty("graders")[0];
        Assert.That(
            (grader.GetProperty("grader").GetString(), grader.GetProperty("kind").GetString(),
                grader.GetProperty("passed").GetInt32()), Is.EqualTo(("wants-text", "FixedRule", 3)));
        Assert.That(overall.GetProperty("kinds")[0].GetProperty("kind").GetString(), Is.EqualTo("FixedRule"));
        var mean = overall.GetProperty("scores")[0];
        Assert.That(
            (mean.GetProperty("key").GetString(), mean.GetProperty("mean").GetDouble(),
                mean.GetProperty("n").GetInt32()), Is.EqualTo(("turns", 1.0, 3)));
    }

    [Test]
    public async Task AReScore_WritesItsSummaryFromTheNewGrades_AndNeverCopiesTheStoredRunsOwn()
    {
        string[] stored = await RunAsync(new SummaryEval());
        var sink = new StringWriter();
        var runner = new EvalRunner(new NoSessionClient(), new ProtocolWriter(sink, new FixedClock(At)),
            new FixedClock(At), NullLogger<EvalRunner>.Instance);

        await runner.RescoreAsync(new SummaryEval(wanted: "Totsiens"), "run_new", "run_old", stored,
            CancellationToken.None);

        var summaries = Events(Lines(sink)).Where(l => Kind(l) == "summary").ToList();
        Assert.That(summaries, Has.Count.EqualTo(3), "one per slice, not the stored run's three as well");
        var overall = summaries[0];
        Assert.That((overall.GetProperty("passed").GetInt32(), overall.GetProperty("failed").GetInt32()),
            Is.EqualTo((0, 3)), "today's grader, the old transcripts");
        Assert.That(Events(stored).Count(l => Kind(l) == "summary"), Is.EqualTo(3),
            "the stored run's own lines are untouched");
    }

    [Test]
    public async Task AReScoredScenarioTheDatasetNoLongerHas_HasNoCategory_SoItIsOnlyInTheOverallLine()
    {
        string[] stored = await RunAsync(new SummaryEval());
        var sink = new StringWriter();
        var runner = new EvalRunner(new NoSessionClient(), new ProtocolWriter(sink, new FixedClock(At)),
            new FixedClock(At), NullLogger<EvalRunner>.Instance);

        await runner.RescoreAsync(new SummaryEval(scenarios: ["a", "b"]), "run_new", "run_old", stored,
            CancellationToken.None);

        var summaries = Events(Lines(sink)).Where(l => Kind(l) == "summary")
            .ToDictionary(l => l.GetProperty("slice").GetString()!);
        Assert.That(summaries["overall"].GetProperty("n").GetInt32(), Is.EqualTo(3));
        Assert.That(summaries["overall"].GetProperty("unknown").GetInt32(), Is.EqualTo(1),
            "c is Unknown: its scenario is gone");
        Assert.That(summaries["language:af"].GetProperty("n").GetInt32(), Is.EqualTo(2), "c is in no category now");
    }

    /// <summary>The summary line as a person and the API read it: ASCII only, the rates absent when too few.</summary>
    [Test]
    public void TheProtocolWriter_WritesTheSummaryLine_InSnakeCase_AsciiOnly_WithRatesOnlyWhenThereAreSome()
    {
        var sink = new StringWriter();
        var writer = new ProtocolWriter(sink, new FixedClock(At));
        writer.Summary(new SummaryLine("cook", "language:af", 6, 4, 1, 1, 0, ["a", "b"], TooFew: false, 0.8, 1.0, 0.5,
            [new GraderCount("wants-text", GraderKind.FixedRule, 4, 1, 1)],
            [new KindCount(GraderKind.FixedRule, 4, 1, 1)], [new ScoreMean("turns", 1.5, 6)]));
        writer.Summary(
            new SummaryLine("cook", "café", 3, 3, 0, 0, 0, ["c"], TooFew: true, null, null, null, [], [], []));

        var lines = Lines(sink);
        Assert.That(lines.All(line => line.All(c => c < 128)), Is.True, "ASCII only");
        var full = Events(lines)[0];
        Assert.That(full.GetProperty("event").GetString(), Is.EqualTo("summary"));
        Assert.That(
            (full.GetProperty("mode").GetString(), full.GetProperty("slice").GetString(),
                full.GetProperty("n").GetInt32(), full.GetProperty("pass_rate").GetDouble()),
            Is.EqualTo(("cook", "language:af", 6, 0.8)));
        Assert.That(
            (full.GetProperty("pass_at_k").GetDouble(), full.GetProperty("pass_hat_k").GetDouble(),
                full.GetProperty("too_few").GetBoolean()), Is.EqualTo((1.0, 0.5, false)));
        Assert.That(full.GetProperty("graders")[0].GetProperty("unknown").GetInt32(), Is.EqualTo(1));
        var few = Events(lines)[1];
        Assert.That(few.GetProperty("slice").GetString(), Is.EqualTo("café"));
        Assert.That(few.TryGetProperty("pass_rate", out _), Is.False);
    }

    /// <summary>An eval of scenarios a, b and c (b and c named in categories), one mode, one grader (the answer holds the wanted text) and one numeric score.</summary>
    private sealed class SummaryEval(string wanted = "Hallo", string[]? scenarios = null) : IEval
    {
        private static readonly Dictionary<string, string[]> Categories = new()
        { ["a"] = ["language:af"], ["b"] = ["language:af", "shop:pnp"], ["c"] = ["shop:pnp"] };

        public string Name => "summary-cook";
        public int Version => 1;
        public string Proves => "A canned run, to roll up.";

        public ValueTask<IReadOnlyList<EvalMode>> ModesAsync(ISessionClient sessions,
            CancellationToken cancellationToken) =>
            ValueTask.FromResult<IReadOnlyList<EvalMode>>([
                new EvalMode("cook", SampleRole.Alpha, [SampleToolset.Alpha])
            ]);

        public IReadOnlyList<EvalScenario> Scenarios { get; } = (scenarios ?? ["a", "b", "c"])
            .Select(id => new EvalScenario(id, new EvalInput(null, "Africa/Johannesburg", ["Gee my resep"]), default,
                Categories[id], DatasetSplit.Dev)).ToList();

        public IReadOnlyList<IGrader> Graders { get; } = [new WantsText(wanted)];

        public EvalScores ScoresOf(Trial trial, IReadOnlyList<Grade> grades) =>
            new Scores(trial.Transcript.Turns.Count);

        public TurnFacts TurnFactsOf(TurnTranscript turn, EvalMode mode) => new(null, null, 0, 0, [], null);
    }

    private sealed class WantsText(string wanted) : IGrader
    {
        public string Name => "wants-text";
        public int Version => 1;
        public GraderKind Kind => GraderKind.FixedRule;
        public IReadOnlySet<string> ExpectedKeys { get; } = new HashSet<string>();
        public bool Required => true;

        public ValueTask<Grade> GradeAsync(Trial trial, CancellationToken cancellationToken)
        {
            bool held = trial.Transcript.Turns.All(turn =>
                EngineEvents.AnswerOf(turn)?.Contains(wanted, StringComparison.Ordinal) == true);
            return ValueTask.FromResult(new Grade(Name, Version, held ? 1 : 0,
                held ? GradeOutcome.Pass : GradeOutcome.Fail, held ? "held" : "missing"));
        }
    }

    private sealed record Scores(int Turns) : EvalScores;
}
