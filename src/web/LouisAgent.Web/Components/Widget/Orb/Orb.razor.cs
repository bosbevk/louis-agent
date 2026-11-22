using LouisAgent.Choices;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Widget.Orb;

/// <summary>
/// The agent's orb (F134.3): a disc beside the agent's box that shows what it is doing, with the agent's mark on it.
/// Idle it breathes, listening it wears a ring in the visitor's colour, thinking an arc goes round, showing rings go
/// out from it, speaking it pulses.
/// </summary>
/// <remarks>
/// A general part of the framework's web (F134's rule 1): it knows nothing of a chat, only its state, which a caller
/// reads with <see cref="OrbStates"/>. It is named for what it is, so a later project can use it alone.
/// </remarks>
public partial class Orb
{
    /// <summary>What the agent is doing.</summary>
    [Parameter]
    public OrbState State { get; set; }

    /// <summary>Who the orb is, for a screen reader, which hears the state after it.</summary>
    [Parameter, EditorRequired]
    public string Label { get; set; } = "";

    /// <summary>The agent's mark on the disc, such as its initial; with none, the disc is bare.</summary>
    [Parameter]
    public string? Mark { get; set; }

    /// <summary>
    /// The mark an agent wears on its orb: the first letter of its name, as the design drew it, so the bar and the
    /// panel's head put the same letter on it.
    /// </summary>
    public static string? InitialOf(string name) => name.Length > 0 ? name[..1] : null;

    /// <summary>The orb's classes: itself, and its state.</summary>
    private string Classes => $"orb is-{State.Wire()}";

    /// <summary>What a screen reader hears: who it is, and what it is doing.</summary>
    private string AriaLabel => $"{Label}: {State.Wire()}";
}
