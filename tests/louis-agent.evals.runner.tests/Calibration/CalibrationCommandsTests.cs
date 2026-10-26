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

// F18 step 6.3: the commands that check a model grader against a person's labels. `--calibrate` runs the judge on every labelled answer and prints how often it agrees, then each
// disagreement; it is never run in CI, because it asks a model. `--label-sheet` writes the files to label. Written first: the commands did not exist.
public class CalibrationCommandsTests
{
    private static readonly EvalScenario Scenario =
        new("cook-en", new EvalInput(null, "Africa/Johannesburg", ["For two"]), JsonDocument.Parse("""{"serves":2}""").RootElement.Clone(), [], DatasetSplit.Dev);

    private static LabeledAnswer Answer(string text, bool pass, int repeat = 1, string grader = "meal-for-people") =>
        new($"cook-en.{repeat}.json", grader, "cook-en", "puls-cook", "8f7802b9a72e", repeat, [new LabelTurn("For two", text)], pass, "");

    private static string? NoCi(string name) => null;

    private static async Task<(int Code, string Out)> CalibrateAsync(FakeEval eval, Func<string, string?> env, string grader = "meal-for-people")
    {
        var output = new StringWriter();
        int code = await CalibrateCommand.RunAsync(new CalibrateRequest(eval.Name, grader), eval, env, TextReader.Null, output, CancellationToken.None);
        return (code, output.ToString().ReplaceLineEndings("\n"));
    }

    [Test]
    public async Task ItPrintsTheAgreement_ThenEachDisagreement()
    {
        var eval = new FakeEval([Answer("yes", pass: true), Answer("yes", pass: false, repeat: 2)]);

        var (code, output) = await CalibrateAsync(eval, NoCi);

        Assert.That(code, Is.Zero);
        string[] lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.That(lines[0], Is.EqualTo("Agreement: 1 of 2"));
        Assert.That(lines.Any(l => l.StartsWith("Labelled pass:")) && lines.Any(l => l.StartsWith("Labelled fail:")), Is.True, "the agreement by class");
        Assert.That(lines.Single(l => l.StartsWith("Result:")), Does.StartWith("Result: not enough"));
        Assert.That(lines.Single(l => l.Contains("repeat 2")), Does.Contain("cook-en").And.Contain("repeat 2").And.Contain("label: fail").And.Contain("judge: pass"));
    }

    // Louis: "Never in CI". It asks a model, so it costs or needs a local one that CI does not have; the guard is the first thing it does.
    [TestCase("true")]
    [TestCase("1")]
    [TestCase("anything")]
    public void InCi_ItRefusesBeforeAnythingElse(string ci)
    {
        var eval = new FakeEval([Answer("yes", pass: true)]);

        Assert.That(async () => await CalibrateAsync(eval, name => name == "CI" ? ci : null),
            Throws.InstanceOf<RunnerException>().With.Message.Contains("CI"));
        Assert.That(eval.Grader.Asked, Is.Zero, "no judge was asked");
    }

    [TestCase("")]
    [TestCase("  ")]
    public async Task AnEmptyCiIsNotCi(string ci)
    {
        var (code, _) = await CalibrateAsync(new FakeEval([Answer("yes", pass: true)]), name => name == "CI" ? ci : null);

        Assert.That(code, Is.Zero);
    }

    [Test]
    public void AGraderTheEvalHasNot_IsRefusedWithTheOnesItHas()
    {
        Assert.That(async () => await CalibrateAsync(new FakeEval([Answer("yes", true)]), NoCi, grader: "nope"),
            Throws.InstanceOf<RunnerException>().With.Message.Contains("'nope'").And.Message.Contains("meal-for-people"));
    }

    [Test]
    public void AFixedRuleNeedsNoCalibration()
    {
        Assert.That(async () => await CalibrateAsync(new FakeEval([Answer("yes", true)]), NoCi, grader: "no-error"),
            Throws.InstanceOf<RunnerException>().With.Message.Contains("'no-error'").And.Message.Contains("model"));
    }

    // F18 step 6a.0: the labels folder may hold labels of more than one grader; a calibration reads those of the grader it is asked about, and says if it has none.
    [Test]
    public async Task OnlyTheLabelsOfTheGraderAskedAbout_AreUsed()
    {
        var eval = new FakeEval([Answer("yes", pass: true), Answer("no", pass: true, repeat: 2, grader: "another-judge")]);

        var (_, output) = await CalibrateAsync(eval, NoCi);

        Assert.That(output, Does.StartWith("Agreement: 1 of 1"));
    }

    [Test]
    public void LabelsOnlyOfAnotherGrader_ArePerhapsNoLabelsForThisOne()
    {
        Assert.That(async () => await CalibrateAsync(new FakeEval([Answer("yes", pass: true, grader: "another-judge")]), NoCi),
            Throws.InstanceOf<RunnerException>().With.Message.Contains("meal-for-people").And.Message.Contains("--label-sheet"));
    }

