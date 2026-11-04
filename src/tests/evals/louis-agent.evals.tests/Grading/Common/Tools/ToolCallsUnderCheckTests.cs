using System.Text.Json;
using LouisAgent.Evals.Catalog;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Grading.Common;
using LouisAgent.Evals.Grading.Common.Tools;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Trials;
using LouisAgent.Testing;
using static LouisAgent.Evals.Tests.Trials.TrialBuilder;

namespace LouisAgent.Evals.Tests.Grading.Common.Tools;

/// <summary>
/// The <c>tool-calls-under</c> check: every tool call in the trial, the agent's and its specialists', hand-overs
/// included, stays under the limit.
/// </summary>
public class ToolCallsUnderCheckTests
{
    /// <summary>
    /// The agent's calls, its hand-over and its specialist's calls all count, and the reason says the total and the
    /// limit.
    /// </summary>
    [Test]
    public async Task EveryCall_Counts_AndTheReasonSaysTheTotal()
    {
        var grade = await GradeAsync(new ToolCallsUnderCheck(5),
            Trial(Turn(1, [ToolCall("WebSearch"), Call(SampleRole.Alpha), SubToolCall("FindProducts")]),
                Turn(2, [ToolCall("FetchUrl")])));

        Assert.That((grade.Outcome, grade.Score), Is.EqualTo((GradeOutcome.Pass, 1.0)));
        Assert.That(grade.Reason, Does.Contain("4").And.Contain("5"));
    }

    /// <summary>As many calls as the limit aren't under it, so they fail.</summary>
    [Test]
    public async Task AsManyCallsAsTheLimit_Fail()
    {
        var grade = await GradeAsync(new ToolCallsUnderCheck(2),
            Trial(Turn(1, [ToolCall("WebSearch"), SubToolCall("FindProducts")])));

        Assert.That((grade.Outcome, grade.Score), Is.EqualTo((GradeOutcome.Fail, 0.0)));
    }

    /// <summary>A turn the infrastructure broke can't be judged, so it is Unknown, never a pass.</summary>
    [Test]
    public async Task AnInfrastructureFailure_IsUnknown()
    {
        var grade = await GradeAsync(new ToolCallsUnderCheck(5), Trial(Turn(1, failure: TurnFailure.Timeout)));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Unknown));
    }

    /// <summary>
    /// The entry builds the check from a file's settings, under its name and version, required by default.
    /// </summary>
    [Test]
    public void TheEntry_BuildsTheCheckFromItsSettings()
    {
        var built = Build("""{"count": 10}""");

        Assert.That(built, Is.TypeOf<ToolCallsUnderCheck>());
        Assert.That((built.Name, built.Version, built.Required), Is.EqualTo(("tool-calls-under", 1, true)));
    }

    /// <summary>
    /// A limit under one, or none, is refused, naming the check type and the field: no trial could pass.
    /// </summary>
    [TestCase("""{"count": 0}""")]
    [TestCase("{}")]
    public void ALimitUnderOne_IsRefused(string settings)
    {
        var refusal = Assert.Throws<InvalidDataException>(() => Build(settings));

        Assert.That(refusal!.Message, Does.Contain("tool-calls-under").And.Contain("count"));
    }

    /// <summary>Grades the trial with the check.</summary>
    private static async Task<Grade> GradeAsync(ICheck check, Trial trial) =>
        await check.GradeAsync(trial, CancellationToken.None);

    /// <summary>Builds the check the way an eval file does: from the general checks' catalogue, by its type.</summary>
    private static ICheck Build(string settings) =>
        new CheckCatalog(GeneralChecks.All).Get("tool-calls-under")
            .Create(JsonDocument.Parse(settings).RootElement.Clone(), CheckContext.None);
}
