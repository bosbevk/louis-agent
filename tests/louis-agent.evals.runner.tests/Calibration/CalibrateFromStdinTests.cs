using LouisAgent.Testing;
using LouisAgent.Choices;
using LouisAgent.Evals.Runner.Evals.Stub;
using System.Text.Json;
using LouisAgent.Evals.Api;
using LouisAgent.Evals.Datasets;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Grading.Common;
using LouisAgent.Evals.Labels;
using LouisAgent.Evals.Runner.Arguments;
using LouisAgent.Evals.Runner.Calibration;
using LouisAgent.Evals.Trials;

namespace LouisAgent.Evals.Runner.Tests.Calibration;

// F18 step 6a.5a: the portal's calibration. The API never references the harness, so it starts the runner as a child process (as a re-score does) and writes the label files to its
// stdin, one JSON object per line: `--calibrate {eval} {grader} --labels -` reads them there instead of the dataset's labels/ folder, with the same checks. After the human lines the
// runner prints one `@` line, the report as data (the eval, the grader, the judge model, the agreement, the bar, each class's counts in the agreement card's shape, and each
// disagreement), which is what the API keeps. Written first: --labels, the line and the stdin reading did not exist.
public class CalibrateFromStdinTests
{
    private static readonly EvalScenario Scenario =
        new("cook-en", new EvalInput(null, "Africa/Johannesburg", ["For two", "Make it four"]), JsonDocument.Parse("""{"serves":2}""").RootElement.Clone(), [], DatasetSplit.Dev);

    /// <summary>A label file's text as the API writes it: one line, snake_case.</summary>
    private static string Line(string answer, bool pass, int repeat = 1, string grader = "meal-for-people", string scenario = "cook-en", int turns = 1) =>
        JsonSerializer.Serialize(new
        {
            grader, scenario, mode = StubMode.Delegating.Wire(), run = "76667d39dcdb", repeat,
            turns = Enumerable.Range(1, turns).Select(n => new { question = n == 1 ? "For two" : "Make it four", answer = n == turns ? answer : "earlier" }).ToArray(),
            label = new { pass, note = "" },
        });

    private static async Task<(int Code, string[] Lines)> CalibrateAsync(FakeEval eval, string stdin, string grader = "meal-for-people", Func<string, string?>? env = null)
    {
        var output = new StringWriter();
        int code = await CalibrateCommand.RunAsync(new CalibrateRequest(eval.Name, grader, LabelsOnStdin: true), eval, env ?? (_ => null), new StringReader(stdin), output,
            CancellationToken.None);
        return (code, output.ToString().ReplaceLineEndings("\n").Split('\n', StringSplitOptions.RemoveEmptyEntries));
    }

    [Test]
    public void TheCommandLine_TakesLabelsOnStdin_AndNothingElseForLabels()
    {
        Assert.That(CommandLine.Parse(["--calibrate", SampleEval.Beta, "meal-for-people", "--labels", "-"]), Is.EqualTo(new CalibrateRequest(SampleEval.Beta, "meal-for-people", LabelsOnStdin: true)));
        Assert.That(CommandLine.Parse(["--calibrate", SampleEval.Beta, "meal-for-people"]), Is.EqualTo(new CalibrateRequest(SampleEval.Beta, "meal-for-people")));
        Assert.That(() => CommandLine.Parse(["--calibrate", SampleEval.Beta, "meal-for-people", "--labels", "some/folder"]),
            Throws.InstanceOf<RunnerException>().With.Message.Contains("--labels").And.Message.Contains("-"));
    }

    [Test]
    public async Task TheLabelsOnStdin_AreTheOnesCalibratedAgainst_NotTheDatasets()
    {
        // The dataset's own labels say the opposite of stdin's: only stdin's count.
        var eval = new FakeEval([Dataset("yes", pass: false)]);

        var (code, lines) = await CalibrateAsync(eval, Line("yes", pass: true) + "\n" + Line("no", pass: false, repeat: 2) + "\n" + Line("yes", pass: false, repeat: 3) + "\n");

        Assert.That(code, Is.Zero);
        Assert.That(lines[0], Is.EqualTo("Agreement: 2 of 3"));
        Assert.That(eval.Grader.Asked, Is.EqualTo(3));
    }

    [Test]
    public async Task TheLastLine_IsTheReportAsData_ForTheAPI()
    {
        var eval = new FakeEval([]);

        var (_, lines) = await CalibrateAsync(eval, Line("yes", pass: true) + "\n" + Line("no", pass: false, repeat: 2) + "\n" + Line("yes", pass: false, repeat: 3) + "\n",
            env: name => name == "EVAL_JUDGE_MODEL" ? "qwen3:4b" : null);

        string last = lines[^1];
        Assert.That(last, Does.StartWith("@{"));
        var line = JsonDocument.Parse(last[1..]).RootElement;
        Assert.That(line.GetProperty("event").GetString(), Is.EqualTo("calibration"));
        Assert.That(line.GetProperty("eval").GetString(), Is.EqualTo("fake"));
        Assert.That(line.GetProperty("grader").GetString(), Is.EqualTo("meal-for-people"));
        Assert.That(line.GetProperty("judge_model").GetString(), Is.EqualTo("qwen3:4b"));
        Assert.That((line.GetProperty("agreed").GetInt32(), line.GetProperty("total").GetInt32(), line.GetProperty("bar").GetInt32()), Is.EqualTo((2, 3, 90)));
        Assert.That(line.GetProperty("passes").GetBoolean(), Is.False, "the fail class agreed 1 of 2");
        var pass = line.GetProperty("person_pass");
        var fail = line.GetProperty("person_fail");
        Assert.That((pass.GetProperty("judge_pass").GetInt32(), pass.GetProperty("judge_fail").GetInt32(), pass.GetProperty("judge_unknown").GetInt32()), Is.EqualTo((1, 0, 0)));
        Assert.That((fail.GetProperty("judge_pass").GetInt32(), fail.GetProperty("judge_fail").GetInt32(), fail.GetProperty("judge_unknown").GetInt32()), Is.EqualTo((1, 1, 0)));
        var disagreement = line.GetProperty("disagreements").EnumerateArray().Single();
        Assert.That((disagreement.GetProperty("scenario").GetString(), disagreement.GetProperty("mode").GetString(), disagreement.GetProperty("repeat").GetInt32(),
                disagreement.GetProperty("run").GetString(), disagreement.GetProperty("label").GetString(), disagreement.GetProperty("judge").GetString(), disagreement.GetProperty("reason").GetString()),
            Is.EqualTo(("cook-en", StubMode.Delegating.Wire(), 3, "76667d39dcdb", "fail", "pass", "a meal")));
        Assert.That(lines.Count(l => l.StartsWith('@')), Is.EqualTo(1), "one line of data, after the human lines");
    }

