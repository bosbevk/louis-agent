using LouisAgent.Testing;
using LouisAgent.Evals.Datasets;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Grading.Common;
using LouisAgent.Evals.Trials;
using LouisAgent.Evals.Grading.Outcomes;

namespace LouisAgent.Evals.Tests.Grading.Outcomes;

// F18 step 5a: pass@k and pass^k over a scenario's trials. pass@k: it passed at least once in its k trials ("can the agent do this?"); pass^k: it passed
// every time ("can a customer rely on it?"). An Unknown never counts as a pass; an infrastructure trial is neither a pass nor a fail and is left out of k.
// Written first: Over, ShareOf and TrialOutcome.Infrastructure did not exist, and Of had no trial to read a failure from.
public class PassRatesOverTrialsTests
{
    private const TrialOutcome P = TrialOutcome.Pass;
    private const TrialOutcome F = TrialOutcome.Fail;
    private const TrialOutcome U = TrialOutcome.Unknown;
    private const TrialOutcome I = TrialOutcome.Infrastructure;

    private static TurnTranscript Turn(int number, TrialFailure? failure = null) =>
        new(number, "sess", "Vraag", [], TimeSpan.FromSeconds(1), failure);

    private static Trial TrialOf(params TurnTranscript[] turns) =>
        new("run_1", new EvalMode("cook", SampleRole.Alpha, [SampleToolset.Alpha]), 1,
            new EvalScenario("pannekoek", new EvalInput(null, "Africa/Johannesburg", ["Vraag"]), default, [],
                DatasetSplit.Dev), new Transcript(turns));

    private static readonly IGrader[] Required = [new RequiredGrader()];

    // --- a trial's outcome

    [Test]
    public void ATrialWhoseTurnFailedForTheInfrastructure_IsInfrastructure_WhateverItsGradesSay()
    {
        var grades = new[] { Verdict.Pass("always", 1, "fine") };

        Assert.That(PassRates.Of(TrialOf(Turn(1), Turn(2, TrialFailure.Timeout)), grades, Required),
            Is.EqualTo(TrialOutcome.Infrastructure));
        Assert.That(
            PassRates.Of(TrialOf(Turn(1, TrialFailure.ReplayMiss)), [Verdict.Fail("always", 1, "wrong")], Required),
            Is.EqualTo(TrialOutcome.Infrastructure));
    }

    [Test]
    public void ATrialWithNoFailedTurn_KeepsTheRulesOfItsGrades()
    {
        Assert.That(PassRates.Of(TrialOf(Turn(1)), [Verdict.Pass("always", 1, "fine")], Required),
            Is.EqualTo(TrialOutcome.Pass));
        Assert.That(PassRates.Of(TrialOf(Turn(1)), [Verdict.Fail("always", 1, "wrong")], Required),
            Is.EqualTo(TrialOutcome.Fail));
        Assert.That(PassRates.Of(TrialOf(Turn(1)), [Verdict.Unknown("always", 1, "can't tell")], Required),
            Is.EqualTo(TrialOutcome.Unknown));
    }

    // --- one scenario over its k trials

    [Test]
    public void APassBesideAFail_IsPassAtKButNotPassHatK() =>
        Assert.That(PassRates.Over([P, F, P]), Is.EqualTo(new PassRate(1, 0, 3)));

    [Test]
    public void EveryTrialPassing_IsBoth() =>
        Assert.That(PassRates.Over([P, P]), Is.EqualTo(new PassRate(1, 1, 2)));

    [Test]
    public void NoPass_IsNeither_AndAnUnknownIsNeverAPass() =>
        Assert.That(PassRates.Over([F, U]), Is.EqualTo(new PassRate(0, 0, 2)));

    [Test]
    public void AnInfrastructureTrial_IsLeftOutOfK() =>
        Assert.That(PassRates.Over([P, I]), Is.EqualTo(new PassRate(1, 1, 1)));

    [Test]
    public void NoCountableTrial_IsNullNeverZero()
    {
        Assert.That(PassRates.Over([I]), Is.EqualTo(new PassRate(null, null, 0)));
        Assert.That(PassRates.Over([]), Is.EqualTo(new PassRate(null, null, 0)));
    }

    [Test]
    public void WithOneTrial_PassAtOneAndPassHatOneAreTheSameNumber()
    {
        Assert.That(PassRates.Over([P]), Is.EqualTo(new PassRate(1, 1, 1)));
        Assert.That(PassRates.Over([F]), Is.EqualTo(new PassRate(0, 0, 1)));
    }

    // --- a mode: the share of its scenarios

    [Test]
    public void AModesShare_IsTheShareOfItsScenariosThatPassThatWay()
    {
        var scenarios = new[]
            { PassRates.Over([P, F]), PassRates.Over([P, P]), PassRates.Over([F, F]), PassRates.Over([P, P]) };

        var share = PassRates.ShareOf(scenarios);

        Assert.That(share.PassAtK, Is.EqualTo(0.75), "three of four passed at least once");
        Assert.That(share.PassHatK, Is.EqualTo(0.5), "two of four passed every time");
    }

    [Test]
    public void AScenarioWithNoCountableTrial_IsNoPartOfTheShare()
    {
        var share = PassRates.ShareOf([PassRates.Over([P, P]), PassRates.Over([I]), PassRates.Over([F, F])]);

        Assert.That(share, Is.EqualTo(new PassShare(0.5, 0.5)), "two scenarios count; the infrastructure one doesn't");
    }

    [Test]
    public void AModeWithNoCountableScenario_HasNoShare()
    {
        Assert.That(PassRates.ShareOf([PassRates.Over([I])]), Is.EqualTo(new PassShare(null, null)));
        Assert.That(PassRates.ShareOf([]), Is.EqualTo(new PassShare(null, null)));
    }

    private sealed class RequiredGrader : IGrader
    {
        public string Name => "always";
        public int Version => 1;
        public GraderKind Kind => GraderKind.FixedRule;
        public IReadOnlySet<string> ExpectedKeys { get; } = new HashSet<string>();
        public bool Required => true;

        public ValueTask<Grade> GradeAsync(Trial trial, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
