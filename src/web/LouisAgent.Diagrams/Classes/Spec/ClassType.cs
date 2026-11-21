using LouisAgent.Diagrams.Icons;
using LouisAgent.Diagrams.Tones;

namespace LouisAgent.Diagrams.Classes.Spec;

/// <summary>
/// A type in a class chart: its name, what kind of type it is (an interface, a record), the members worth showing,
/// and the icon, tone and group a flow's box would wear.
/// </summary>
public sealed record ClassType(
    string Id,
    string Name,
    string? Kind = null,
    IReadOnlyList<string>? Members = null,
    DiagramIcon? Icon = null,
    DiagramTone Tone = DiagramTone.Plain,
    string? Group = null);
