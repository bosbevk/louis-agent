using LouisAgent.Packs.Triage.Web.Runs;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Packs.Triage.Web.Demo;

/// <summary>
/// The triage team as a stage (F106.3.4): the workflow and the agents in the order a case meets them, the manager in
/// the middle as the hub, and the one whose step is on screen lit.
/// </summary>
public partial class TriageStage
{
    /// <summary>The agent at work, by its name in the run file.</summary>
    [Parameter] public string? Lit { get; set; }

    /// <summary>The site's words.</summary>
    [Parameter] public TriageWords Words { get; set; } = new();

    /// <summary>What a screen reader hears: who is at work.</summary>
    private string AriaLabel => Lit is null ? "" : AgentWords.Of(Lit, Words);
}
