using LouisAgent.Composition;
using LouisAgent.Evals.Grading.Common.Compare.Readers;
using LouisAgent.Evals.Readers;

namespace LouisAgent.Evals.Runner.Catalog;

/// <summary>
/// The reader catalogue the runner gathers, which an eval file's checks name a reader from: the harness's general
/// readers and every identity's own. A name listed twice stops the runner's start.
/// </summary>
/// <remarks>
/// Registry (Fowler, PoEAA), beside <see cref="RunnerChecks"/>, where the lists meet for the same reason: the runner is
/// the one project that knows every evals project, and an identity's readers reach it through the composition's
/// <see cref="EvalReaders"/>.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0056-a-check-is-a-catalogue-entry.md">ADR 0056: a check is a catalogue entry</seealso>
public static class RunnerReaders
{
    /// <summary>Every reader the runner's evals can name.</summary>
    public static ReaderCatalog Catalog { get; } = new([.. GeneralReaders.All, .. EvalReaders.All]);
}
