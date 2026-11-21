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
/// The <c>starts-with</c> check: the first answer, trimmed, starts with the text, case ignored, and it grades a broken
/// turn the way the other general checks do.
/// </summary>
public class StartsWithCheckTests
{
    /// <summary>Leading spaces and a different case don't stop the match.</summary>
    [Test]
    public async Task TheTrimmedAnswer_StartingWithTheText_Passes()
    {
        var grade = await GradeAsync(new StartsWithCheck("bobotie"),
            Trial(Turn(1, [Text("  Bobotie vir vier: 500 g maalvleis.")])));

        Assert.That((grade.Outcome, grade.Score), Is.EqualTo((GradeOutcome.Pass, 1.0)));
    }

    /// <summary>An answer that starts otherwise fails, and the reason names the text it should start with.</summary>
    [Test]
    public async Task AnotherStart_Fails_NamingTheText()
    {
        var grade = await GradeAsync(new StartsWithCheck("bobotie"),
            Trial(Turn(1, [Text("Here is a bobotie for four.")])));

        Assert.That((grade.Outcome, grade.Score), Is.EqualTo((GradeOutcome.Fail, 0.0)));
        Assert.That(grade.Reason, Does.Contain("bobotie"));
    }

    /// <summary>A turn the infrastructure broke can't be judged, so it is Unknown, never a fail.</summary>
    [Test]
    public async Task AnInfrastructureFailure_IsUnknown()
    {
        var grade = await GradeAsync(new StartsWithCheck("bobotie"), Trial(Turn(1, failure: TurnFailure.Timeout)));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Unknown));
    }

    /// <summary>
    /// The entry builds the check from a file's settings, under its name and version, required by default.
    /// </summary>
    [Test]
    public void TheEntry_BuildsTheCheckFromItsSettings()
    {
        var built = Build("""{"text": "bobotie"}""");

        Assert.That(built, Is.TypeOf<StartsWithCheck>());
        Assert.That((built.Name, built.Version, built.Required), Is.EqualTo(("starts-with", 1, true)));
    }

    /// <summary>
    /// An empty or missing text is refused, naming the check type and the field: every answer would pass.
    /// </summary>
    [TestCase("""{"text": " "}""")]
    [TestCase("{}")]
    public void NoText_IsRefused(string settings)
    {
        var refusal = Assert.Throws<InvalidDataException>(() => Build(settings));

        Assert.That(refusal!.Message, Does.Contain("starts-with").And.Contain("text"));
    }

    /// <summary>Grades the trial with the check.</summary>
    private static async Task<Grade> GradeAsync(ICheck check, Trial trial) =>
        await check.GradeAsync(trial, CancellationToken.None);

    /// <summary>Builds the check the way an eval file does: from the general checks' catalogue, by its type.</summary>
    private static ICheck Build(string settings) =>
        new CheckCatalog(GeneralChecks.All).Get("starts-with")
            .Create(JsonDocument.Parse(settings).RootElement.Clone(), CheckContext.None);
}
