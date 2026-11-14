using LouisAgent.Diagrams.Tones;

namespace LouisAgent.Diagrams.Composite.Spec;

/// <summary>
/// A composite structure diagram as its JSON says it (F134.13): one class, the parts it is built from, the ports
/// on its edge, and which parts work together.
/// </summary>
/// <param name="Name">The class the picture is of.</param>
/// <param name="Parts">Its parts.</param>
/// <param name="Ports">Its ports: what is asked of it, and what it needs.</param>
/// <param name="Connectors">Which of its parts work together.</param>
/// <param name="Kind">The few words in the chip beside its name, such as "class".</param>
/// <param name="Aside">A few words at the right of its name, such as where it is.</param>
/// <param name="Tone">Its colour, which means what it means in every picture: green is this project's own.</param>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0040-records-for-values-classes-for-things.md">ADR 0040: records for values, classes for things</seealso>
public sealed record CompositeSpec(
    string Name,
    IReadOnlyList<CompositePart> Parts,
    IReadOnlyList<CompositePort>? Ports = null,
    IReadOnlyList<CompositeConnector>? Connectors = null,
    string? Kind = null,
    string? Aside = null,
    DiagramTone Tone = DiagramTone.Plain);
