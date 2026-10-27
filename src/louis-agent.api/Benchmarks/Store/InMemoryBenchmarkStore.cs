using LouisAgent.Api.Benchmarks.Runs;
using static LouisAgent.Api.Benchmarks.Store.BenchmarkDocuments;
using static LouisAgent.Api.Benchmarks.Runs.BenchmarkProtocol;

namespace LouisAgent.Api.Benchmarks.Store;

/// <summary>
/// The store in memory: what the tests run on, and the reference the Mongo store is held to. One lock covers everything, because a handful of writes a second is all it ever sees.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0022-benchmarks-recorded-from-the-protocol.md">ADR 0022: benchmark results in MongoDB</seealso>
public sealed class InMemoryBenchmarkStore : IBenchmarkStore
{
    /// <summary>The one lock over everything below.</summary>
    private readonly Lock _lock = new();
    /// <summary>Runs by id.</summary>
    private readonly Dictionary<string, Run> _runs = [];
    /// <summary>Conversations by run and key.</summary>
    private readonly Dictionary<(string Run, ConversationKey Key), Conversation> _conversations = [];
    /// <summary>Turns with their stream events, by run and key.</summary>
    private readonly Dictionary<(string Run, TurnKey Key), (Turn Turn, StreamEvent[] Events)> _turns = [];
    /// <summary>Every run's requests, in one list.</summary>
    private readonly List<Request> _requests = [];
    /// <summary>Notes by run, in the order written. They live apart from the run, so saving it again keeps them.</summary>
    private readonly Dictionary<string, List<RunNote>> _notes = [];

    /// <summary>Saves the run, replacing the one under its id. Notes are not part of it, so they stay.</summary>
    public Task SaveRunAsync(Run run, CancellationToken cancellationToken = default)
    {
        lock (_lock) _runs[run.Id] = run;
        return Task.CompletedTask;
    }

    /// <summary>The run, or nothing if there is none.</summary>
    public Task<Run?> GetRunAsync(string id, CancellationToken cancellationToken = default)
    {
        lock (_lock) return Task.FromResult(_runs.GetValueOrDefault(id));
    }

    /// <summary>The newest runs first, up to the limit, and one check's only if asked.</summary>
    public Task<IReadOnlyList<Run>> ListRunsAsync(string? check = null, int limit = 50, CancellationToken cancellationToken = default)
    {
        lock (_lock)
            return Task.FromResult<IReadOnlyList<Run>>(_runs.Values.Where(r => check is null || r.Check == check)
                .OrderByDescending(r => r.StartedAt).Take(limit).ToArray());
    }

    /// <summary>Saves a conversation, replacing the one under its key.</summary>
    public Task SaveConversationAsync(string run, Conversation conversation, CancellationToken cancellationToken = default)
    {
        lock (_lock) _conversations[(run, conversation.Key)] = conversation;
        return Task.CompletedTask;
    }

    /// <summary>The run's conversations in the order they started.</summary>
    public Task<IReadOnlyList<Conversation>> ConversationsAsync(string run, CancellationToken cancellationToken = default)
    {
        lock (_lock)
            return Task.FromResult<IReadOnlyList<Conversation>>(_conversations.Where(c => c.Key.Run == run).Select(c => c.Value)
                .OrderBy(c => c.Order).ToArray());
    }

    /// <summary>Saves a turn with its stream events, replacing the ones under its key.</summary>
    public Task SaveTurnAsync(string run, Turn turn, IReadOnlyList<StreamEvent> events, CancellationToken cancellationToken = default)
    {
        lock (_lock) _turns[(run, turn.Key)] = (turn, events.ToArray());
        return Task.CompletedTask;
    }

    /// <summary>A conversation's turns in order.</summary>
    public Task<IReadOnlyList<Turn>> TurnsAsync(string run, ConversationKey conversation, CancellationToken cancellationToken = default)
    {
        lock (_lock)
            return Task.FromResult<IReadOnlyList<Turn>>(_turns.Where(t => t.Key.Run == run && t.Key.Key.Conversation == conversation)
                .Select(t => t.Value.Turn).OrderBy(t => t.Key.Turn).ToArray());
    }

    /// <summary>A turn's stream events in the order they came; none for an unknown turn.</summary>
    public Task<IReadOnlyList<StreamEvent>> TurnEventsAsync(string run, TurnKey turn, CancellationToken cancellationToken = default)
    {
        lock (_lock)
            return Task.FromResult<IReadOnlyList<StreamEvent>>(_turns.TryGetValue((run, turn), out var kept) ? kept.Events : []);
    }

    /// <summary>Adds requests to the list, tagged with the run they belong to.</summary>
    public Task AddRequestsAsync(string run, IReadOnlyList<Request> requests, CancellationToken cancellationToken = default)
    {
        lock (_lock) _requests.AddRange(requests.Select(r => r with { Run = run }));
        return Task.CompletedTask;
    }

    /// <summary>The run's requests, by time.</summary>
    public Task<IReadOnlyList<Request>> RequestsAsync(string run, CancellationToken cancellationToken = default)
    {
        lock (_lock)
            return Task.FromResult<IReadOnlyList<Request>>(_requests.Where(r => r.Run == run).OrderBy(r => r.At).ToArray());
    }

    /// <summary>Marks every run still 'running' as failed and ended now, and says how many.</summary>
    public Task<int> CloseOrphansAsync(DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var orphans = _runs.Values.Where(r => r.Status == BenchmarkRunStatus.Running).ToArray();
            foreach (var run in orphans) _runs[run.Id] = run with { Status = BenchmarkRunStatus.Failed, EndedAt = now };
            return Task.FromResult(orphans.Length);
        }
    }

    /// <summary>Adds a note once the shared rule says the text is fine, and keeps it beside the run.</summary>
    public Task<NoteResult> AddNoteAsync(string run, string? text, DateTimeOffset at, ConversationKey? conversation = null, CancellationToken cancellationToken = default)
    {
        // Turn a bad text down before taking the lock.
        var made = RunNotes.Make(text, at, conversation);
        if (made is not NoteResult.Added added) return Task.FromResult(made);

        // Under the lock: the run must be there, then the note goes on its list (made the first time).
        lock (_lock)
        {
            if (!_runs.ContainsKey(run)) return Task.FromResult<NoteResult>(new NoteResult.NoRun());
            if (!_notes.TryGetValue(run, out var kept)) _notes[run] = kept = [];
            kept.Add(added.Note);
        }

        return Task.FromResult(made);
    }

    /// <summary>The run's notes, in the order written.</summary>
    public Task<IReadOnlyList<RunNote>> NotesAsync(string run, CancellationToken cancellationToken = default)
    {
        lock (_lock) return Task.FromResult<IReadOnlyList<RunNote>>(_notes.TryGetValue(run, out var kept) ? kept.ToArray() : []);
    }

    /// <summary>Deletes the run and everything under it, notes included. Says whether there was a run.</summary>
    public Task<bool> DeleteRunAsync(string id, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            bool had = _runs.Remove(id);
            foreach (var key in _conversations.Keys.Where(k => k.Run == id).ToArray()) had |= _conversations.Remove(key);
            foreach (var key in _turns.Keys.Where(k => k.Run == id).ToArray()) had |= _turns.Remove(key);
            had |= _requests.RemoveAll(r => r.Run == id) > 0;
            had |= _notes.Remove(id);
            return Task.FromResult(had);
        }
    }
}
