using LouisAgent.Evals.Grading;

namespace LouisAgent.Evals.Catalog;

/// <summary>
/// What an entry may need beyond its settings: the eval's folder, where a file it names (a judge's rubric) is
/// read from, and the checks the eval built before it, which a review of a judge looks its judge up in.
/// </summary>
/// <remarks>
/// A Parameter Object (Fowler, <i>Refactoring</i>): one record in place of the separate arguments each entry would
/// otherwise need, so a new need is a field here, not a change to every entry. Most entries ignore it.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0056-a-check-is-a-catalogue-entry.md">ADR 0056: a check is a catalogue entry</seealso>
public sealed record CheckContext(string Folder, IReadOnlyList<ICheck> Before)
{
    /// <summary>For a check built outside an eval's folder, with nothing before it: C#, or a test.</summary>
    public static CheckContext None { get; } = new(string.Empty, []);
}
