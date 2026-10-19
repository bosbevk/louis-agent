using static LouisAgent.Api.Benchmarks.Store.BenchmarkDocuments;
using static LouisAgent.Api.Benchmarks.Runs.BenchmarkProtocol;

namespace LouisAgent.Api.Benchmarks.Store;

/// <summary>
/// Where benchmark runs are kept (F31): in memory for the tests and a store-less API, in MongoDB for Louis's runs
/// (step 5c). Writes are upserts by key, so the recorder can write a document again as it grows; reads come back in
/// the run's order. <c>BenchmarkStoreContract</c> holds every store to this behaviour.
/// </summary>
public interface IBenchmarkStore
{
    /// <summary>Writes the run, replacing what was kept under its id.</summary>
    Task SaveRunAsync(Run run, CancellationToken cancellationToken = default);

    Task<Run?> GetRunAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>The runs, newest first; only <paramref name="check"/>'s when given; at most <paramref name="limit"/>.</summary>
    Task<IReadOnlyList<Run>> ListRunsAsync(string? check = null, int limit = 50, CancellationToken cancellationToken = default);

    /// <summary>Writes a conversation of <paramref name="run"/>, replacing the one under its key.</summary>
    Task SaveConversationAsync(string run, Conversation conversation, CancellationToken cancellationToken = default);

    /// <summary>The run's conversations in their <see cref="Conversation.Order"/>.</summary>
    Task<IReadOnlyList<Conversation>> ConversationsAsync(string run, CancellationToken cancellationToken = default);

    /// <summary>Writes a turn and its stream events, replacing the ones under its key.</summary>
    Task SaveTurnAsync(string run, Turn turn, IReadOnlyList<StreamEvent> events, CancellationToken cancellationToken = default);

    /// <summary>A conversation's turns by number, without their events.</summary>
    Task<IReadOnlyList<Turn>> TurnsAsync(string run, ConversationKey conversation, CancellationToken cancellationToken = default);

    /// <summary>A turn's stream events in the order they came; empty for an unknown turn.</summary>
    Task<IReadOnlyList<StreamEvent>> TurnEventsAsync(string run, TurnKey turn, CancellationToken cancellationToken = default);

    /// <summary>Adds the ledger's requests for a run (step 4).</summary>
    Task AddRequestsAsync(string run, IReadOnlyList<Request> requests, CancellationToken cancellationToken = default);

    /// <summary>The run's requests by time.</summary>
    Task<IReadOnlyList<Request>> RequestsAsync(string run, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks every run still <see cref="BenchmarkRunStatus.Running"/> as failed, ended at <paramref name="now"/>: at
    /// start-up nothing is running, so such a run was cut off by a restart. Returns how many.
    /// </summary>
    Task<int> CloseOrphansAsync(DateTimeOffset now, CancellationToken cancellationToken = default);

    /// <summary>Deletes a run with its conversations, turns and requests; false when there was none.</summary>
    Task<bool> DeleteRunAsync(string id, CancellationToken cancellationToken = default);
}
