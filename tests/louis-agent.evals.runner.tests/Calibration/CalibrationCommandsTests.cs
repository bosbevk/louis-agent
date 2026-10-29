using LouisAgent.Testing;
using LouisAgent.Choices;
using LouisAgent.Evals.Runner.Evals.Stub;
using System.Text.Json;
using LouisAgent.Evals.Api;
using LouisAgent.Evals.Datasets;
using LouisAgent.Evals.Datasets.Scenarios;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Grading.Common;
using LouisAgent.Evals.Protocol;
using LouisAgent.Evals.Protocol.Events.Conversations;
using LouisAgent.Evals.Protocol.Events.Run;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Protocol.Parts.Conversations;
using LouisAgent.Evals.Labels;
using LouisAgent.Evals.Runner.Arguments;
using LouisAgent.Evals.Runner.Calibration;
using LouisAgent.Evals.Trials;

namespace LouisAgent.Evals.Runner.Tests.Calibration;

/// <summary>
/// The commands that check a model grader against a person's labels (F18 step 6.3). <c>--calibrate</c> runs the
/// judge on every labelled answer and prints how often it agrees, then each disagreement; it never runs in CI, since
/// it asks a model. <c>--label-sheet</c> writes the files to label.
/// </summary>
public class CalibrationCommandsTests
{
    /// <summary>The one scenario: a meal for two.</summary>
    private static readonly EvalScenario Scenario =
        new(SampleScenario.Beta, new EvalInput(null, "Africa/Johannesburg", ["For two"]),
            JsonDocument.Parse("""{"serves":2}""").RootElement.Clone(), [], DatasetSplit.Dev);

    /// <summary>The time every stored line carries.</summary>
    private static readonly DateTimeOffset At = new(2026, 10, 28, 8, 0, 0, TimeSpan.Zero);

    /// <summary>A person's label of one answer, for a grader.</summary>
    private static LabeledAnswer Answer(string text, bool pass, int repeat = 1, string grader = "meal-for-people") =>
        new(SampleScenario.Fill($"<ScenarioBeta>.{repeat}.json"), grader, SampleScenario.Beta,
            StubMode.Delegating.Wire(), "8f7802b9a72e", repeat, [new LabelTurn("For two", text)], pass, "");

    /// <summary>No setting is set: not in CI.</summary>
    private static string? NoCi(string name) => null;

    /// <summary>Runs <c>--calibrate</c> on the eval and hands back its exit code and what it printed.</summary>
    private static async Task<(int Code, string Out)> CalibrateAsync(FakeEval eval, Func<string, string?> env,
        string grader = "meal-for-people")
    {
        var output = new StringWriter();
        int code = await CalibrateCommand.RunAsync(new CalibrateRequest(eval.Name, grader), eval, env, TextReader.Null,
            output, CancellationToken.None);
        return (code, output.ToString().ReplaceLineEndings("\n"));
    }

    /// <summary>The agreement comes first, by class too, then each answer the judge and person disagree on.</summary>
    [Test]
    public async Task ItPrintsTheAgreement_ThenEachDisagreement()
    {
        var eval = new FakeEval([Answer("yes", pass: true), Answer("yes", pass: false, repeat: 2)]);

        var (code, output) = await CalibrateAsync(eval, NoCi);

        Assert.That(code, Is.Zero);
        string[] lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.That(lines[0], Is.EqualTo("Agreement: 1 of 2"));
        Assert.That(lines.Any(l => l.StartsWith("Labelled pass:")) && lines.Any(l => l.StartsWith("Labelled fail:")),
            Is.True, "the agreement by class");
        Assert.That(lines.Single(l => l.StartsWith("Result:")), Does.StartWith("Result: not enough"));
        Assert.That(lines.Single(l => l.Contains("repeat 2")),
            Does.Contain(SampleScenario.Beta).And.Contain("repeat 2").And.Contain("label: fail").And
                .Contain("judge: pass"));
    }

    /// <summary>
    /// Never in CI (Louis): it asks a model, so it costs money or needs a local one CI doesn't have. The guard is the
    /// first thing it does.
    /// </summary>
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

