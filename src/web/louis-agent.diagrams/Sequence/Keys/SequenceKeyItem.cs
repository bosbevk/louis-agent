using LouisAgent.Diagrams.Sequence.Spec;
using LouisAgent.Diagrams.Tones;

namespace LouisAgent.Diagrams.Sequence.Keys;

/// <summary>
/// One item of a sequence's key, placed (F134.10): a small sample of a thing, and what it means beside it.
/// </summary>
/// <param name="Kind">What the item explains, which decides the sample drawn.</param>
/// <param name="Words">What it means.</param>
/// <param name="X">Where its sample starts, from the key's left.</param>
/// <param name="Y">The top of its row, from the key's top.</param>
/// <param name="Sample">How wide its sample is. Its words start a little after it.</param>
/// <param name="Tone">The tone a tone's item explains. Plain for every other item.</param>
/// <param name="Block">The kind a block's item explains. None for every other item.</param>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0040-records-for-values-classes-for-things.md">ADR 0040: records for values, classes for things</seealso>
public sealed record SequenceKeyItem(
    SequenceKeyKind Kind,
    string Words,
    double X,
    double Y,
    double Sample,
    DiagramTone Tone = DiagramTone.Plain,
    BlockKind? Block = null);