    [Test]
    public async Task WithoutAJudgeModelSet_TheLineSaysNull()
    {
        var (_, lines) = await CalibrateAsync(new FakeEval([]), Line("yes", pass: true) + "\n");

        Assert.That(JsonDocument.Parse(lines[^1][1..]).RootElement.GetProperty("judge_model").ValueKind, Is.EqualTo(JsonValueKind.Null));
    }

    // The same rules as the folder's: a label of a scenario the dataset hasn't, a turn it hasn't, or no label yet, is refused, naming the line, so the API's export and the loader never drift apart.
    [Test]
    public void ABadLine_IsRefusedByItsNumber()
    {
        Assert.That(async () => await CalibrateAsync(new FakeEval([]), Line("yes", pass: true) + "\n" + Line("yes", pass: true, scenario: "unknown-dish") + "\n"),
            Throws.InstanceOf<InvalidDataException>().With.Message.Contains("line 2").And.Message.Contains("unknown-dish"));
        Assert.That(async () => await CalibrateAsync(new FakeEval([]), "{not json\n"),
            Throws.InstanceOf<InvalidDataException>().With.Message.Contains("line 1"));
        Assert.That(async () => await CalibrateAsync(new FakeEval([]), Line("yes", pass: true, turns: 3) + "\n"),
            Throws.InstanceOf<InvalidDataException>().With.Message.Contains("line 1").And.Message.Contains("turn 3"));
    }

    [Test]
    public void NoLinesOnStdin_IsRefused_WithoutSuggestingASheet()
    {
        Assert.That(async () => await CalibrateAsync(new FakeEval([Dataset("yes", pass: true)]), "\n"),
            Throws.InstanceOf<RunnerException>().With.Message.Contains("stdin").And.Message.Not.Contains("--label-sheet"));
    }

    [Test]
    public async Task AFollowUp_IsJudgedWithItsEarlierTurns()
    {
        var eval = new FakeEval([]);

        await CalibrateAsync(eval, Line("yes", pass: true, turns: 2) + "\n");

        Assert.That(eval.Grader.LastTrial!.Transcript.Turns.Select(t => t.Question), Is.EqualTo(new[] { "For two", "Make it four" }));
    }

    private static LabeledAnswer Dataset(string text, bool pass) =>
        new("cook-en.1.json", "meal-for-people", "cook-en", StubMode.Delegating.Wire(), "8f7802b9a72e", 1, [new LabelTurn("For two", text)], pass, "");

    /// <summary>A model grader that says yes to "yes" and no to anything else, and keeps the last trial it saw.</summary>
    private sealed class FakeGrader : IGrader
    {
        public int Asked { get; private set; }
        public Trial? LastTrial { get; private set; }
        public string Name => "meal-for-people";
        public int Version => 1;
        public GraderKind Kind => GraderKind.Model;
        public bool Required => false;
        public IReadOnlySet<string> ExpectedKeys { get; } = new HashSet<string> { "serves" };

        public ValueTask<Grade> GradeAsync(Trial trial, CancellationToken cancellationToken)
        {
            Asked++;
            LastTrial = trial;
            return ValueTask.FromResult(EngineEvents.AnswerOf(trial.Transcript.Turns[^1]) == "yes" ? Verdict.Pass(Name, Version, "a meal") : Verdict.Fail(Name, Version, "not a meal"));
        }
    }

    private sealed class FakeEval(IReadOnlyList<LabeledAnswer> labels) : IEval
    {
        public FakeGrader Grader { get; } = new();
        public string Name => "fake";
        public string? Identity => null;
        public int Version => 1;
        public string Proves => "A fake eval for the calibration from stdin.";
        public IReadOnlyList<LabeledAnswer> Labels => labels;
        public IReadOnlyList<EvalScenario> Scenarios { get; } = [Scenario];
        public IReadOnlyList<IGrader> Graders => [Grader];

        public ValueTask<IReadOnlyList<EvalMode>> ModesAsync(ISessionClient sessions, CancellationToken cancellationToken) => throw new NotSupportedException();
        public EvalScores ScoresOf(Trial trial, IReadOnlyList<Grade> grades) => throw new NotSupportedException();
        public TurnFacts TurnFactsOf(TurnTranscript turn, EvalMode mode) => throw new NotSupportedException();
    }
}
