using LouisAgent.Web.Components.Site.Turns;

namespace LouisAgent.Site.Pages;

/// <summary>
/// The landing: the offer, what the agent does, where it runs, custom agents, the demos and the proof.
/// </summary>
public partial class HomePage
{
    /// <summary>The coding agent's turn the custom agents' band plays, line by line (ADR 0125).</summary>
    private static readonly IReadOnlyList<TurnLine> CustomTurn =
    [
        new(TurnLineKind.Prompt, "› fix the failing test in UsageReportTests", 0),
        new(TurnLineKind.Think, "thinking · the test expects null for an unpriced request, not 0…", 1.2),
        new(TurnLineKind.Tool, "ReadWorkspaceFile", 2.6, "tests/UsageReportTests.cs"),
        new(TurnLineKind.Guard, "guard · 1,912 chars, within budget", 3.4),
        new(TurnLineKind.Tool, "ReadWorkspaceFile", 4.4, "louis-agent.core/Usage/Reports/UsageReport.cs"),
        new(TurnLineKind.Tool, "DotNetTest", 5.6, "--filter UsageReportTests"),
        new(TurnLineKind.Guard, "guard · 61,340 chars → summarised to 1,100", 6.8),
        new(TurnLineKind.Prompt, "passed 12 · failed 0", 7.9),
        new(TurnLineKind.Answer, "The total now keeps an unpriced request as null instead of adding 0. The test passes. Pushing stays with you.", 9)
    ];
}
