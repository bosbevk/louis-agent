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
/// The <c>tool-not-used</c> check: no one in the trial, the agent or a specialist, called the tool; a broken turn with
/// no call found is Unknown, since the call might have been in it.
/// </summary>
public class ToolNotUsedCheckTests
{
    /// <summary>A trial that never calls the tool passes.</summary>
    [Test]
    public async Task NeverCalled_Passes()
    {
        var grade = await GradeAsync(new ToolNotUsedCheck("WebSearch"), Trial(Turn(1, [ToolCall("FetchUrl")])));

        Assert.That((grade.Outcome, grade.Score), Is.EqualTo((GradeOutcome.Pass, 1.0)));
    }

    /// <summary>The agent's call fails, naming the tool and the turn.</summary>
    [Test]
    public async Task TheAgentsCall_Fails_NamingTheTurn()
    {
        var grade = await GradeAsync(new ToolNotUsedCheck("WebSearch"),
            Trial(Turn(1), Turn(2, [ToolCall("WebSearch")])));

        Assert.That((grade.Outcome, grade.Score), Is.EqualTo((GradeOutcome.Fail, 0.0)));
        Assert.That(grade.Reason, Does.Contain("WebSearch").And.Contain("turn 2"));
    }

    /// <summary>A specialist's call fails too, and the reason says a specialist made it.</summary>
    [Test]
    public async Task ASpecialistsCall_Fails_SayingWhoMadeIt()
    {
        var grade = await GradeAsync(new ToolNotUsedCheck("WebSearch"), Trial(Turn(1, [SubToolCall("WebSearch")])));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Fail));
        Assert.That(grade.Reason, Does.Contain("specialist"));
    }

    /// <summary>
    /// With no call found, a turn the infrastructure broke is Unknown: the call might have been in it.
    /// </summary>
    [Test]
    public async Task NoCallFound_AndABrokenTurn_IsUnknown()
    {
        var grade = await GradeAsync(new ToolNotUsedCheck("WebSearch"), Trial(Turn(1, failure: TurnFailure.Timeout)));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Unknown));
    }

    /// <summary>
    /// The entry builds the check from a file's settings, under its name and version, required by default.
    /// </summary>
    [Test]
    public void TheEntry_BuildsTheCheckFromItsSettings()
    {
        var built = Build("""{"tool": "WebSearch"}""");

        Assert.That(built, Is.TypeOf<ToolNotUsedCheck>());
        Assert.That((built.Name, built.Version, built.Required), Is.EqualTo(("tool-not-used", 1, true)));
    }

    /// <summary>A blank or missing tool is refused, naming the check type and the field.</summary>
    [TestCase("""{"tool": " "}""")]
    [TestCase("{}")]
    public void NoTool_IsRefused(string settings)
    {
        var refusal = Assert.Throws<InvalidDataException>(() => Build(settings));

        Assert.That(refusal!.Message, Does.Contain("tool-not-used").And.Contain("tool"));
    }

    /// <summary>Grades the trial with the check.</summary>
    private static async Task<Grade> GradeAsync(ICheck check, Trial trial) =>
        await check.GradeAsync(trial, CancellationToken.None);

    /// <summary>Builds the check the way an eval file does: from the general checks' catalogue, by its type.</summary>
    private static ICheck Build(string settings) =>
        new CheckCatalog(GeneralChecks.All).Get("tool-not-used")
            .Create(JsonDocument.Parse(settings).RootElement.Clone(), CheckContext.None);
}
