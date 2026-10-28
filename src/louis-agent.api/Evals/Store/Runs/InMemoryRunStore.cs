using LouisAgent.Api.Evals.Runs.Kinds;
using static LouisAgent.Api.Evals.Store.EvalDocuments;

namespace LouisAgent.Api.Evals.Store.Runs;

/// <summary>
/// The runs in memory: what the tests run on, and the reference the MongoDB one is held to. One lock is plenty,
/// because a handful of writes a second is all it ever sees.
/// </summary>
/// <remarks>
/// Repository, the in-memory side: the same runs as MongoDB behind the same interface, held to the same contract test.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0022-benchmarks-recorded-from-the-protocol.md">ADR 0022: benchmark results in MongoDB</seealso>
public sealed class InMemoryRunStore : IRunStore
{
    /// <summary>The one lock over the runs.</summary>
    private readonly Lock _lock = new();

    /// <summary>Runs by id.</summary>
    private readonly Dictionary<string, Run> _runs = [];

    /// <summary>Saves the run, replacing the one under its id.</summary>
    public Task SaveRunAsync(Run run, CancellationToken cancellationToken = default)
    {
        lock (_lock) _runs[run.Id] = run;
        return Task.CompletedTask;
    }

    /// <summary>Finds the run, or nothing if there is none.</summary>
    public Task<Run?> GetRunAsync(string id, CancellationToken cancellationToken = default)
    {
        lock (_lock) return Task.FromResult(_runs.GetValueOrDefault(id));
    }

    /// <summary>Lists the newest runs first, up to the limit, and only one check's if asked.</summary>
    public Task<IReadOnlyList<Run>> ListRunsAsync(string? check = null, int limit = 50,
        CancellationToken cancellationToken = default)
    {
        lock (_lock)
            return Task.FromResult<IReadOnlyList<Run>>(_runs.Values.Where(r => check is null || r.Check == check)
                .OrderByDescending(r => r.StartedAt).Take(limit).ToArray());
    }

    /// <summary>Marks every run still 'running' as failed and ended now, and says how many.</summary>
    public Task<int> CloseOrphansAsync(DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var orphans = _runs.Values.Where(r => r.Status == EvalRunStatus.Running).ToArray();
            foreach (var run in orphans)
                _runs[run.Id] = run with
                {
                    Status = EvalRunStatus.Failed,
                    EndedAt = now
                };
            return Task.FromResult(orphans.Length);
        }
    }

    /// <summary>Forgets the run, and says whether it was there.</summary>
    public Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        lock (_lock) return Task.FromResult(_runs.Remove(id));
    }
}
