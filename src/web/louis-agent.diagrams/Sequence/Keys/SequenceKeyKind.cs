namespace LouisAgent.Diagrams.Sequence.Keys;

/// <summary>
/// What one item of a sequence's key explains (F134.10): a tone a badge wears, a call, an answer, a note, or a kind
/// of block.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
public enum SequenceKeyKind
{
    Tone,
    Call,
    Answer,
    Note,
    Block
}
