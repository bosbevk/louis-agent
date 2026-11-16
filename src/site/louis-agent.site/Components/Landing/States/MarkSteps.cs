using LouisAgent.Web.Components.Widget.Orb;

namespace LouisAgent.Site.Components.Landing.States;

/// <summary>
/// The states the landing's mark plays, in the order an answer goes through them (F151.6): it rests, it listens, it
/// thinks, it answers. Each line says what the framework's orb draws in that state, in a visitor's words.
/// </summary>
/// <remarks>
/// The list, its pace and what comes after a step are known here, so the component only draws them (ADR 0123).
/// </remarks>
public static class MarkSteps
{
    /// <summary>How long the mark stays in a state before it plays the next.</summary>
    public static readonly TimeSpan Pace = TimeSpan.FromSeconds(3.6);

    /// <summary>The four states, first to last.</summary>
    public static IReadOnlyList<MarkStep> All { get; } =
    [
        new(OrbState.Idle, "Resting", "It breathes, and waits for you."),
        new(OrbState.Listening, "Listening",
            "A ring in your own colour, while you type or pick something on the page."),
        new(OrbState.Thinking, "Thinking", "A light runs round it while it works out what to call."),
        new(OrbState.Speaking, "Answering", "It pulses while its words arrive.")
    ];

    /// <summary>The place of the step after this one, the first again after the last.</summary>
    public static int After(int at) => (at + 1) % All.Count;
}
