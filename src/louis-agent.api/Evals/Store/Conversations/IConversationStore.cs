using static LouisAgent.Api.Evals.Store.EvalDocuments;

namespace LouisAgent.Api.Evals.Store.Conversations;

/// <summary>
/// Where a run's conversations are kept: one record per conversation, with its scores and each grader's verdict,
/// saved again as the run goes.
/// </summary>
/// <remarks>
/// Repository, one per record (Fowler, PoEAA): its MongoDB class holds one collection, one mapping and one index, and
/// a test of conversations needs nothing else. <see cref="IEvalStore"/> puts the record stores back together.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0022-benchmarks-recorded-from-the-protocol.md">ADR 0022: benchmark results in MongoDB</seealso>
public interface IConversationStore
{
    /// <summary>Saves one conversation of a run, replacing the one under its key.</summary>
    Task SaveConversationAsync(string run, Conversation conversation, CancellationToken cancellationToken = default);

    /// <summary>A run's conversations, in the order they started.</summary>
    Task<IReadOnlyList<Conversation>> ConversationsAsync(string run, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes every conversation of a run, and tells you how many went. Deleting a whole run is
    /// <see cref="IEvalStore.DeleteRunAsync"/>.
    /// </summary>
    Task<int> DeleteConversationsAsync(string run, CancellationToken cancellationToken = default);
}