    /// <summary>A CI setting that's empty or blank isn't CI.</summary>
    [TestCase("")]
    [TestCase("  ")]
    public async Task AnEmptyCiIsNotCi(string ci)
    {
        var (code, _) =
            await CalibrateAsync(new FakeEval([Answer("yes", pass: true)]), name => name == "CI" ? ci : null);

        Assert.That(code, Is.Zero);
    }

    /// <summary>A grader the eval doesn't have is refused, naming the ones it has.</summary>
    [Test]
    public void AGraderTheEvalHasNot_IsRefusedWithTheOnesItHas()
    {
        Assert.That(async () => await CalibrateAsync(new FakeEval([Answer("yes", true)]), NoCi, grader: "nope"),
            Throws.InstanceOf<RunnerException>().With.Message.Contains("'nope'").And.Message
                .Contains("meal-for-people"));
    }

    /// <summary>A fixed rule asks no model, so there's nothing to calibrate.</summary>
    [Test]
    public void AFixedRuleNeedsNoCalibration()
    {
        Assert.That(async () => await CalibrateAsync(new FakeEval([Answer("yes", true)]), NoCi, grader: "no-error"),
            Throws.InstanceOf<RunnerException>().With.Message.Contains("'no-error'").And.Message.Contains("model"));
    }

    /// <summary>
    /// The labels folder may hold more than one grader's labels; a calibration reads only those of the grader it's
    /// asked about (F18 step 6a.0).
    /// </summary>
    [Test]
    public async Task OnlyTheLabelsOfTheGraderAskedAbout_AreUsed()
    {
        var eval = new FakeEval([
            Answer("yes", pass: true), Answer("no", pass: true, repeat: 2, grader: "another-judge")
        ]);

        var (_, output) = await CalibrateAsync(eval, NoCi);

        Assert.That(output, Does.StartWith("Agreement: 1 of 1"));
    }

    /// <summary>Labels only of another grader are no labels for this one, and it says how to make some.</summary>
    [Test]
    public void LabelsOnlyOfAnotherGrader_ArePerhapsNoLabelsForThisOne()
    {
        Assert.That(
            async () => await CalibrateAsync(new FakeEval([Answer("yes", pass: true, grader: "another-judge")]), NoCi),
            Throws.InstanceOf<RunnerException>().With.Message.Contains("meal-for-people").And.Message
                .Contains("--label-sheet"));
    }

    /// <summary>With no labels there's nothing to calibrate against, and it says how to make them.</summary>
    [Test]
    public void WithNoLabels_ThereIsNothingToCalibrateAgainst_AndItSaysHowToMakeThem()
    {
        Assert.That(async () => await CalibrateAsync(new FakeEval([]), NoCi),
            Throws.InstanceOf<RunnerException>().With.Message.Contains("--label-sheet"));
    }

    /// <summary>The label sheet is written from the stored run read on stdin, for the grader's scenarios.</summary>
    [Test]
    public void TheLabelSheet_IsWrittenFromTheStoredRunOnStdin_ForTheGradersScenarios()
    {
        string folder = Path.Combine(Path.GetTempPath(), "sheet-" + Guid.NewGuid().ToString("N"));
        try
        {
            var output = new StringWriter();

            int code = LabelSheetCommand.Run(
                new LabelSheetRequest("fake", "meal-for-people", "8f7802b9a72e", folder, 10), new FakeEval([]),
                StoredRun(), output);

            Assert.That(code, Is.Zero);
            Assert.That(Directory.GetFiles(folder).Select(Path.GetFileName),
                Is.EqualTo(new[]
                {
                    SampleScenario.Fill(
                        $"meal-for-people.<ScenarioBeta>.{StubMode.Delegating.Wire()}.8f7802b9a72e.1.json")
                }));
            Assert.That(output.ToString(), Does.Contain("1 label file").And.Contain(folder));
        }
        finally
        {
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
        }
    }

