using LouisAgent.Api.Evals.Store.Notes;
using LouisAgent.Api.Evals.Store.Runs;
using static LouisAgent.Api.Evals.Store.EvalDocuments;
using static LouisAgent.Api.Evals.Runs.Protocol.EvalProtocol;

namespace LouisAgent.Api.Evals.Store;

/// <summary>
/// The whole store in memory: what the tests run on, and the reference the Mongo store is held to. Each record lives
/// in its own in-memory store; this class adds the notes and deleting a run with everything under it.
/// </summary>
/// <remarks>
/// Facade, the in-memory side: it hands each record to its own store, so tests and an API with no database see one
/// store, and each record store is held to its own contract test.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0022-benchmarks-recorded-from-the-protocol.md">ADR 0022: benchmark results in MongoDB</seealso>
public sealed class InMemoryEvalStore : IEvalStore
{
    /// <summary>The runs themselves.</summary>
    private readonly InMemoryRunStore _runs = new();

    /// <summary>The one lock over the records still kept here.</summary>
    private readonly Lock _lock = new();

    /// <summary>Conversations by run and key.</summary>
    private readonly Dictionary<(string Run, ConversationKey Key), Conversation> _conversations = [];

    /// <summary>Turns with their stream events, by run and key.</summary>
    private readonly Dictionary<(string Run, TurnKey Key), (Turn Turn, StreamEvent[] Events)> _turns = [];

    /// <summary>Every run's requests, in one list.</summary>
    private readonly List<Request> _requests = [];

    /// <summary>
    /// Notes by run, in the order written. They live apart from the run, so saving it again keeps them.
    /// </summary>
    private readonly Dictionary<string, List<RunNote>> _notes = [];

    /// <summary>Hands the run to the run store.</summary>
    public Task SaveRunAsync(Run run, CancellationToken cancellationToken = default) =>
        _runs.SaveRunAsync(run, cancellationToken);

    /// <summary>Asks the run store for the run.</summary>
    public Task<Run?> GetRunAsync(string id, CancellationToken cancellationToken = default) =>
        _runs.GetRunAsync(id, cancellationToken);

    /// <summary>Asks the run store for the newest runs.</summary>
    public Task<IReadOnlyList<Run>> ListRunsAsync(string? check = null, int limit = 50,
        CancellationToken cancellationToken = default) => _runs.ListRunsAsync(check, limit, cancellationToken);

    /// <summary>Has the run store close the runs a restart cut off.</summary>
    public Task<int> CloseOrphansAsync(DateTimeOffset now, CancellationToken cancellationToken = default) =>
        _runs.CloseOrphansAsync(now, cancellationToken);

    /// <summary>Deletes only the run's own record.</summary>
    public Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default) =>
        _runs.DeleteAsync(id, cancellationToken);

    /// <summary>Saves a conversation, replacing the one under its key.</summary>
    public Task SaveConversationAsync(string run, Conversation conversation,
        CancellationToken cancellationToken = default)
    {
        lock (_lock) _conversations[(run, conversation.Key)] = conversation;
        return Task.CompletedTask;
    }

    /// <summary>The run's conversations in the order they started.</summary>
    public Task<IReadOnlyList<Conversation>> ConversationsAsync(string run,
        CancellationToken cancellationToken = default)
    {
        lock (_lock)
            return Task.FromResult<IReadOnlyList<Conversation>>(_conversations.Where(c => c.Key.Run == run)
                .Select(c => c.Value)
                .OrderBy(c => c.Order).ToArray());
    }

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

    /// <summary>Adds a note beside the run, once the shared rule says the text is fine and the run is there.</summary>
    /// <remarks>Guard clauses: each reason not to keep the note returns first, so keeping it comes last.</remarks>
    public async Task<NoteResult> AddNoteAsync(string run, string? text, DateTimeOffset at,
        ConversationKey? conversation = null, CancellationToken cancellationToken = default)
    {
        // Turn a bad text down before asking about the run.
        var made = RunNotes.Make(text, at, conversation);
        if (made is not NoteResult.Added added) return made;

        // A note needs a run to belong to.
        if (await _runs.GetRunAsync(run, cancellationToken) is null) return new NoteResult.NoRun();

        // The note goes on the run's list, made the first time.
        lock (_lock)
        {
            if (!_notes.TryGetValue(run, out var kept)) _notes[run] = kept = [];
            kept.Add(added.Note);
        }

        return made;
    }

    /// <summary>The run's notes, in the order written.</summary>
    public Task<IReadOnlyList<RunNote>> NotesAsync(string run, CancellationToken cancellationToken = default)
    {
        lock (_lock)
            return Task.FromResult<IReadOnlyList<RunNote>>(_notes.TryGetValue(run, out var kept) ? kept.ToArray() : []);
    }

    /// <summary>Deletes the run and everything under it, notes included. Says whether there was a run.</summary>
    public async Task<bool> DeleteRunAsync(string id, CancellationToken cancellationToken = default)
    {
        // Anything found under the id counts as a run having been there, even a leftover with no run record.
        bool had = await _runs.DeleteAsync(id, cancellationToken);
        lock (_lock)
        {
            foreach (var key in _conversations.Keys.Where(k => k.Run == id).ToArray())
                had |= _conversations.Remove(key);
            foreach (var key in _turns.Keys.Where(k => k.Run == id).ToArray()) had |= _turns.Remove(key);
            had |= _requests.RemoveAll(r => r.Run == id) > 0;
            had |= _notes.Remove(id);
        }

        return had;
    }
}
