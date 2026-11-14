using LouisAgent.Diagrams.Engine.Geometry;
using LouisAgent.Diagrams.Tones;

namespace LouisAgent.Diagrams.Engine.Tickets.Placed;

/// <summary>The status pill at the head's top right: its box, its words and its tone.</summary>
/// <param name="Box">Where it stands and how large it is.</param>
/// <param name="Label">What it says.</param>
/// <param name="Tone">Its colour.</param>
public sealed record PlacedPill(IBox Box, string Label, DiagramTone Tone);
