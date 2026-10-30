using LouisAgent.Evals.Grading.Common.Compare.Readers;
using LouisAgent.Evals.Readers;

namespace LouisAgent.Evals.Runner.Catalog;

/// <summary>
/// The reader catalogue the runner gathers, which an eval file's checks name a reader from: today the harness's
/// general readers. A name listed twice stops the runner's start.
/// </summary>
/// <remarks>
/// Registry (Fowler, PoEAA), beside <see cref="RunnerChecks"/>, where the lists meet for the same reason: the runner is
/// the one project that knows every evals project. An identity's own readers join through the composition when an
/// identity ships one (F49.2b step 6).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0056-a-check-is-a-catalogue-entry.md">ADR 0056: a check is a catalogue entry</seealso>
public static class RunnerReaders
{
    /// <summary>Every reader the runner's evals can name.</summary>
    public static ReaderCatalog Catalog { get; } = new(GeneralReaders.All);
}
