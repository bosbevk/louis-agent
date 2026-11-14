namespace LouisAgent.Diagrams.Classes.Keys;

/// <summary>
/// What one item of a class chart's key explains (F134.11): a tone a badge wears, a letter a badge shows, or a
/// kind of link.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
public enum ClassKeyKind
{
    Tone,
    Mark,
    Link
}
