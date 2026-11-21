using System.Text.Json;
using LouisAgent.Evals.Catalog;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Grading.Common;
using LouisAgent.Evals.Grading.Common.Answers;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Trials;
using static LouisAgent.Evals.Tests.Trials.TrialBuilder;

namespace LouisAgent.Evals.Tests.Grading.Common.Answers;

/// <summary>
/// The <c>contains</c> check: the first turn's answer holds at least one of the eval's texts, case and accents
/// ignored, and it grades a broken turn the way the other general checks do.
/// </summary>
public class ContainsCheckTests
{
    /// <summary>One of the two texts is enough, and a different case doesn't stop the match.</summary>
    [Test]
    public async Task OneOfTheTexts_InTheAnswer_Passes()
    {
        var grade = await GradeAsync(new ContainsCheck(["recipe", "resep"]),
            Trial(Turn(1, [Text("Hier is die Resep vir bobotie.")])));

        Assert.That((grade.Outcome, grade.Score), Is.EqualTo((GradeOutcome.Pass, 1.0)));
        Assert.That(grade.Reason, Does.Contain("resep"));
    }

    /// <summary>An accent the text leaves out still matches, so "creme brulee" finds "Crème Brûlée".</summary>
    [Test]
    public async Task AnAccentLeftOut_StillMatches()
    {
        var grade = await GradeAsync(new ContainsCheck(["creme brulee"]),
            Trial(Turn(1, [Text("Try a Crème Brûlée for dessert.")])));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Pass));
    }

    /// <summary>When none of the texts is there it fails, and the reason says how many were looked for.</summary>
    [Test]
    public async Task NoneOfTheTexts_Fails()
    {
        var grade = await GradeAsync(new ContainsCheck(["recipe", "resep"]),
            Trial(Turn(1, [Text("Ek weet nie.")])));

        Assert.That((grade.Outcome, grade.Score), Is.EqualTo((GradeOutcome.Fail, 0.0)));
        Assert.That(grade.Reason, Does.Contain("2"));
    }

    /// <summary>Only the first turn's answer counts: a text that turns up later doesn't.</summary>
    [Test]
    public async Task AText_InALaterTurn_DoesNotCount()
    {
        var grade = await GradeAsync(new ContainsCheck(["recipe"]),
            Trial(Turn(1, [Text("Ek weet nie.")]), Turn(2, [Text("Here is the recipe.")])));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Fail));
    }

    /// <summary>A turn the infrastructure broke can't be judged, so it is Unknown, never a fail.</summary>
    [Test]
    public async Task AnInfrastructureFailure_IsUnknown()
    {
        var grade = await GradeAsync(new ContainsCheck(["recipe"]), Trial(Turn(1, failure: TurnFailure.Timeout)));

        Assert.That((grade.Outcome, grade.Score), Is.EqualTo((GradeOutcome.Unknown, 0.0)));
    }

    /// <summary>A turn that streamed an error fails, even when the text got out before it.</summary>
    [Test]
    public async Task AStreamedError_Fails()
    {
        var grade = await GradeAsync(new ContainsCheck(["recipe"]), Trial(Turn(1, [Text("The recipe"), Error()])));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Fail));
    }

    /// <summary>A trial with no turn has no answer to read, so it is Unknown.</summary>
    [Test]
    public async Task NoTurn_IsUnknown()
    {
        var grade = await GradeAsync(new ContainsCheck(["recipe"]), Trial());

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Unknown));
    }

    /// <summary>
    /// The entry builds the check from a file's settings, under its name and version, required by default.
    /// </summary>
    [Test]
    public void TheEntry_BuildsTheCheckFromItsSettings()
    {
        var built = Build("""{"texts": ["recipe", "resep"]}""");

        Assert.That(built, Is.TypeOf<ContainsCheck>());
        Assert.That((built.Name, built.Version, built.Required), Is.EqualTo(("contains", 1, true)));
    }

    /// <summary>An empty or missing list is refused, naming the check type and the field.</summary>
    [TestCase("""{"texts": []}""")]
    [TestCase("{}")]
    public void NoTexts_AreRefused(string settings)
    {
        var refusal = Assert.Throws<InvalidDataException>(() => Build(settings));

        Assert.That(refusal!.Message, Does.Contain("contains").And.Contain("texts"));
    }

    /// <summary>Grades the trial with the check.</summary>
    private static async Task<Grade> GradeAsync(ICheck check, Trial trial) =>
        await check.GradeAsync(trial, CancellationToken.None);

    /// <summary>Builds the check the way an eval file will: from the general checks' catalogue, by its type.</summary>
    private static ICheck Build(string settings) =>
        new CheckCatalog(GeneralChecks.All).Get("contains")
            .Create(JsonDocument.Parse(settings).RootElement.Clone(), CheckContext.None);
}
