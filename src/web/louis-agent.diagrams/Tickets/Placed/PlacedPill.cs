using LouisAgent.Diagrams.Tones;

namespace LouisAgent.Diagrams.Tickets.Placed;

/// <summary>The status pill at the band's top right: its box, its words and its tone.</summary>
public sealed record PlacedPill(double X, double Y, double W, double H, string Label, DiagramTone Tone);
