using System.Text.Json;
using LouisAgent.Evals.Catalog;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Grading.Common;
using LouisAgent.Evals.Grading.Common.Similarity;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Trials;
using static LouisAgent.Evals.Tests.Trials.TrialBuilder;

namespace LouisAgent.Evals.Tests.Grading.Common.Similarity;

/// <summary>
/// The <c>near</c> check: the whole first answer, trimmed and case ignored, is within a number of edits (letters
/// added, removed or changed) of the text: for a short fixed answer, a label or a code.
/// </summary>
public class NearCheckTests
{
    /// <summary>Within the edits passes, saying how many it took.</summary>
    [TestCase("Bobotie", 0)]
    [TestCase("  BOBOTIE \n", 0)]
    [TestCase("Bobotle", 1)]
    [TestCase("Bobotie!", 1)]
    [TestCase("Bobti", 2)]
    public async Task WithinTheEdits_Passes(string answer, int edits)
    {
        var grade = await GradeAsync(new NearCheck("bobotie", 2), Trial(Turn(1, [Text(answer)])));

        Assert.That((grade.Outcome, grade.Score), Is.EqualTo((GradeOutcome.Pass, 1.0)));
        Assert.That(grade.Reason, Does.Contain($"{edits} edits"));
    }

    /// <summary>More edits than allowed fails, saying how many it took and the limit.</summary>
    [Test]
    public async Task MoreEdits_Fails()
    {
        var grade = await GradeAsync(new NearCheck("bobotie", 2), Trial(Turn(1, [Text("Bobotie met rys")])));

        Assert.That((grade.Outcome, grade.Score), Is.EqualTo((GradeOutcome.Fail, 0.0)));
        Assert.That(grade.Reason, Does.Contain("8 edits").And.Contain("2"));
    }

    /// <summary>
    /// No turn, or one the infrastructure broke, is Unknown; a turn that streamed an error fails.
    /// </summary>
    [Test]
    public async Task ABrokenTurn_IsUnknown_AndAFailedOne_Fails()
    {
        var check = new NearCheck("bobotie", 2);

        Assert.That((await GradeAsync(check, Trial())).Outcome, Is.EqualTo(GradeOutcome.Unknown));
        Assert.That((await GradeAsync(check, Trial(Turn(1, failure: TurnFailure.Timeout)))).Outcome,
            Is.EqualTo(GradeOutcome.Unknown));
        Assert.That((await GradeAsync(check, Trial(Turn(1, [Text("bobotie"), Error()])))).Outcome,
            Is.EqualTo(GradeOutcome.Fail));
    }

    /// <summary>
    /// The entry builds the check from a file's settings; a blank text or a distance under zero is refused.
    /// </summary>
    [Test]
    public void TheEntry_BuildsTheCheck_AndRefusesBadSettings()
    {
        var built = Build("""{"text": "bobotie", "distance": 2, "required": false}""");

        Assert.That(built, Is.TypeOf<NearCheck>());
        Assert.That((built.Name, built.Version, built.Required), Is.EqualTo(("near", 1, false)));
        Assert.That(Assert.Throws<InvalidDataException>(() => Build("""{"text": " ", "distance": 2}"""))!.Message,
            Does.Contain("text"));
        Assert.That(Assert.Throws<InvalidDataException>(() => Build("""{"text": "x", "distance": -1}"""))!.Message,
            Does.Contain("distance"));
    }

    /// <summary>Grades the trial with the check.</summary>
    private static async Task<Grade> GradeAsync(ICheck check, Trial trial) =>
        await check.GradeAsync(trial, CancellationToken.None);

    /// <summary>Builds the check the way an eval file does, by its type.</summary>
    private static ICheck Build(string settings) =>
        new CheckCatalog(GeneralChecks.All).Get("near")
            .Create(JsonDocument.Parse(settings).RootElement.Clone(), CheckContext.None);
}
