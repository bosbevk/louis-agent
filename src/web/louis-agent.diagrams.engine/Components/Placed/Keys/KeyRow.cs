using LouisAgent.Diagrams.Tones;

namespace LouisAgent.Diagrams.Engine.Components.Placed.Keys;

/// <summary>One row of a component diagram's key (F134.7): a small sample of a thing, and what it means.</summary>
/// <param name="Kind">What the row explains, which decides the sample drawn.</param>
/// <param name="Words">What it means.</param>
/// <param name="Tone">The tone a tone's row explains. Plain for every other row.</param>
public sealed record KeyRow(KeyRowKind Kind, string Words, DiagramTone Tone = DiagramTone.Plain);
