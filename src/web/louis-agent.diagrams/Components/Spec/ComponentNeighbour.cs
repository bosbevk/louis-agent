using LouisAgent.Diagrams.Tones;

namespace LouisAgent.Diagrams.Components.Spec;

/// <summary>
/// What stands beside the root of a component diagram (F134.7): a page that hands it things, a service it reads,
/// or a script it calls.
/// </summary>
/// <param name="Id">Its id.</param>
/// <param name="Name">Its name, as the code has it.</param>
/// <param name="Side">Before the root, or after it.</param>
/// <param name="Kind">The few words in its chip.</param>
/// <param name="Note">A line of what it does.</param>
/// <param name="Tone">Its colour, as a box's.</param>
/// <param name="To">The box it stands by. The root, when it names none.</param>
/// <param name="Gives">What it hands to that box, each on a line of its own.</param>
/// <param name="Reads">Whether that box only reads it, which is drawn as one dashed line.</param>
/// <param name="Calls">The word on the line from the box to it.</param>
/// <param name="Back">The word on the line from it back to the box.</param>
public sealed record ComponentNeighbour(
    string Id,
    string Name,
    NeighbourSide Side,
    string? Kind = null,
    string? Note = null,
    DiagramTone Tone = DiagramTone.Plain,
    string? To = null,
    IReadOnlyList<string>? Gives = null,
    bool Reads = false,
    string? Calls = null,
    string? Back = null);
