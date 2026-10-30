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
/// The <c>length-under</c> check: the first answer is shorter than the limit, in characters, and it grades a broken
/// turn the way the other general checks do.
/// </summary>
public class LengthUnderCheckTests
{
    /// <summary>A short answer passes, and the reason says its length and the limit.</summary>
    [Test]
    public async Task AShortAnswer_Passes_SayingItsLength()
    {
        var grade = await GradeAsync(new LengthUnderCheck(20), Trial(Turn(1, [Text("Bobotie vir vier.")])));

        Assert.That((grade.Outcome, grade.Score), Is.EqualTo((GradeOutcome.Pass, 1.0)));
        Assert.That(grade.Reason, Does.Contain("17").And.Contain("20"));
    }

    /// <summary>An answer as long as the limit isn't under it, so it fails, saying its length and the limit.</summary>
    [Test]
    public async Task AnAnswerAtTheLimit_Fails()
    {
        var grade = await GradeAsync(new LengthUnderCheck(17), Trial(Turn(1, [Text("Bobotie vir vier.")])));

        Assert.That((grade.Outcome, grade.Score), Is.EqualTo((GradeOutcome.Fail, 0.0)));
        Assert.That(grade.Reason, Does.Contain("17"));
    }

    /// <summary>A turn the infrastructure broke can't be judged, so it is Unknown, never a pass.</summary>
    [Test]
    public async Task AnInfrastructureFailure_IsUnknown()
    {
        var grade = await GradeAsync(new LengthUnderCheck(20), Trial(Turn(1, failure: TurnFailure.Timeout)));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Unknown));
    }

    /// <summary>
    /// The entry builds the check from a file's settings, under its name and version, required by default.
    /// </summary>
    [Test]
    public void TheEntry_BuildsTheCheckFromItsSettings()
    {
        var built = Build("""{"characters": 2000}""");

        Assert.That(built, Is.TypeOf<LengthUnderCheck>());
        Assert.That((built.Name, built.Version, built.Required), Is.EqualTo(("length-under", 1, true)));
    }

    /// <summary>A missing limit is refused, never read as a default, naming the check type and the field.</summary>
    [Test]
    public void AMissingLimit_IsRefused()
    {
        var refusal = Assert.Throws<InvalidDataException>(() => Build("{}"));

        Assert.That(refusal!.Message, Does.Contain("length-under").And.Contain("characters"));
    }

    /// <summary>Grades the trial with the check.</summary>
    private static async Task<Grade> GradeAsync(ICheck check, Trial trial) =>
        await check.GradeAsync(trial, CancellationToken.None);

    /// <summary>Builds the check the way an eval file does: from the general checks' catalogue, by its type.</summary>
    private static ICheck Build(string settings) =>
        new CheckCatalog(GeneralChecks.All).Get("length-under")
            .Create(JsonDocument.Parse(settings).RootElement.Clone(), CheckContext.None);
}
