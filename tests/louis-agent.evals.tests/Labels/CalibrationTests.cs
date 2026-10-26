using System.Text.Json;
using LouisAgent.Evals.Datasets;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Grading.Common;
using LouisAgent.Evals.Labels;
using LouisAgent.Evals.Trials;

namespace LouisAgent.Evals.Tests.Labels;

// F18 step 6.3: calibration asks a model grader about every labelled answer and counts how often it agrees with the person. Unknown ("can't tell") is not agreement, and each
// disagreement is listed with the label, the verdict and the judge's reason, so the person can see why. The trial it builds has the answer in its turn, after the turns before it,
// so the grader sees the scenario's ask as in a run. Written first: Calibration did not exist.
public class CalibrationTests
{
    private static EvalScenario Scenario(string id, string expected, params string[] messages) =>
        new(id, new EvalInput(null, "Africa/Johannesburg", messages), JsonDocument.Parse(expected).RootElement.Clone(), [], DatasetSplit.Dev);

    private static readonly IReadOnlyList<EvalScenario> Scenarios =
    [
        Scenario("cook-af", """{"serves":[4,6]}""", "Vir vier mense", "Maak dit vir ses"),
        Scenario("cook-en", """{"serves":2}""", "For two"),
    ];

    private static LabeledAnswer Answer(string scenario, string text, bool meal, int turn = 1, int repeat = 1) =>
        new($"{scenario}.{repeat}.json", scenario, "puls-cook", "8f7802b9a72e", repeat, turn, text, meal, $"note on {text}");

    /// <summary>A model grader that answers by what the answer says: "yes" passes, "no" fails, anything else it can't tell. It keeps the trials it was asked about.</summary>
    private sealed class WordGrader : IGrader
    {
        public List<Trial> Asked { get; } = [];
        public string Name => "meal-for-people";
        public int Version => 1;
        public GraderKind Kind => GraderKind.Model;
        public bool Required => false;
        public IReadOnlySet<string> ExpectedKeys { get; } = new HashSet<string> { "serves" };

        public ValueTask<Grade> GradeAsync(Trial trial, CancellationToken cancellationToken)
        {
            Asked.Add(trial);
            string answer = EngineEvents.AnswerOf(trial.Transcript.Turns[^1]);
            return ValueTask.FromResult(answer switch
            {
                "yes" => Verdict.Pass(Name, Version, "it is a meal"),
                "no" => Verdict.Fail(Name, Version, "it is a side"),
                _ => Verdict.Unknown(Name, Version, "cannot tell"),
            });
        }
    }

    [Test]
    public async Task ItCountsHowOftenTheJudgeAgreesWithThePerson()
    {
        var labels = new[] { Answer("cook-en", "yes", meal: true), Answer("cook-en", "no", meal: false, repeat: 2), Answer("cook-en", "yes", meal: false, repeat: 3) };

        var report = await Calibration.RunAsync(new WordGrader(), Scenarios, labels, CancellationToken.None);

        Assert.That((report.Agreed, report.Total), Is.EqualTo((2, 3)));
        Assert.That(report.Disagreements.Single().Answer.Repeat, Is.EqualTo(3));
        Assert.That((report.Disagreements.Single().Verdict, report.Disagreements.Single().Reason), Is.EqualTo((GradeOutcome.Pass, "it is a meal")));
    }

    [Test]
    public async Task ACantTell_IsNotAgreement_ItIsListedAsOne()
    {
        var report = await Calibration.RunAsync(new WordGrader(), Scenarios, [Answer("cook-en", "hmm", meal: true)], CancellationToken.None);

        Assert.That((report.Agreed, report.Total), Is.EqualTo((0, 1)));
        Assert.That(report.Disagreements.Single().Verdict, Is.EqualTo(GradeOutcome.Unknown));
    }

    [Test]
    public async Task TheTrialHasTheAnswerInItsTurn_AfterTheTurnsBeforeIt_WithTheScenariosAsk()
    {
        var grader = new WordGrader();

        await Calibration.RunAsync(grader, Scenarios, [Answer("cook-af", "yes", meal: true, turn: 2)], CancellationToken.None);

        var trial = grader.Asked.Single();
        Assert.That(trial.Scenario.Id, Is.EqualTo("cook-af"));
        Assert.That(trial.Transcript.Turns.Select(t => t.Turn), Is.EqualTo(new[] { 1, 2 }));
        Assert.That(trial.Transcript.Turns.Select(t => t.Question), Is.EqualTo(new[] { "Vir vier mense", "Maak dit vir ses" }));
        Assert.That(EngineEvents.AnswerOf(trial.Transcript.Turns[0]), Is.Empty, "the earlier turns are not what is labelled");
        Assert.That(EngineEvents.AnswerOf(trial.Transcript.Turns[1]), Is.EqualTo("yes"));
        Assert.That(trial.Mode.Name, Is.EqualTo("puls-cook"));
    }

    [Test]
    public async Task TheReport_ReadsAsAgreementThenEachDisagreement()
    {
        var labels = new[] { Answer("cook-en", "yes", meal: true), Answer("cook-en", "yes", meal: false, repeat: 2) };

        var report = await Calibration.RunAsync(new WordGrader(), Scenarios, labels, CancellationToken.None);

        var lines = report.Lines();
        Assert.That(lines[0], Is.EqualTo("Agreement: 1 of 2"));
        Assert.That(lines[1], Does.Contain("cook-en").And.Contain("run 8f7802b9a72e").And.Contain("repeat 2").And.Contain("label: not a meal").And.Contain("judge: a meal").And.Contain("it is a meal"));
        Assert.That(lines, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task WithNothingLabelled_ThereIsNothingToAgreeWith()
    {
        var report = await Calibration.RunAsync(new WordGrader(), Scenarios, [], CancellationToken.None);

        Assert.That((report.Agreed, report.Total), Is.EqualTo((0, 0)));
        Assert.That(report.Lines(), Is.EqualTo(new[] { "Agreement: 0 of 0" }));
    }
}
