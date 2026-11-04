using static LouisAgent.Api.Evals.Store.EvalDocuments;
using LouisAgent.Evals.Protocol.Parts.Conversations;

namespace LouisAgent.Api.Evals.Store.Turns;

/// <summary>
/// Where a conversation's turns are kept, each with the stream events it gave, saved again as the turn streams. The
/// events are heavy and only a replay reads them, so they come back on their own.
/// </summary>
/// <remarks>
/// Repository, one per record (Fowler, PoEAA): its MongoDB class holds one collection, one mapping and one index, and
/// a test of turns needs nothing else. <see cref="IEvalStore"/> puts the record stores back together.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0022-benchmarks-recorded-from-the-protocol.md">ADR 0022: benchmark results in MongoDB</seealso>
public interface ITurnStore
{
    /// <summary>Saves a turn together with its stream events, replacing the ones under its key.</summary>
    Task SaveTurnAsync(string run, Turn turn, IReadOnlyList<StreamEvent> events,
        CancellationToken cancellationToken = default);

    /// <summary>A conversation's turns in order, without their stream events, which are heavy.</summary>
    Task<IReadOnlyList<Turn>> TurnsAsync(string run, ConversationKey conversation,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// One turn's stream events in the order they came, for replaying it. Nothing for a turn we don't know.
    /// </summary>
    Task<IReadOnlyList<StreamEvent>> TurnEventsAsync(string run, TurnKey turn,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes every turn of a run, events and all, and tells you how many turns went. Deleting a whole run is
    /// <see cref="IEvalStore.DeleteRunAsync"/>.
    /// </summary>
    Task<int> DeleteTurnsAsync(string run, CancellationToken cancellationToken = default);
}
