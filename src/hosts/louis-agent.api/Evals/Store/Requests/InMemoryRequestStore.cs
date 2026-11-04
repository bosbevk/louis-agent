using static LouisAgent.Api.Evals.Store.EvalDocuments;

namespace LouisAgent.Api.Evals.Store.Requests;

/// <summary>
/// The model requests in memory: what the tests run on, and the reference the MongoDB one is held to. One lock is
/// plenty, because requests are added once per run.
/// </summary>
/// <remarks>
/// Repository, the in-memory side: the same requests as MongoDB behind the same interface, held to the same contract
/// test.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0022-benchmarks-recorded-from-the-protocol.md">ADR 0022: benchmark results in MongoDB</seealso>
public sealed class InMemoryRequestStore : IRequestStore
{
    /// <summary>The one lock over the requests.</summary>
    private readonly Lock _lock = new();

    /// <summary>Every run's requests, in one list.</summary>
    private readonly List<Request> _requests = [];

    /// <summary>Adds requests to the list, tagged with the run they belong to.</summary>
    public Task AddRequestsAsync(string run, IReadOnlyList<Request> requests,
        CancellationToken cancellationToken = default)
    {
        lock (_lock) _requests.AddRange(requests.Select(r => r with { Run = run }));
        return Task.CompletedTask;
    }

    /// <summary>The run's requests, by time.</summary>
    public Task<IReadOnlyList<Request>> RequestsAsync(string run, CancellationToken cancellationToken = default)
    {
        lock (_lock)
            return Task.FromResult<IReadOnlyList<Request>>(_requests.Where(r => r.Run == run).OrderBy(r => r.At)
                .ToArray());
    }

    /// <summary>Forgets the run's requests, and says how many went.</summary>
    public Task<int> DeleteRequestsAsync(string run, CancellationToken cancellationToken = default)
    {
        lock (_lock) return Task.FromResult(_requests.RemoveAll(r => r.Run == run));
    }
}
