using LouisAgent.Diagrams.Tones;

namespace LouisAgent.Diagrams.Composite.Spec;

/// <summary>
/// One port of a class in a composite structure diagram (F134.13): a way in or out on the class's edge, drawn as
/// a pin with its name.
/// </summary>
/// <param name="Name">Its name, written in its pin. No two ports of a class share one.</param>
/// <param name="Side">In, for what is asked of the class, or out, for what the class needs.</param>
/// <param name="Part">The part that does what is asked, or the part that needs what comes in.</param>
/// <param name="Label">What is asked or needed, in the code's words: a method or an interface.</param>
/// <param name="Tone">Its pin's colour. Plain wears the class's own.</param>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0040-records-for-values-classes-for-things.md">ADR 0040: records for values, classes for things</seealso>
public sealed record CompositePort(
    string Name,
    PortSide Side,
    string Part,
    string? Label = null,
    DiagramTone Tone = DiagramTone.Plain);