    /// <summary>A stored run of one conversation: a meal for two, answered "Bobotie".</summary>
    private static string[] StoredRun()
    {
        string mode = StubMode.Delegating.Wire(), task = SampleScenario.Beta;
        var delta = JsonDocument.Parse(
                """{"type":"content_block_delta","index":0,"delta":{"type":"text_delta","text":"Bobotie"}}""")
            .RootElement.Clone();
        EvalEvent[] events =
        [
            new ProtocolEvent(At, ProtocolEvent.CurrentVersion, "fake", 1, "abc123", RunSplit.Dev, null, null, [], []),
            new ModeEvent(At, mode, 1, [StubToolset.Web.Wire()], "r", null, 1),
            new QuestionEvent(At, mode, 1, task, 1, "s", "For two"),
            new SseEvent(At, mode, 1, task, 1, "content_block_delta", delta),
            new TurnEvent(At, mode, 1, task, 1, "s", "For two", "Bobotie", null, null, 0, 0, [], 1.0, null, null),
            new ConversationEvent(At, mode, 1, task, null, 0, 1.0, [], TrialOutcome.Pass,
                new Metrics(0, 0, [], 1, null, null, 1.0, null)),
        ];
        return [.. events.Select(EvalProtocolJson.Line)];
    }

    /// <summary>A model grader that says yes to "yes" and no to anything else, and counts its asks.</summary>
    private sealed class FakeGrader : IGrader
    {
        /// <summary>How many answers it was asked about.</summary>
        public int Asked { get; private set; }

        /// <inheritdoc />
        public string Name => "meal-for-people";

        /// <inheritdoc />
        public int Version => 1;

        /// <inheritdoc />
        public GraderKind Kind => GraderKind.Model;

        /// <inheritdoc />
        public bool Required => false;

        /// <inheritdoc />
        public IReadOnlySet<string> ExpectedKeys { get; } = new HashSet<string> { "serves" };

        /// <summary>Passes an answer of "yes", fails anything else, and counts the ask.</summary>
        public ValueTask<Grade> GradeAsync(Trial trial, CancellationToken cancellationToken)
        {
            Asked++;
            return ValueTask.FromResult(EngineEvents.AnswerOf(trial.Transcript.Turns[^1]) == "yes"
                ? Verdict.Pass(Name, Version, "a meal")
                : Verdict.Fail(Name, Version, "not a meal"));
        }
    }

    /// <summary>A fixed rule that always passes: one a calibration refuses.</summary>
    private sealed class NoErrorRule : IGrader
    {
        /// <inheritdoc />
        public string Name => "no-error";

        /// <inheritdoc />
        public int Version => 1;

        /// <inheritdoc />
        public GraderKind Kind => GraderKind.FixedRule;

        /// <inheritdoc />
        public bool Required => true;

        /// <inheritdoc />
        public IReadOnlySet<string> ExpectedKeys { get; } = new HashSet<string>();

        /// <summary>Always passes.</summary>
        public ValueTask<Grade> GradeAsync(Trial trial, CancellationToken cancellationToken) =>
            ValueTask.FromResult(Verdict.Pass(Name, Version, "fine"));
    }

    /// <summary>An eval with the two graders, the one scenario and the labels a test hands it.</summary>
    private sealed class FakeEval(IReadOnlyList<LabeledAnswer> labels) : IEval
    {
        /// <summary>The model grader, so a test can count its asks.</summary>
        public FakeGrader Grader { get; } = new();

        /// <inheritdoc />
        public string Name => "fake";

        /// <inheritdoc />
        public int Version => 1;

        /// <inheritdoc />
        public string Proves => "A fake eval for the calibration commands.";

        /// <inheritdoc />
        public IReadOnlyList<LabeledAnswer> Labels => labels;

        /// <inheritdoc />
        public IReadOnlyList<EvalScenario> Scenarios { get; } = [Scenario];

        /// <inheritdoc />
        public IReadOnlyList<IGrader> Graders => [new NoErrorRule(), Grader];

        /// <summary>Never called: the calibration commands run no mode.</summary>
        public ValueTask<IReadOnlyList<EvalMode>> ModesAsync(ISessionClient sessions,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        /// <summary>Never called: the calibration commands read no turn's facts.</summary>
        public TurnFacts TurnFactsOf(TurnTranscript turn, EvalMode mode) => throw new NotSupportedException();
    }
}
