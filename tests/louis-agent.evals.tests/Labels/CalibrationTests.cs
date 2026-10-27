using LouisAgent.Testing;
using System.Text.Json;
using LouisAgent.Evals.Datasets;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Grading.Common;
using LouisAgent.Evals.Labels;
using LouisAgent.Evals.Trials;

namespace LouisAgent.Evals.Tests.Labels;

// F18 step 6.3, and 6a.0 (the 6.3 review): calibration asks a model grader about every labelled answer and counts how often it agrees with the person. Unknown ("can't tell") is not
// agreement, and each disagreement is listed with the label, the verdict and the judge's reason. The words are generic (pass, fail), so another judge can use them. The headline alone
// can be gamed, since a judge that always says yes scores the share of yes labels, so the report also splits the agreement by the label's class, and the bar (90%) holds in each class.
// The trial it builds has the answer in its turn, after the turns before it, so the grader sees the scenario's ask as in a run.
public class CalibrationTests
{
    private static EvalScenario Scenario(string id, string expected, params string[] messages) =>
        new(id, new EvalInput(null, "Africa/Johannesburg", messages), JsonDocument.Parse(expected).RootElement.Clone(), [], DatasetSplit.Dev);

    private static readonly IReadOnlyList<EvalScenario> Scenarios =
    [
        Scenario("cook-af", """{"serves":[4,6]}""", "Vir vier mense", "Maak dit vir ses"),
        Scenario("cook-en", """{"serves":2}""", "For two"),
    ];

