using System.Text.Json;
using LouisAgent.Evals.Catalog;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Grading.Common;
using LouisAgent.Evals.Grading.Common.Tools;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Trials;
using static LouisAgent.Evals.Tests.Trials.TrialBuilder;

namespace LouisAgent.Evals.Tests.Grading.Common.Tools;

/// <summary>
/// The <c>tool-used-with</c> check: someone in the trial called the tool with an input field holding the value, as
/// text, case and accents ignored.
/// </summary>
public class ToolUsedWithCheckTests
{
    /// <summary>A call whose field holds the value passes, whatever its case, and the reason names the turn.</summary>
    [Test]
    public async Task ACallWithTheValue_Passes()
    {
        var grade = await GradeAsync(new ToolUsedWithCheck("FindProducts", "shop", "checkers"),
            Trial(Turn(1, [ToolCall("FindProducts", """{"shop":"Checkers","query":"hoender"}""")])));

        Assert.That((grade.Outcome, grade.Score), Is.EqualTo((GradeOutcome.Pass, 1.0)));
        Assert.That(grade.Reason, Does.Contain("turn 1"));
    }

    /// <summary>A number in the input is compared as its JSON text.</summary>
    [Test]
    public async Task ANumber_IsComparedAsItsText()
    {
        var grade = await GradeAsync(new ToolUsedWithCheck("Scale", "serves", "4"),
            Trial(Turn(1, [ToolCall("Scale", """{"serves":4}""")])));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Pass));
    }

    /// <summary>A specialist's call counts, as it does for <c>tool-used</c>.</summary>
    [Test]
    public async Task ASpecialistsCall_Counts()
    {
        var grade = await GradeAsync(new ToolUsedWithCheck("FindProducts", "shop", "pnp"),
            Trial(Turn(1, [SubToolCall("FindProducts", """{"shop":"pnp"}""")])));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Pass));
    }

    /// <summary>A value inside a longer text isn't the same text, so it doesn't match.</summary>
    [Test]
    public async Task AValueInsideALongerText_DoesNotMatch()
    {
        var grade = await GradeAsync(new ToolUsedWithCheck("FindProducts", "query", "hoender"),
            Trial(Turn(1, [ToolCall("FindProducts", """{"query":"hoender 1 kg"}""")])));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Fail));
    }

    /// <summary>
    /// Calls with other values fail, and the reason says the tool was called but never with the value.
    /// </summary>
    [Test]
    public async Task CallsWithOtherValues_Fail_SayingSo()
    {
        var grade = await GradeAsync(new ToolUsedWithCheck("FindProducts", "shop", "checkers"),
            Trial(Turn(1, [ToolCall("FindProducts", """{"shop":"pnp"}""")])));

        Assert.That((grade.Outcome, grade.Score), Is.EqualTo((GradeOutcome.Fail, 0.0)));
        Assert.That(grade.Reason, Does.Contain("never with").And.Contain("checkers"));
    }

    /// <summary>A tool never called fails, saying so.</summary>
    [Test]
    public async Task NeverCalled_Fails()
    {
        var grade = await GradeAsync(new ToolUsedWithCheck("FindProducts", "shop", "checkers"), Trial(Turn(1)));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Fail));
        Assert.That(grade.Reason, Does.Contain("never called"));
    }

    /// <summary>With no matching call found, a turn the infrastructure broke is Unknown.</summary>
    [Test]
    public async Task NoMatchFound_AndABrokenTurn_IsUnknown()
    {
        var grade = await GradeAsync(new ToolUsedWithCheck("FindProducts", "shop", "checkers"),
            Trial(Turn(1, failure: TurnFailure.Timeout)));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Unknown));
    }

    /// <summary>
    /// The entry builds the check from a file's settings, under its name and version, required by default.
    /// </summary>
    [Test]
    public void TheEntry_BuildsTheCheckFromItsSettings()
    {
        var built = Build("""{"tool": "FindProducts", "field": "shop", "value": "checkers"}""");

        Assert.That(built, Is.TypeOf<ToolUsedWithCheck>());
        Assert.That((built.Name, built.Version, built.Required), Is.EqualTo(("tool-used-with", 1, true)));
    }

    /// <summary>A blank or missing setting is refused, naming the check type and the field.</summary>
    [TestCase("""{"tool": "FindProducts", "field": "shop", "value": " "}""", "value")]
    [TestCase("""{"tool": "FindProducts", "value": "checkers"}""", "field")]
    public void ABlankOrMissingSetting_IsRefused(string settings, string field)
    {
        var refusal = Assert.Throws<InvalidDataException>(() => Build(settings));

        Assert.That(refusal!.Message, Does.Contain("tool-used-with").And.Contain(field));
    }

    /// <summary>Grades the trial with the check.</summary>
    private static async Task<Grade> GradeAsync(ICheck check, Trial trial) =>
        await check.GradeAsync(trial, CancellationToken.None);

    /// <summary>Builds the check the way an eval file does: from the general checks' catalogue, by its type.</summary>
    private static ICheck Build(string settings) =>
        new CheckCatalog(GeneralChecks.All).Get("tool-used-with")
            .Create(JsonDocument.Parse(settings).RootElement.Clone(), CheckContext.None);
}
