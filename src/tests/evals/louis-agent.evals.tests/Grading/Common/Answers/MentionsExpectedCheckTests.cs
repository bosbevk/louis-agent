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
/// The <c>mentions-expected</c> check: the first answer names the value the scenario expects under a key (the town,
/// the shop), case and accents ignored. A scenario without the key is refused, as <c>routed-to</c> refuses one.
/// </summary>
public class MentionsExpectedCheckTests
{
    /// <summary>The expected text in the answer passes, with case and accents ignored.</summary>
    [Test]
    public async Task TheExpectedText_InTheAnswer_Passes()
    {
        var grade = await GradeAsync(new MentionsExpectedCheck("town"),
            Trial(Mode, """{"town": "Paarl"}""", Turn(1, [Text("Die winkels in PAARL is oop.")])));

        Assert.That((grade.Outcome, grade.Score), Is.EqualTo((GradeOutcome.Pass, 1.0)));
        Assert.That(grade.Reason, Does.Contain("Paarl"));
    }

    /// <summary>An expected number is looked for as its JSON text.</summary>
    [Test]
    public async Task AnExpectedNumber_IsLookedForAsItsText()
    {
        var grade = await GradeAsync(new MentionsExpectedCheck("serves"),
            Trial(Mode, """{"serves": 4}""", Turn(1, [Text("Bobotie vir 4 mense.")])));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Pass));
    }

    /// <summary>An answer that doesn't name the value fails, and the reason names the value.</summary>
    [Test]
    public async Task AnAnswerWithoutTheValue_Fails_NamingIt()
    {
        var grade = await GradeAsync(new MentionsExpectedCheck("town"),
            Trial(Mode, """{"town": "Paarl"}""", Turn(1, [Text("Die winkels is oop.")])));

        Assert.That((grade.Outcome, grade.Score), Is.EqualTo((GradeOutcome.Fail, 0.0)));
        Assert.That(grade.Reason, Does.Contain("Paarl"));
    }

    /// <summary>
    /// A scenario that doesn't set the key is refused, naming the scenario and the key: a pass would hide a dataset
    /// mistake.
    /// </summary>
    [Test]
    public void AScenarioWithoutTheKey_IsRefused()
    {
        var refusal = Assert.ThrowsAsync<InvalidDataException>(async () =>
            await GradeAsync(new MentionsExpectedCheck("town"), Trial(Mode, "{}", Turn(1, [Text("Paarl")]))));

        Assert.That(refusal!.Message, Does.Contain("bobotie").And.Contain("town"));
    }

    /// <summary>A turn the infrastructure broke can't be judged, so it is Unknown, never a fail.</summary>
    [Test]
    public async Task AnInfrastructureFailure_IsUnknown()
    {
        var grade = await GradeAsync(new MentionsExpectedCheck("town"),
            Trial(Mode, """{"town": "Paarl"}""", Turn(1, failure: TurnFailure.Timeout)));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Unknown));
    }

    /// <summary>
    /// The entry builds the check from a file's settings, and the check lists its key, so the loader can see the key
    /// is read and fed.
    /// </summary>
    [Test]
    public void TheEntry_BuildsTheCheck_ThatListsItsKey()
    {
        var built = Build("""{"key": "town"}""");

        Assert.That(built, Is.TypeOf<MentionsExpectedCheck>());
        Assert.That((built.Name, built.Version, built.Required), Is.EqualTo(("mentions-expected", 1, true)));
        Assert.That(built.ExpectedKeys, Is.EqualTo(new[] { "town" }));
    }

    /// <summary>A missing key is refused, naming the check type and the field.</summary>
    [Test]
    public void AMissingKey_IsRefused()
    {
        var refusal = Assert.Throws<InvalidDataException>(() => Build("{}"));

        Assert.That(refusal!.Message, Does.Contain("mentions-expected").And.Contain("key"));
    }

    /// <summary>Grades the trial with the check.</summary>
    private static async Task<Grade> GradeAsync(ICheck check, Trial trial) =>
        await check.GradeAsync(trial, CancellationToken.None);

    /// <summary>Builds the check the way an eval file does: from the general checks' catalogue, by its type.</summary>
    private static ICheck Build(string settings) =>
        new CheckCatalog(GeneralChecks.All).Get("mentions-expected")
            .Create(JsonDocument.Parse(settings).RootElement.Clone(), CheckContext.None);
}
