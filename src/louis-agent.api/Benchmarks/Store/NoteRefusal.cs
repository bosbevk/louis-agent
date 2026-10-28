namespace LouisAgent.Api.Benchmarks.Store;

/// <summary>
/// Why a note was turned down: nothing but spaces, or too long.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
public enum NoteRefusal
{
    /// <summary>There was nothing in it but spaces.</summary>
    Blank,

    /// <summary>It was longer than a note may be.</summary>
    TooLong,
}
