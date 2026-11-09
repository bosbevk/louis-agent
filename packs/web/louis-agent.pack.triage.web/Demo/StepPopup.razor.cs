using LouisAgent.Packs.Triage.Web.Runs;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Packs.Triage.Web.Demo;

/// <summary>
/// The popup beside the agent at work (F106.3.4): who took this step, what it did, why in its own words, and what that
/// kind of step is, so a visitor who has never seen a triage follows it.
/// </summary>
public partial class StepPopup
{
    /// <summary>The step on screen.</summary>
    [Parameter, EditorRequired] public TriageStepView Step { get; set; } = null!;

    /// <summary>The site's words.</summary>
    [Parameter] public TriageWords Words { get; set; } = new();
}
