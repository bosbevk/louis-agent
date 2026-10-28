using static LouisAgent.Api.Evals.Store.EvalDocuments;

namespace LouisAgent.Api.Evals.Store.Runs;

/// <summary>
/// Where the runs themselves are kept: one record per run, saved again as it grows. Its conversations, turns and
/// requests are kept by their own stores.
/// </summary>
/// <remarks>
/// Repository, one per record (Fowler, PoEAA). The run store knows only runs, so its MongoDB class holds one
/// collection, one mapping and one index, and a test of runs needs nothing else.
/// <see cref="IEvalStore"/> puts the four record stores back together for the callers that want all of them.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0022-benchmarks-recorded-from-the-protocol.md">ADR 0022: benchmark results in MongoDB</seealso>
public interface IRunStore
{
    /// <summary>Saves the run, replacing whatever was kept under its id.</summary>
    Task SaveRunAsync(Run run, CancellationToken cancellationToken = default);

    /// <summary>Finds the run with this id, or nothing if there is none.</summary>
    Task<Run?> GetRunAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>Lists the newest runs first, up to a limit, and only one check's if you ask.</summary>
    Task<IReadOnlyList<Run>> ListRunsAsync(string? check = null, int limit = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks every run still 'running' as failed. At start-up nothing is running, so those were cut off by a restart.
    /// Tells you how many.
    /// </summary>
    Task<int> CloseOrphansAsync(DateTimeOffset now, CancellationToken cancellationToken = default);

    /// <summary>
    /// The ids of the runs another build wrote: under any other protocol version than this one, or with none at all.
    /// Only their ids, so a run of another shape never has to be read.
    /// </summary>
    Task<IReadOnlyList<string>> RunsBeforeAsync(int version, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the run's own record, nothing under it, and tells you how many went: one, or none. Deleting a whole run
    /// is <see cref="IEvalStore.DeleteRunAsync"/>.
    /// </summary>
    Task<int> DeleteAsync(string id, CancellationToken cancellationToken = default);
}
