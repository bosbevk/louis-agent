using LouisAgent.Diagrams.Icons;
using LouisAgent.Diagrams.Tones;

namespace LouisAgent.Diagrams.Sequence.Spec;

/// <summary>
/// Someone in a sequence chart: a head at the top with a lifeline under it. It wears an icon and a tone the way a
/// flow's box does, so a colour means the same in every chart; an actor is a person rather than a part.
/// </summary>
public sealed record Participant(
    string Id,
    string Label,
    DiagramIcon? Icon = null,
    DiagramTone Tone = DiagramTone.Plain,
    bool Actor = false);
