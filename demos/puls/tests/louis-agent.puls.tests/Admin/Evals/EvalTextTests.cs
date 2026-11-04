using LouisAgent.Api.Contracts.Admin.Evals;
using LouisAgent.Api.Contracts.Admin.Runs;
using LouisAgent.Puls.Admin.Evals;

namespace LouisAgent.Puls.Tests.Admin.Evals;

/// <summary>
/// How the eval list words an eval: whose it is, its dataset and graders, its modes' rates, and whether it ran.
/// </summary>
public class EvalTextTests
{
    /// <summary>An eval with no run says so, and one with no store to ask doesn't claim either way.</summary>
    [TestCase(null, 0, "runs not known: no store")]
    [TestCase(true, 0, "never run")]
    [TestCase(false, 1, "1 run")]
    [TestCase(false, 3, "3 runs")]
    public void TheRunCount_SaysNeverRun_OrNotKnown(bool? neverRun, int runs, string expected) =>
        Assert.That(EvalText.Runs(neverRun, runs), Is.EqualTo(expected));

    /// <summary>An eval of no identity is the engine's own, as the stub is.</summary>
    [Test]
    public void AnEvalOfNoIdentity_IsTheEnginesOwn()
    {
        Assert.That(EvalText.Whose(null), Is.EqualTo("the engine's own"));
        Assert.That(EvalText.Whose(SampleIdentity.Alpha), Is.EqualTo(SampleIdentity.Alpha));
    }

    /// <summary>A dataset reads as its name and version; none is said, not left blank.</summary>
    [Test]
    public void TheDataset_IsItsNameAndVersion_OrNone()
    {
        Assert.That(EvalText.Dataset(new EvalDatasetRow("meals", 2)), Is.EqualTo("meals v2"));
        Assert.That(EvalText.Dataset(null), Is.EqualTo("none"));
    }

    /// <summary>What a grader judges with reads in words; a kind we don't know is shown as it came.</summary>
    [TestCase("FixedRule", "fixed rule")]
    [TestCase("Model", "model")]
    [TestCase("Human", "a person")]
    [TestCase("Oracle", "Oracle")]
    public void AGradersKind_ReadsInWords(string kind, string expected) =>
        Assert.That(EvalText.Kind(kind), Is.EqualTo(expected));

    /// <summary>A list of names is joined with commas; an empty one is a dash.</summary>
    [Test]
    public void AList_IsJoined_OrADash()
    {
        Assert.That(EvalText.Names(["town", "city"]), Is.EqualTo("town, city"));
        Assert.That(EvalText.Names([]), Is.EqualTo("—"));
    }

    /// <summary>The version is "v" and the number; one we don't know is a question mark.</summary>
    [Test]
    public void TheVersion_IsVAndTheNumber()
    {
        Assert.That(EvalText.Version(3), Is.EqualTo("v3"));
        Assert.That(EvalText.Version(null), Is.EqualTo("v?"));
    }

    /// <summary>
    /// A run's tag says where its results came from, and names the run a replay or re-score came from.
    /// </summary>
    [TestCase(RunOrigin.Recorded, null, "recorded")]
    [TestCase(RunOrigin.Replay, "dc272b95eaeb", "replay of dc272b95")]
    [TestCase(RunOrigin.Rescore, "5d1fd231c9b1", "re-scored from 5d1fd231")]
    public void ARunsTag_SaysWhereItCameFrom(RunOrigin origin, string? source, string expected) =>
        Assert.That(EvalText.Origin(origin, source), Is.EqualTo(expected));

    /// <summary>Each origin has its own colour, and only a recording's hover says the model answered.</summary>
    [TestCase(RunOrigin.Recorded, "is-recorded", "answered live")]
    [TestCase(RunOrigin.Replay, "is-replay", "no model was asked")]
    [TestCase(RunOrigin.Rescore, "is-rescore", "No model was asked")]
    public void ARunsTag_HasItsColourAndItsHint(RunOrigin origin, string tone, string hint)
    {
        Assert.That(EvalText.OriginTone(origin), Is.EqualTo(tone));
        Assert.That(EvalText.OriginHint(origin), Does.Contain(hint));
    }

    /// <summary>
    /// A rate's ring is the share passed and the rest; with no rate, nothing is drawn but the empty track, never a 0.
    /// </summary>
    [Test]
    public void ARatesRing_IsTheSharePassedAndTheRest()
    {
        Assert.That(EvalText.Ring(0.25), Is.EqualTo(new[] { 0.25, 0.75 }));
        Assert.That(EvalText.Ring(1.0), Is.EqualTo(new[] { 1.0, 0.0 }));
        Assert.That(EvalText.Ring(null), Is.EqualTo(new[] { 0.0, 0.0 }));
    }

    /// <summary>The fold's note counts what the eval checks, one or many.</summary>
    [Test]
    public void TheFoldsNote_CountsWhatItChecks()
    {
        Assert.That(EvalText.Checks(9, 9, 2), Is.EqualTo("9 graders · 9 categories · 2 modes"));
        Assert.That(EvalText.Checks(1, 0, 1), Is.EqualTo("1 grader · 0 categories · 1 mode"));
    }

    /// <summary>A grader kind's colour on the card: a rule, a model judge, a person, or one we don't know.</summary>
    [TestCase("FixedRule", "rule")]
    [TestCase("Model", "model")]
    [TestCase("Human", "person")]
    [TestCase("Oracle", "other")]
    public void AGraderKind_HasItsTone(string kind, string tone) =>
        Assert.That(EvalText.Tone(kind), Is.EqualTo(tone));

    /// <summary>A run's start reads as a day, a month, a year and a time, in the reader's own time.</summary>
    [Test]
    public void ARunsStart_ReadsAsADayAndATime()
    {
        // Noon UTC, so every time zone the reader can be in still reads the 29th.
        var at = new DateTimeOffset(2026, 10, 29, 12, 0, 0, TimeSpan.Zero);

        Assert.That(EvalText.When(at), Does.StartWith("29 Oct 2026, "));
    }
}
