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
/// The <c>not-contains</c> check: the first answer holds none of the texts, case and accents ignored, and it grades a
/// broken turn the way the other general checks do.
/// </summary>
public class NotContainsCheckTests
{
    /// <summary>An answer with none of the texts passes.</summary>
    [Test]
    public async Task NoneOfTheTexts_Passes()
    {
        var grade = await GradeAsync(new NotContainsCheck(["as an AI", "I cannot"]),
            Trial(Turn(1, [Text("Hier is die resep vir bobotie.")])));

        Assert.That((grade.Outcome, grade.Score), Is.EqualTo((GradeOutcome.Pass, 1.0)));
    }

    /// <summary>One text found fails, and the reason names it, with case and accents ignored.</summary>
    [Test]
    public async Task AText_InTheAnswer_Fails_NamingIt()
    {
        var grade = await GradeAsync(new NotContainsCheck(["creme brulee"]),
            Trial(Turn(1, [Text("Try a Crème Brûlée for dessert.")])));

        Assert.That((grade.Outcome, grade.Score), Is.EqualTo((GradeOutcome.Fail, 0.0)));
        Assert.That(grade.Reason, Does.Contain("creme brulee"));
    }

    /// <summary>A turn the infrastructure broke can't be judged, so it is Unknown, never a pass.</summary>
    [Test]
    public async Task AnInfrastructureFailure_IsUnknown()
    {
        var grade = await GradeAsync(new NotContainsCheck(["recipe"]), Trial(Turn(1, failure: TurnFailure.Timeout)));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Unknown));
    }

    /// <summary>
    /// The entry builds the check from a file's settings, under its name and version, required by default.
    /// </summary>
    [Test]
    public void TheEntry_BuildsTheCheckFromItsSettings()
    {
        var built = Build("""{"texts": ["as an AI"]}""");

        Assert.That(built, Is.TypeOf<NotContainsCheck>());
        Assert.That((built.Name, built.Version, built.Required), Is.EqualTo(("not-contains", 1, true)));
    }

    /// <summary>An empty or missing list is refused, naming the check type and the field.</summary>
    [TestCase("""{"texts": []}""")]
    [TestCase("{}")]
    public void NoTexts_AreRefused(string settings)
    {
        var refusal = Assert.Throws<InvalidDataException>(() => Build(settings));

        Assert.That(refusal!.Message, Does.Contain("not-contains").And.Contain("texts"));
    }

    /// <summary>Grades the trial with the check.</summary>
    private static async Task<Grade> GradeAsync(ICheck check, Trial trial) =>
        await check.GradeAsync(trial, CancellationToken.None);

    /// <summary>Builds the check the way an eval file does: from the general checks' catalogue, by its type.</summary>
    private static ICheck Build(string settings) =>
        new CheckCatalog(GeneralChecks.All).Get("not-contains")
            .Create(JsonDocument.Parse(settings).RootElement.Clone(), CheckContext.None);
}
