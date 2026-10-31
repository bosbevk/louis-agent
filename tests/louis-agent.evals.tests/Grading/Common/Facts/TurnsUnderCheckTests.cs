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
/// The <c>turns-under</c> check: the trial took fewer turns than the limit, and a turn the infrastructure broke makes
/// it Unknown.
/// </summary>
public class TurnsUnderCheckTests
{
    /// <summary>Fewer turns than the limit pass, and the reason says how many and the limit.</summary>
    [Test]
    public async Task FewerTurns_Pass_SayingHowMany()
    {
        var grade = await GradeAsync(new TurnsUnderCheck(3), Trial(Turn(1), Turn(2)));

        Assert.That((grade.Outcome, grade.Score), Is.EqualTo((GradeOutcome.Pass, 1.0)));
        Assert.That(grade.Reason, Does.Contain("2").And.Contain("3"));
    }

    /// <summary>As many turns as the limit aren't under it, so they fail.</summary>
    [Test]
    public async Task AsManyTurnsAsTheLimit_Fail()
    {
        var grade = await GradeAsync(new TurnsUnderCheck(2), Trial(Turn(1), Turn(2)));

        Assert.That((grade.Outcome, grade.Score), Is.EqualTo((GradeOutcome.Fail, 0.0)));
        Assert.That(grade.Reason, Does.Contain("2"));
    }

    /// <summary>A turn the infrastructure broke can't be judged, so it is Unknown, never a pass.</summary>
    [Test]
    public async Task AnInfrastructureFailure_IsUnknown()
    {
        var grade = await GradeAsync(new TurnsUnderCheck(3), Trial(Turn(1, failure: TurnFailure.Timeout)));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Unknown));
    }

    /// <summary>
    /// The entry builds the check from a file's settings, under its name and version, required by default.
    /// </summary>
    [Test]
    public void TheEntry_BuildsTheCheckFromItsSettings()
    {
        var built = Build("""{"count": 4}""");

        Assert.That(built, Is.TypeOf<TurnsUnderCheck>());
        Assert.That((built.Name, built.Version, built.Required), Is.EqualTo(("turns-under", 1, true)));
    }

    /// <summary>
    /// A limit under one, or none, is refused, naming the check type and the field: no trial could pass.
    /// </summary>
    [TestCase("""{"count": 0}""")]
    [TestCase("{}")]
    public void ALimitUnderOne_IsRefused(string settings)
    {
        var refusal = Assert.Throws<InvalidDataException>(() => Build(settings));

        Assert.That(refusal!.Message, Does.Contain("turns-under").And.Contain("count"));
    }

    /// <summary>Grades the trial with the check.</summary>
    private static async Task<Grade> GradeAsync(ICheck check, Trial trial) =>
        await check.GradeAsync(trial, CancellationToken.None);

    /// <summary>Builds the check the way an eval file does: from the general checks' catalogue, by its type.</summary>
    private static ICheck Build(string settings) =>
        new CheckCatalog(GeneralChecks.All).Get("turns-under")
            .Create(JsonDocument.Parse(settings).RootElement.Clone(), CheckContext.None);
}