    /// <summary>A label of <paramref name="text"/> as the answer of turn <paramref name="turn"/>; the turns before it answered "earlier n" (6a.4f: a label carries the whole conversation).</summary>
    private static LabeledAnswer Answer(string scenario, string text, bool pass, int turn = 1, int repeat = 1) =>
        new($"{scenario}.{repeat}.json", "meal-for-people", scenario, SampleMode.Beta, "8f7802b9a72e", repeat,
            [.. Enumerable.Range(1, turn).Select(n => new LabelTurn($"question {n}", n == turn ? text : $"earlier {n}"))], pass, $"note on {text}");

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
                "yes" => Verdict.Pass(Name, Version, "it passes"),
                "no" => Verdict.Fail(Name, Version, "it fails"),
                _ => Verdict.Unknown(Name, Version, "cannot tell"),
            });
        }
    }

    /// <summary>The labels of a judge's answers: <paramref name="pass"/> labels the person passed, <paramref name="fail"/> labels they failed, each as the judge is to see it.</summary>
    private static List<LabeledAnswer> Labels(int pass, int fail, string passText = "yes", string failText = "no") =>
        [.. Enumerable.Range(1, pass).Select(i => Answer("cook-en", passText, pass: true, repeat: i)), .. Enumerable.Range(1, fail).Select(i => Answer("cook-en", failText, pass: false, repeat: 100 + i))];

    [Test]
    public async Task ItCountsHowOftenTheJudgeAgreesWithThePerson()
    {
        var labels = new[] { Answer("cook-en", "yes", pass: true), Answer("cook-en", "no", pass: false, repeat: 2), Answer("cook-en", "yes", pass: false, repeat: 3) };

        var report = await Calibration.RunAsync(new WordGrader(), Scenarios, labels, CancellationToken.None);

        Assert.That((report.Agreed, report.Total), Is.EqualTo((2, 3)));
        Assert.That(report.Disagreements.Single().Answer.Repeat, Is.EqualTo(3));
        Assert.That((report.Disagreements.Single().Verdict, report.Disagreements.Single().Reason), Is.EqualTo((GradeOutcome.Pass, "it passes")));
    }

    [Test]
    public async Task ACantTell_IsNotAgreement_ItIsListedAsOne()
    {
        var report = await Calibration.RunAsync(new WordGrader(), Scenarios, [Answer("cook-en", "hmm", pass: true)], CancellationToken.None);

        Assert.That((report.Agreed, report.Total), Is.EqualTo((0, 1)));
        Assert.That(report.Disagreements.Single().Verdict, Is.EqualTo(GradeOutcome.Unknown));
    }

    [Test]
    public async Task TheTrialHasTheWholeConversation_TheEarlierAnswersToo_AndTheLabelledAnswerLast()
    {
        var grader = new WordGrader();

        await Calibration.RunAsync(grader, Scenarios, [Answer("cook-af", "yes", pass: true, turn: 2)], CancellationToken.None);

        var trial = grader.Asked.Single();
        Assert.That(trial.Scenario.Id, Is.EqualTo("cook-af"));
        Assert.That(trial.Transcript.Turns.Select(t => t.Turn), Is.EqualTo(new[] { 1, 2 }));
        Assert.That(trial.Transcript.Turns.Select(t => t.Question), Is.EqualTo(new[] { "question 1", "question 2" }), "the questions as the label recorded them");
        Assert.That(EngineEvents.AnswerOf(trial.Transcript.Turns[0]), Is.EqualTo("earlier 1"), "6a.4f: the judge sees what a follow-up relies on");
        Assert.That(EngineEvents.AnswerOf(trial.Transcript.Turns[1]), Is.EqualTo("yes"));
        Assert.That(trial.Mode.Name, Is.EqualTo(SampleMode.Beta));
    }

    [Test]
    public async Task TheReport_ReadsAsAgreementThenEachClassThenTheResultThenEachDisagreement()
    {
        var labels = new[] { Answer("cook-en", "yes", pass: true), Answer("cook-en", "yes", pass: false, repeat: 2) };

        var lines = (await Calibration.RunAsync(new WordGrader(), Scenarios, labels, CancellationToken.None)).Lines();

        Assert.That(lines[0], Is.EqualTo("Agreement: 1 of 2"));
        Assert.That(lines[1], Is.EqualTo("Labelled pass: judge pass 1, fail 0, can't tell 0 (1 of 1, 100%), few labels"));
        Assert.That(lines[2], Is.EqualTo("Labelled fail: judge pass 1, fail 0, can't tell 0 (0 of 1, 0%), few labels"));
        Assert.That(lines[3], Does.StartWith("Result: not enough").And.Contain("labelled fail"));
        Assert.That(lines[4], Does.Contain("cook-en").And.Contain("run 8f7802b9a72e").And.Contain("repeat 2").And.Contain("label: fail").And.Contain("judge: pass").And.Contain("it passes"));
        Assert.That(lines, Has.Count.EqualTo(5));
    }

    [Test]
    public async Task AJudgeThatAlwaysSaysPass_FailsTheBar_WhateverItsHeadlineScore()
    {
        // 14 labelled pass and 4 labelled fail, as Louis's 18 are: always saying pass agrees on 14 of 18 (78%) without judging anything.
        var report = await Calibration.RunAsync(new WordGrader(), Scenarios, Labels(pass: 14, fail: 4, failText: "yes"), CancellationToken.None);

        Assert.That((report.Agreed, report.Total), Is.EqualTo((14, 18)));
        Assert.That(report.Passes, Is.False, "0 of 4 on the labelled-fail class");
        Assert.That(report.Lines()[3], Does.StartWith("Result: not enough").And.Contain("labelled fail").And.Contain("0 of 4"));
    }

    [Test]
    public async Task TheBarIsNinetyPercentInEachClass_AndAClassOfFourNeedsAllFour()
    {
        var ok = await Calibration.RunAsync(new WordGrader(), Scenarios, Labels(pass: 14, fail: 4), CancellationToken.None);
        var oneMissed = await Calibration.RunAsync(new WordGrader(), Scenarios, [.. Labels(pass: 14, fail: 3), Answer("cook-en", "yes", pass: false, repeat: 200)], CancellationToken.None);
        var tenOfEleven = await Calibration.RunAsync(new WordGrader(), Scenarios, [.. Labels(pass: 10, fail: 4), Answer("cook-en", "no", pass: true, repeat: 300)], CancellationToken.None);

        Assert.That(ok.Passes, Is.True);
        Assert.That(oneMissed.Passes, Is.False, "3 of 4 is 75%");
        Assert.That(tenOfEleven.Passes, Is.True, "10 of 11 is 91%: above the bar");
    }

    [Test]
    public async Task ACantTell_CountsAgainstItsClass()
    {
        var report = await Calibration.RunAsync(new WordGrader(), Scenarios, [.. Labels(pass: 9, fail: 0), Answer("cook-en", "hmm", pass: true, repeat: 500)], CancellationToken.None);

        Assert.That(report.Lines()[1], Is.EqualTo("Labelled pass: judge pass 9, fail 0, can't tell 1 (9 of 10, 90%)"), "ten labels are no longer few");
    }

    [Test]
    public async Task AClassWithNoLabels_CannotBePassed_ItIsUntested()
    {
        var report = await Calibration.RunAsync(new WordGrader(), Scenarios, Labels(pass: 12, fail: 0), CancellationToken.None);

        Assert.That(report.Passes, Is.False);
        Assert.That(report.Lines()[2], Is.EqualTo("Labelled fail: no labels"));
        Assert.That(report.Lines()[3], Does.StartWith("Result: not enough").And.Contain("labelled fail").And.Contain("no labels"));
    }

    [Test]
    public async Task WithNothingLabelled_ThereIsNothingToAgreeWith()
    {
        var report = await Calibration.RunAsync(new WordGrader(), Scenarios, [], CancellationToken.None);

        Assert.That((report.Agreed, report.Total, report.Passes), Is.EqualTo((0, 0, false)));
        Assert.That(report.Lines()[0], Is.EqualTo("Agreement: 0 of 0"));
    }
}
