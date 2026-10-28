using static LouisAgent.Api.Evals.Store.EvalDocuments;
using LouisAgent.Evals.Protocol.Parts.Conversations;

namespace LouisAgent.Api.Evals.Store.Turns;

/// <summary>
/// The turns in memory, each with its stream events: what the tests run on, and the reference the MongoDB one is held
/// to. One lock is plenty, because a handful of writes a second is all it ever sees.
/// </summary>
/// <remarks>
/// Repository, the in-memory side: the same turns as MongoDB behind the same interface, held to the same contract
/// test.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0022-benchmarks-recorded-from-the-protocol.md">ADR 0022: benchmark results in MongoDB</seealso>
public sealed class InMemoryTurnStore : ITurnStore
{
    /// <summary>The one lock over the turns.</summary>
    private readonly Lock _lock = new();

    /// <summary>Turns with their stream events, by run and key.</summary>
    private readonly Dictionary<(string Run, TurnKey Key), (Turn Turn, StreamEvent[] Events)> _turns = [];

    /// <summary>Saves a turn with its stream events, replacing the ones under its key.</summary>
    public Task SaveTurnAsync(string run, Turn turn, IReadOnlyList<StreamEvent> events,
        CancellationToken cancellationToken = default)
    {
        lock (_lock) _turns[(run, turn.Key)] = (turn, events.ToArray());
        return Task.CompletedTask;
    }

    /// <summary>A conversation's turns in order.</summary>
    public Task<IReadOnlyList<Turn>> TurnsAsync(string run, ConversationKey conversation,
        CancellationToken cancellationToken = default)
    {
        lock (_lock)
            return Task.FromResult<IReadOnlyList<Turn>>(_turns
                .Where(t => t.Key.Run == run && t.Key.Key.Conversation == conversation)
                .Select(t => t.Value.Turn).OrderBy(t => t.Key.Turn).ToArray());
    }

    /// <summary>A turn's stream events in the order they came; none for an unknown turn.</summary>
    public Task<IReadOnlyList<StreamEvent>> TurnEventsAsync(string run, TurnKey turn,
        CancellationToken cancellationToken = default)
    {
        lock (_lock)
            return Task.FromResult<IReadOnlyList<StreamEvent>>(_turns.TryGetValue((run, turn), out var kept)
                ? kept.Events
                : []);
    }

    /// <summary>Forgets the run's turns and their events, and says how many turns went.</summary>
    public Task<int> DeleteTurnsAsync(string run, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var keys = _turns.Keys.Where(k => k.Run == run).ToArray();
            foreach (var key in keys) _turns.Remove(key);
            return Task.FromResult(keys.Length);
        }
    }
}
