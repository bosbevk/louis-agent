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
/// The <c>tool-used</c> check: a tool with this name was called somewhere in the trial, by the agent or by a
/// specialist it handed to.
/// </summary>
public class ToolUsedCheckTests
{
    /// <summary>The agent's own call in any turn passes, and the reason names the tool and the turn.</summary>
    [Test]
    public async Task TheAgentsCall_Passes()
    {
        var grade = await GradeAsync(new ToolUsedCheck("WebSearch"),
            Trial(Turn(1, [Text("Hallo")]), Turn(2, [ToolCall("WebSearch")])));

        Assert.That((grade.Outcome, grade.Score), Is.EqualTo((GradeOutcome.Pass, 1.0)));
        Assert.That(grade.Reason, Does.Contain("WebSearch").And.Contain("turn 2"));
    }

    /// <summary>A specialist's call counts too, and the reason says a specialist made it.</summary>
    [Test]
    public async Task ASpecialistsCall_Passes()
    {
        var grade = await GradeAsync(new ToolUsedCheck("SearchShops"), Trial(Turn(1, [SubToolCall("SearchShops")])));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Pass));
        Assert.That(grade.Reason, Does.Contain("specialist"));
    }

    /// <summary>Only the exact name counts: another tool's call fails, naming the tool that wasn't called.</summary>
    [Test]
    public async Task NoCall_Fails()
    {
        var grade = await GradeAsync(new ToolUsedCheck("WebSearch"),
            Trial(Turn(1, [ToolCall("WebFetch"), Text("Hallo")])));

        Assert.That((grade.Outcome, grade.Score), Is.EqualTo((GradeOutcome.Fail, 0.0)));
        Assert.That(grade.Reason, Does.Contain("WebSearch"));
    }

    /// <summary>With no call found and a turn the infrastructure broke, it can't tell, so it is Unknown.</summary>
    [Test]
    public async Task NoCall_AndAnInfrastructureFailure_IsUnknown()
    {
        var grade = await GradeAsync(new ToolUsedCheck("WebSearch"),
            Trial(Turn(1, [Text("Hallo")]), Turn(2, failure: TurnFailure.Timeout)));

        Assert.That((grade.Outcome, grade.Score), Is.EqualTo((GradeOutcome.Unknown, 0.0)));
    }

    /// <summary>A call already made is a call: a later broken turn doesn't take it back.</summary>
    [Test]
    public async Task ACall_BeforeAnInfrastructureFailure_Passes()
    {
        var grade = await GradeAsync(new ToolUsedCheck("WebSearch"),
            Trial(Turn(1, [ToolCall("WebSearch")]), Turn(2, failure: TurnFailure.Timeout)));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Pass));
    }

    /// <summary>A streamed error is no-error's to report: the call before it still counts.</summary>
    [Test]
    public async Task ACall_BeforeAStreamedError_Passes()
    {
        var grade = await GradeAsync(new ToolUsedCheck("WebSearch"), Trial(Turn(1, [ToolCall("WebSearch"), Error()])));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Pass));
    }

    /// <summary>
    /// The entry builds the check from a file's settings, under its name and version, required by default.
    /// </summary>
    [Test]
    public void TheEntry_BuildsTheCheckFromItsSettings()
    {
        var built = Build("""{"tool": "WebSearch"}""");

        Assert.That(built, Is.TypeOf<ToolUsedCheck>());
        Assert.That((built.Name, built.Version, built.Required), Is.EqualTo(("tool-used", 1, true)));
    }

    /// <summary>A missing tool name is refused, naming the check type and the field.</summary>
    [Test]
    public void NoTool_IsRefused()
    {
        var refusal = Assert.Throws<InvalidDataException>(() => Build("{}"));

        Assert.That(refusal!.Message, Does.Contain("tool-used").And.Contain("tool"));
    }

    /// <summary>Grades the trial with the check.</summary>
    private static async Task<Grade> GradeAsync(ICheck check, Trial trial) =>
        await check.GradeAsync(trial, CancellationToken.None);

    /// <summary>Builds the check the way an eval file will: from the general checks' catalogue, by its type.</summary>
    private static ICheck Build(string settings) =>
        new CheckCatalog(GeneralChecks.All).Get("tool-used").Create(JsonDocument.Parse(settings).RootElement.Clone());
}
