using LouisAgent.Core.Agent.Turns;
using Microsoft.Extensions.AI;
using ToolTextAttribute = System.ComponentModel.DescriptionAttribute;

namespace LouisAgent.Core.Tests.Agent.Turns;

/// <summary>
/// A tool call that leaves out an argument the tool needs is answered with that parameter's own description, its
/// choices included (FX108, Louis's pick): the model called ReportInvestigation three times without `area` and only
/// ever heard "the arguments dictionary is missing a value", so it never knew what to send.
/// </summary>
public class MissingArgumentsTests
{
    /// <summary>A tool shaped like the investigator's: two needed choices and one optional note.</summary>
    [ToolText("Ends an investigation.")]
    private static string Report(
        [ToolText("The run id you were given.")] string runId,
        [ToolText("Whose area it is: code, data, hosting, third-party or unknown.")] string area,
        [ToolText("Anything else worth saying.")] string? note = null) => $"{runId} {area} {note}";

    private static AIFunction Tool => AIFunctionFactory.Create(Report, "ReportInvestigation");

    /// <summary>The missing argument is named with its description, so the model knows what to send.</summary>
    [Test]
    public void AMissingArgument_IsNamedWithItsDescription()
    {
        string? said = MissingArguments.Message(Tool, new AIFunctionArguments { ["runId"] = "r1" });

        Assert.That(said, Does.Contain("ReportInvestigation").And.Contain("area")
            .And.Contain("code, data, hosting, third-party or unknown"));
    }

    /// <summary>A call with every needed argument passes, and an optional one left out is not asked for.</summary>
    [Test]
    public void ACompleteCall_Passes()
    {
        var arguments = new AIFunctionArguments { ["runId"] = "r1", ["area"] = "code" };

        Assert.That(MissingArguments.Message(Tool, arguments), Is.Null);
    }

    /// <summary>Two missing arguments are both named, in the tool's order.</summary>
    [Test]
    public void EveryMissingArgument_IsNamed()
    {
        string? said = MissingArguments.Message(Tool, new AIFunctionArguments());

        Assert.That(said, Does.Contain("runId").And.Contain("area"));
        Assert.That(said!.IndexOf("runId", StringComparison.Ordinal),
            Is.LessThan(said.IndexOf("area:", StringComparison.Ordinal)));
    }
}
