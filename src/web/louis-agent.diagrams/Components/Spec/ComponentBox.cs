using LouisAgent.Diagrams.Tones;

namespace LouisAgent.Diagrams.Components.Spec;

/// <summary>
/// One component in a component diagram (F134.7): a box with its name, a few words of what kind it is and a line of
/// what it does, the pins on its edge, and the boxes it holds, drawn inside it.
/// </summary>
/// <param name="Id">Its id, by which a state or a neighbour names it.</param>
/// <param name="Name">Its name, as the code has it.</param>
/// <param name="Kind">The few words in its chip, such as "general part".</param>
/// <param name="Note">A line of what it does.</param>
/// <param name="Tone">Its colour, which means what it means in every picture: green is this project's own.</param>
/// <param name="In">What is handed down to it: its parameters, each a pin with an arrow down.</param>
/// <param name="Out">What it says back up: its callbacks, each a pin with an arrow up.</param>
/// <param name="Holds">The boxes inside it, top to bottom.</param>
/// <param name="State">The one thing it holds of its own, with the pins that carry it.</param>
/// <param name="Ghost">Drawn dashed: what stands in this place when the usual box does not.</param>
public sealed record ComponentBox(
    string Id,
    string Name,
    string? Kind = null,
    string? Note = null,
    DiagramTone Tone = DiagramTone.Plain,
    IReadOnlyList<string>? In = null,
    IReadOnlyList<string>? Out = null,
    IReadOnlyList<ComponentBox>? Holds = null,
    ComponentState? State = null,
    bool Ghost = false)
{
    /// <summary>This box and every box inside it, however deep, each before the boxes it holds.</summary>
    public IEnumerable<ComponentBox> Within() => (Holds ?? []).SelectMany(held => held.Within()).Prepend(this);
}
