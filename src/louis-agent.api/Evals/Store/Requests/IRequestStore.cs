using static LouisAgent.Api.Evals.Store.EvalDocuments;

namespace LouisAgent.Api.Evals.Store.Requests;

/// <summary>
/// Where a run's model requests are kept, as the ledger had them: added once the run is over, read back by time.
/// A token count or a price the ledger didn't know stays unknown, never 0.
/// </summary>
/// <remarks>
/// Repository, one per record (Fowler, PoEAA): its MongoDB class holds one collection, one mapping and one index, and
/// a test of requests needs nothing else. <see cref="IEvalStore"/> puts the record stores back together.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0007-measure-every-request.md">ADR 0007: measure every request</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0022-benchmarks-recorded-from-the-protocol.md">ADR 0022: benchmark results in MongoDB</seealso>
public interface IRequestStore
{
    /// <summary>Adds the model requests the ledger has for this run.</summary>
    Task AddRequestsAsync(string run, IReadOnlyList<Request> requests, CancellationToken cancellationToken = default);

    /// <summary>A run's model requests, by time.</summary>
    Task<IReadOnlyList<Request>> RequestsAsync(string run, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes every request of a run, and tells you whether there were any. Deleting a whole run is
    /// <see cref="IEvalStore.DeleteRunAsync"/>.
    /// </summary>
    Task<bool> DeleteRequestsAsync(string run, CancellationToken cancellationToken = default);
}
