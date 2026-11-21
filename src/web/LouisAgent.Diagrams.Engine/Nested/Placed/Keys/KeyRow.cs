using LouisAgent.Diagrams.Tones;

namespace LouisAgent.Diagrams.Engine.Nested.Placed.Keys;

/// <summary>One row of a nested picture's key (F134.7): a small sample of a thing, and what it means.</summary>
/// <param name="Kind">What the row explains, which decides the sample drawn.</param>
/// <param name="Words">What it means.</param>
/// <param name="Tone">The tone a tone's row explains. Plain for every other row.</param>
public sealed record KeyRow(KeyRowKind Kind, string Words, DiagramTone Tone = DiagramTone.Plain);
