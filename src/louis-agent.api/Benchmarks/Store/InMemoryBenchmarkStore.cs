using LouisAgent.Api.Benchmarks.Runs;
using static LouisAgent.Api.Benchmarks.Store.BenchmarkDocuments;
using static LouisAgent.Api.Benchmarks.Runs.BenchmarkProtocol;

namespace LouisAgent.Api.Benchmarks.Store;

/// <summary>
/// A benchmark store in memory (F31): the contract's reference, and what the tests run against. One lock over
/// everything: the recorder writes one run while the portal reads, a handful of documents a second at most.
/// </summary>
public sealed class InMemoryBenchmarkStore : IBenchmarkStore
{
    private readonly Lock _lock = new();
    private readonly Dictionary<string, Run> _runs = [];
    private readonly Dictionary<(string Run, ConversationKey Key), Conversation> _conversations = [];
    private readonly Dictionary<(string Run, TurnKey Key), (Turn Turn, StreamEvent[] Events)> _turns = [];
    private readonly List<Request> _requests = [];

    public Task SaveRunAsync(Run run, CancellationToken cancellationToken = default)
    {
        lock (_lock) _runs[run.Id] = run;
        return Task.CompletedTask;
    }

    public Task<Run?> GetRunAsync(string id, CancellationToken cancellationToken = default)
    {
        lock (_lock) return Task.FromResult(_runs.GetValueOrDefault(id));
    }

    public Task<IReadOnlyList<Run>> ListRunsAsync(string? check = null, int limit = 50, CancellationToken cancellationToken = default)
    {
        lock (_lock)
            return Task.FromResult<IReadOnlyList<Run>>(_runs.Values.Where(r => check is null || r.Check == check)
                .OrderByDescending(r => r.StartedAt).Take(limit).ToArray());
    }

    public Task SaveConversationAsync(string run, Conversation conversation, CancellationToken cancellationToken = default)
    {
        lock (_lock) _conversations[(run, conversation.Key)] = conversation;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Conversation>> ConversationsAsync(string run, CancellationToken cancellationToken = default)
    {
        lock (_lock)
            return Task.FromResult<IReadOnlyList<Conversation>>(_conversations.Where(c => c.Key.Run == run).Select(c => c.Value)
                .OrderBy(c => c.Order).ToArray());
    }

    public Task SaveTurnAsync(string run, Turn turn, IReadOnlyList<StreamEvent> events, CancellationToken cancellationToken = default)
    {
        lock (_lock) _turns[(run, turn.Key)] = (turn, events.ToArray());
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Turn>> TurnsAsync(string run, ConversationKey conversation, CancellationToken cancellationToken = default)
    {
        lock (_lock)
            return Task.FromResult<IReadOnlyList<Turn>>(_turns.Where(t => t.Key.Run == run && t.Key.Key.Conversation == conversation)
                .Select(t => t.Value.Turn).OrderBy(t => t.Key.Turn).ToArray());
    }

    public Task<IReadOnlyList<StreamEvent>> TurnEventsAsync(string run, TurnKey turn, CancellationToken cancellationToken = default)
    {
        lock (_lock)
            return Task.FromResult<IReadOnlyList<StreamEvent>>(_turns.TryGetValue((run, turn), out var kept) ? kept.Events : []);
    }

    public Task AddRequestsAsync(string run, IReadOnlyList<Request> requests, CancellationToken cancellationToken = default)
    {
        lock (_lock) _requests.AddRange(requests.Select(r => r with { Run = run }));
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Request>> RequestsAsync(string run, CancellationToken cancellationToken = default)
    {
        lock (_lock)
            return Task.FromResult<IReadOnlyList<Request>>(_requests.Where(r => r.Run == run).OrderBy(r => r.At).ToArray());
    }

    public Task<int> CloseOrphansAsync(DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var orphans = _runs.Values.Where(r => r.Status == BenchmarkRunStatus.Running).ToArray();
            foreach (var run in orphans) _runs[run.Id] = run with { Status = BenchmarkRunStatus.Failed, EndedAt = now };
            return Task.FromResult(orphans.Length);
        }
    }

    public Task<bool> DeleteRunAsync(string id, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            bool had = _runs.Remove(id);
            foreach (var key in _conversations.Keys.Where(k => k.Run == id).ToArray()) had |= _conversations.Remove(key);
            foreach (var key in _turns.Keys.Where(k => k.Run == id).ToArray()) had |= _turns.Remove(key);
            had |= _requests.RemoveAll(r => r.Run == id) > 0;
            return Task.FromResult(had);
        }
    }
}
