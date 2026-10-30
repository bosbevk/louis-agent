using System.Text.Json;
using LouisAgent.Evals.Catalog;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Grading.Common;
using LouisAgent.Evals.Grading.Common.Facts;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Trials;
using static LouisAgent.Evals.Tests.Trials.TrialBuilder;

namespace LouisAgent.Evals.Tests.Grading.Common.Facts;

/// <summary>
/// The <c>tokens-under</c> check: the trial's tokens, the four counts of every turn summed, stay under the limit. A
/// turn that reported no usage makes it Unknown, as <c>cost-under</c> does, never a pass.
/// </summary>
public class TokensUnderCheckTests
{
    /// <summary>A total under the limit passes, and the reason says the total and the limit.</summary>
    [Test]
    public async Task ATotalUnderTheLimit_Passes_SayingTheTotal()
    {
        var grade = await GradeAsync(new TokensUnderCheck(1000),
            Trial(Turn(1, [Tokens(10, 100, 200, 50)]), Turn(2, [Tokens(10, 0, 300, 40)])));

        Assert.That((grade.Outcome, grade.Score), Is.EqualTo((GradeOutcome.Pass, 1.0)));
        Assert.That(grade.Reason, Does.Contain("710").And.Contain("1000"));
    }

    /// <summary>A total at or over the limit fails, saying the total.</summary>
    [Test]
    public async Task ATotalAtTheLimit_Fails()
    {
        var grade = await GradeAsync(new TokensUnderCheck(710),
            Trial(Turn(1, [Tokens(10, 100, 200, 50)]), Turn(2, [Tokens(10, 0, 300, 40)])));

        Assert.That((grade.Outcome, grade.Score), Is.EqualTo((GradeOutcome.Fail, 0.0)));
        Assert.That(grade.Reason, Does.Contain("710"));
    }

    /// <summary>
    /// A turn that reported no usage leaves the total unknown, so the grade is Unknown, naming the turn.
    /// </summary>
    [Test]
    public async Task ATurnWithoutUsage_IsUnknown()
    {
        var grade = await GradeAsync(new TokensUnderCheck(1000),
            Trial(Turn(1, [Tokens(10, 100, 200, 50)]), Turn(2, [Text("Hallo")])));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Unknown));
        Assert.That(grade.Reason, Does.Contain("turn 2"));
    }

    /// <summary>A turn the infrastructure broke can't be judged, so it is Unknown, never a pass.</summary>
    [Test]
    public async Task AnInfrastructureFailure_IsUnknown()
    {
        var grade = await GradeAsync(new TokensUnderCheck(1000),
            Trial(Turn(1, [Tokens(10, 100, 200, 50)], failure: TurnFailure.Timeout)));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Unknown));
    }

    /// <summary>
    /// The entry builds the check from a file's settings, under its name and version, required by default.
    /// </summary>
    [Test]
    public void TheEntry_BuildsTheCheckFromItsSettings()
    {
        var built = Build("""{"count": 100000}""");

        Assert.That(built, Is.TypeOf<TokensUnderCheck>());
        Assert.That((built.Name, built.Version, built.Required), Is.EqualTo(("tokens-under", 1, true)));
    }

    /// <summary>A missing limit is refused, never read as a default, naming the check type and the field.</summary>
    [Test]
    public void AMissingLimit_IsRefused()
    {
        var refusal = Assert.Throws<InvalidDataException>(() => Build("{}"));

        Assert.That(refusal!.Message, Does.Contain("tokens-under").And.Contain("count"));
    }

    /// <summary>Grades the trial with the check.</summary>
    private static async Task<Grade> GradeAsync(ICheck check, Trial trial) =>
        await check.GradeAsync(trial, CancellationToken.None);

    /// <summary>Builds the check the way an eval file does: from the general checks' catalogue, by its type.</summary>
    private static ICheck Build(string settings) =>
        new CheckCatalog(GeneralChecks.All).Get("tokens-under")
            .Create(JsonDocument.Parse(settings).RootElement.Clone(), CheckContext.None);
}
