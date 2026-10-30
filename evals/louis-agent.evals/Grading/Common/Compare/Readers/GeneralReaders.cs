using LouisAgent.Evals.Readers;

namespace LouisAgent.Evals.Grading.Common.Compare.Readers;

/// <summary>
/// The harness's own readers, the ones any eval file can name beside a key: the list the harness puts into the reader
/// catalogue.
/// </summary>
/// <remarks>
/// Registry (Fowler, PoEAA), as <c>GeneralChecks</c> is for checks: each owner lists its readers in one place, and the
/// runner gathers the lists.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0056-a-check-is-a-catalogue-entry.md">ADR 0056: a check is a catalogue entry</seealso>
public static class GeneralReaders
{
    /// <summary>Every general reader, by name.</summary>
    public static IReadOnlyList<IReader> All { get; } = [new CurrencyAmount(), new Digits()];
}
