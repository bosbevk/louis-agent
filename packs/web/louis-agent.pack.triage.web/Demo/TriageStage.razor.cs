using LouisAgent.Packs.Triage.Web.Runs;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Packs.Triage.Web.Demo;

/// <summary>
/// The triage team as swimlanes (F122, Louis's pick of three, "lanes with steps"): a lane per agent in the order a
/// case meets them, the one at work lit, and the steps played so far as numbered dots in their agents' lanes, joined
/// in order, so a visitor sees who handed the case to whom; a dot shows its step.
/// </summary>
/// <remarks>
/// A swimlane diagram, as BPMN draws a process across the people who take part: the lanes say who, the line says the
/// order. Where each dot goes is <see cref="SwimlaneLayout"/>'s, plain code with its own tests, so this holds only
/// the drawing.
/// </remarks>
public partial class TriageStage
{
    /// <summary>The agent at work, by its name in the run file.</summary>
    [Parameter] public string? Lit { get; set; }

    /// <summary>The agents of the steps played so far, in order.</summary>
    [Parameter] public IReadOnlyList<string> Played { get; set; } = [];

    /// <summary>How many steps the whole run has, so the picture is as wide as the run from the start.</summary>
    [Parameter] public int Total { get; set; }

    /// <summary>The step on screen, from 0.</summary>
    [Parameter] public int Current { get; set; } = -1;

    /// <summary>A visitor picked a step's dot.</summary>
    [Parameter] public EventCallback<int> OnPick { get; set; }

    /// <summary>The site's words.</summary>
    [Parameter] public TriageWords Words { get; set; } = new();

    /// <summary>The dots of the steps played so far.</summary>
    private IReadOnlyList<LaneDot> Dots => SwimlaneLayout.Dots(Played);

    /// <summary>The picture's width, the whole run's.</summary>
    private int Width => SwimlaneLayout.Width(Math.Max(Total, Played.Count));

    /// <summary>What a screen reader hears: who is at work.</summary>
    private string AriaLabel => Lit is null ? "" : AgentWords.Of(Lit, Words);
}