    [Test]
    public void WithNoLabels_ThereIsNothingToCalibrateAgainst_AndItSaysHowToMakeThem()
    {
        Assert.That(async () => await CalibrateAsync(new FakeEval([]), NoCi),
            Throws.InstanceOf<RunnerException>().With.Message.Contains("--label-sheet"));
    }

    [Test]
    public async Task TheLabelSheet_IsWrittenFromTheStoredRunOnStdin_ForTheGradersScenarios()
    {
        string folder = Path.Combine(Path.GetTempPath(), "sheet-" + Guid.NewGuid().ToString("N"));
        try
        {
            string At(object e) => "@" + JsonSerializer.Serialize(e);
            string[] stored =
            [
                At(new { @event = "protocol", version = 1, eval = "fake", eval_version = 1, graders = Array.Empty<object>(), models = Array.Empty<object>() }),
                At(new { @event = "mode", mode = "puls-cook", repeat = 1, toolsets = new[] { "web" }, role = "r", llm = (string?)null }),
                At(new { @event = "question", mode = "puls-cook", repeat = 1, task = "cook-en", turn = 1, session = "s", text = "For two" }),
                At(new { @event = "sse", mode = "puls-cook", repeat = 1, task = "cook-en", turn = 1, type = "content_block_delta", data = new { type = "content_block_delta", index = 0, delta = new { type = "text_delta", text = "Bobotie" } } }),
                At(new { @event = "turn", mode = "puls-cook", repeat = 1, task = "cook-en", turn = 1, session = "s", question = "For two", answer = "Bobotie", seconds = 1.0 }),
                At(new { @event = "conversation", mode = "puls-cook", repeat = 1, task = "cook-en", scores = new { }, errors = 0, seconds = 1.0 }),
            ];
            var output = new StringWriter();

            int code = LabelSheetCommand.Run(new LabelSheetRequest("fake", "meal-for-people", "8f7802b9a72e", folder, 10), new FakeEval([]), stored, output);

            Assert.That(code, Is.Zero);
            Assert.That(Directory.GetFiles(folder).Select(Path.GetFileName), Is.EqualTo(new[] { "meal-for-people.cook-en.puls-cook.8f7802b9a72e.1.json" }));
            Assert.That(output.ToString(), Does.Contain("1 label file").And.Contain(folder));
        }
        finally { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
    }

    /// <summary>A model grader that says yes to "yes" and no to anything else, and counts how often it was asked.</summary>
    private sealed class FakeGrader : IGrader
    {
        public int Asked { get; private set; }
        public string Name => "meal-for-people";
        public int Version => 1;
        public GraderKind Kind => GraderKind.Model;
        public bool Required => false;
        public IReadOnlySet<string> ExpectedKeys { get; } = new HashSet<string> { "serves" };

        public ValueTask<Grade> GradeAsync(Trial trial, CancellationToken cancellationToken)
        {
            Asked++;
            return ValueTask.FromResult(EngineEvents.AnswerOf(trial.Transcript.Turns[^1]) == "yes" ? Verdict.Pass(Name, Version, "a meal") : Verdict.Fail(Name, Version, "not a meal"));
        }
    }

    private sealed class NoErrorRule : IGrader
    {
        public string Name => "no-error";
        public int Version => 1;
        public GraderKind Kind => GraderKind.FixedRule;
        public bool Required => true;
        public IReadOnlySet<string> ExpectedKeys { get; } = new HashSet<string>();
        public ValueTask<Grade> GradeAsync(Trial trial, CancellationToken cancellationToken) => ValueTask.FromResult(Verdict.Pass(Name, Version, "fine"));
    }

    private sealed class FakeEval(IReadOnlyList<LabeledAnswer> labels) : IEval
    {
        public FakeGrader Grader { get; } = new();
        public string Name => "fake";
        public string? Identity => null;
        public int Version => 1;
        public string Proves => "A fake eval for the calibration commands.";
        public IReadOnlyList<LabeledAnswer> Labels => labels;
        public IReadOnlyList<EvalScenario> Scenarios { get; } = [Scenario];
        public IReadOnlyList<IGrader> Graders => [new NoErrorRule(), Grader];

        public ValueTask<IReadOnlyList<EvalMode>> ModesAsync(ISessionClient sessions, CancellationToken cancellationToken) => throw new NotSupportedException();
        public EvalScores ScoresOf(Trial trial, IReadOnlyList<Grade> grades) => throw new NotSupportedException();
        public TurnFacts TurnFactsOf(TurnTranscript turn, EvalMode mode) => throw new NotSupportedException();
    }
}
