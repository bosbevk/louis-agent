using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Readers;

namespace LouisAgent.Evals.Catalog;

/// <summary>
/// What an entry may need beyond its settings: the eval's folder, where a file it names (a judge's rubric) is
/// read from, the checks the eval built before it, which a review of a judge looks its judge up in, and the readers a
/// check that compares a value names one from.
/// </summary>
/// <remarks>
/// A Parameter Object (Fowler, <i>Refactoring</i>): one record in place of the separate arguments each entry would
/// otherwise need, so a new need is a field here, not a change to every entry. Most entries ignore it.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0056-a-check-is-a-catalogue-entry.md">ADR 0056: a check is a catalogue entry</seealso>
public sealed record CheckContext(string Folder, IReadOnlyList<ICheck> Before, ReaderCatalog Readers)
{
    /// <summary>
    /// For a check built outside an eval's folder, with nothing before it and no readers: C#, or a test. The general
    /// readers are the harness's, which this contract can't see, so a test of a check that names a reader builds its
    /// own context.
    /// </summary>
    public static CheckContext None { get; } = new(string.Empty, [], ReaderCatalog.Empty);
}
