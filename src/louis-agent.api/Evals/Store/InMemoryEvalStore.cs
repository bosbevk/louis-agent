using LouisAgent.Api.Evals.Store.Conversations;
using LouisAgent.Api.Evals.Store.Notes;
using LouisAgent.Api.Evals.Store.Runs;
using LouisAgent.Api.Evals.Store.Turns;
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

    /// <summary>The conversations.</summary>
    private readonly InMemoryConversationStore _conversations = new();

    /// <summary>The turns, with their stream events.</summary>
    private readonly InMemoryTurnStore _turns = new();

    /// <summary>The one lock over the records still kept here.</summary>
    private readonly Lock _lock = new();

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

    /// <summary>Hands the conversation to the conversation store.</summary>
    public Task SaveConversationAsync(string run, Conversation conversation,
        CancellationToken cancellationToken = default) =>
        _conversations.SaveConversationAsync(run, conversation, cancellationToken);

    /// <summary>Asks the conversation store for the run's conversations.</summary>
    public Task<IReadOnlyList<Conversation>> ConversationsAsync(string run,
        CancellationToken cancellationToken = default) => _conversations.ConversationsAsync(run, cancellationToken);

    /// <summary>Deletes only the run's conversations.</summary>
    public Task<bool> DeleteConversationsAsync(string run, CancellationToken cancellationToken = default) =>
        _conversations.DeleteConversationsAsync(run, cancellationToken);

    /// <summary>Hands the turn and its events to the turn store.</summary>
    public Task SaveTurnAsync(string run, Turn turn, IReadOnlyList<StreamEvent> events,
        CancellationToken cancellationToken = default) => _turns.SaveTurnAsync(run, turn, events, cancellationToken);

    /// <summary>Asks the turn store for a conversation's turns.</summary>
    public Task<IReadOnlyList<Turn>> TurnsAsync(string run, ConversationKey conversation,
        CancellationToken cancellationToken = default) => _turns.TurnsAsync(run, conversation, cancellationToken);

    /// <summary>Asks the turn store for a turn's stream events.</summary>
    public Task<IReadOnlyList<StreamEvent>> TurnEventsAsync(string run, TurnKey turn,
        CancellationToken cancellationToken = default) => _turns.TurnEventsAsync(run, turn, cancellationToken);

    /// <summary>Deletes only the run's turns.</summary>
    public Task<bool> DeleteTurnsAsync(string run, CancellationToken cancellationToken = default) =>
        _turns.DeleteTurnsAsync(run, cancellationToken);

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
        had |= await _conversations.DeleteConversationsAsync(id, cancellationToken);
        had |= await _turns.DeleteTurnsAsync(id, cancellationToken);
        lock (_lock)
        {
            had |= _requests.RemoveAll(r => r.Run == id) > 0;
            had |= _notes.Remove(id);
        }

        return had;
    }
}
