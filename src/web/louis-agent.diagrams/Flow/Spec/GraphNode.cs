using LouisAgent.Diagrams.Icons;
using LouisAgent.Diagrams.Tones;

namespace LouisAgent.Diagrams.Flow.Spec;

/// <summary>
/// One box of a diagram: its id (what an edge names), the words on it, an optional line under them, and the column it
/// stands in, counted from the left; left out, the arrows say it (<see cref="Ranks"/>). It may stand in a group, and
/// wear an icon and a tone (CL60.2); without them it is the quiet grey box it always was. A class chart's type lists
/// its members under the label (CL60.4).
/// </summary>
public sealed record GraphNode(
    string Id,
    string Label,
    string? Note = null,
    int? Column = null,
    string? Group = null,
    DiagramIcon? Icon = null,
    DiagramTone Tone = DiagramTone.Plain,
    IReadOnlyList<string>? Members = null);
